using System;
using System.Collections.Generic;
using Lokanta.Core;
using Xunit;

namespace Lokanta.Core.Tests
{
    public class RngTests
    {
        [Fact]
        public void The_same_seed_gives_the_same_sequence()
        {
            Rng a = RngSeeder.Create(20260909UL, RngStream.Arrival);
            Rng b = RngSeeder.Create(20260909UL, RngStream.Arrival);

            for (int i = 0; i < 1000; i++)
                Assert.Equal(a.Next(), b.Next());
        }

        [Fact]
        public void Different_streams_give_different_sequences()
        {
            Rng arrival = RngSeeder.Create(20260909UL, RngStream.Arrival);
            Rng market = RngSeeder.Create(20260909UL, RngStream.Market);

            int same = 0;
            for (int i = 0; i < 500; i++)
                if (arrival.Next() == market.Next()) same++;

            Assert.True(same < 5, $"The streams collide: {same} identical values in 500 draws");
        }

        [Fact]
        public void Adding_calls_to_one_stream_does_not_shift_another()
        {
            // docs/23 3.4: the stream independence test.
            Rng arrivalBefore = RngSeeder.Create(7UL, RngStream.Arrival);
            uint[] expected = new uint[20];
            for (int i = 0; i < expected.Length; i++) expected[i] = arrivalBefore.Next();

            // Make a thousand calls on a different stream
            Rng noisy = RngSeeder.Create(7UL, RngStream.Event);
            for (int i = 0; i < 1000; i++) noisy.Next();

            Rng arrivalAfter = RngSeeder.Create(7UL, RngStream.Arrival);
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal(expected[i], arrivalAfter.Next());
        }

        [Fact]
        public void The_direct_sequences_match_the_independent_reference()
        {
            // The expected values come not from the C# output but from a
            // separate Python implementation (tools/balance/rng_reference.py).
            // A test that takes its expected value from the code it tests
            // proves nothing at all.
            RngReference reference = RngReference.Load();

            foreach (RngDirectCase c in reference.Direct)
            {
                Rng r = new Rng(c.State[0], c.State[1], c.State[2], c.State[3]);
                for (int i = 0; i < c.Values.Count; i++)
                {
                    uint got = r.Next();
                    Assert.True(c.Values[i] == got,
                        $"state [{string.Join(",", c.State)}] draw {i}: " +
                        $"reference {c.Values[i]}, C# {got}");
                }
            }
        }

        [Fact]
        public void The_seeded_streams_match_the_independent_reference()
        {
            RngReference reference = RngReference.Load();

            foreach (RngSeededCase c in reference.Seeded)
            {
                Rng r = RngSeeder.Create(c.MasterSeed, (RngStream)c.StreamIndex);
                for (int i = 0; i < c.Values.Count; i++)
                {
                    uint got = r.Next();
                    Assert.True(c.Values[i] == got,
                        $"stream {c.Stream} draw {i}: reference {c.Values[i]}, C# {got}");
                }
            }
        }

        [Fact]
        public void NextInt_matches_the_independent_reference()
        {
            RngReference reference = RngReference.Load();
            Rng r = RngSeeder.Create(42UL, RngStream.Order);

            for (int i = 0; i < reference.NextInt10.Count; i++)
            {
                int got = r.NextInt(10);
                Assert.True(reference.NextInt10[i] == got,
                    $"NextInt(10) draw {i}: reference {reference.NextInt10[i]}, C# {got}");
            }
        }

        [Fact]
        public void An_all_zero_seed_does_not_lock_up()
        {
            Rng r = new Rng(0, 0, 0, 0);
            bool anyNonZero = false;
            for (int i = 0; i < 10; i++) if (r.Next() != 0) anyNonZero = true;
            Assert.True(anyNonZero, "The zero state locked the generator up");
        }

        [Fact]
        public void NextInt_does_not_exceed_its_bounds()
        {
            Rng r = RngSeeder.Create(42UL, RngStream.Order);
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(7);
                Assert.InRange(v, 0, 6);
            }
        }

        [Fact]
        public void NextInt_handles_a_range_correctly()
        {
            Rng r = RngSeeder.Create(42UL, RngStream.Order);
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(10, 20);
                Assert.InRange(v, 10, 19);
            }
        }

        [Fact]
        public void NextInt_throws_on_an_invalid_bound()
        {
            Rng r = RngSeeder.Create(1UL, RngStream.Order);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(-1));
        }

        [Fact]
        public void Chance_behaves_sensibly_at_three_points()
        {
            Rng r = RngSeeder.Create(99UL, RngStream.StaffError);
            Assert.False(r.Chance(0));
            Assert.True(r.Chance(Fx.One));

            int hits = 0;
            const int n = 20000;
            for (int i = 0; i < n; i++) if (r.Chance(2500)) hits++;

            // 25% expected; with room left for statistical noise
            Assert.InRange(hits, (int)(n * 0.23), (int)(n * 0.27));
        }

        [Fact]
        public void The_distribution_passes_a_rough_uniformity_test()
        {
            Rng r = RngSeeder.Create(20260910UL, RngStream.Archetype);
            int[] buckets = new int[10];
            const int n = 100000;
            for (int i = 0; i < n; i++) buckets[r.NextInt(10)]++;

            foreach (int b in buckets)
                Assert.InRange(b, (int)(n * 0.09), (int)(n * 0.11));
        }

        [Fact]
        public void Every_stream_gets_a_different_starting_state()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < (int)RngStream.Count; i++)
            {
                Rng r = RngSeeder.Create(20260909UL, (RngStream)i);
                string key = $"{r.S0}-{r.S1}-{r.S2}-{r.S3}";
                Assert.True(seen.Add(key), $"Stream {i} starts in the same state as another stream");
            }
        }

        [Fact]
        public void The_state_can_be_restored_from_a_save()
        {
            Rng r = RngSeeder.Create(5UL, RngStream.Hiring);
            for (int i = 0; i < 37; i++) r.Next();

            // The four uints written into the save file
            Rng restored = new Rng(r.S0, r.S1, r.S2, r.S3);

            for (int i = 0; i < 50; i++)
                Assert.Equal(r.Next(), restored.Next());
        }
    }
}
