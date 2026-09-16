using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit;

namespace Lokanta.Core.Tests
{
    public sealed class RngDirectCase
    {
        [JsonProperty("state")] public List<uint> State { get; set; }
        [JsonProperty("values")] public List<uint> Values { get; set; }
    }

    public sealed class RngSeededCase
    {
        [JsonProperty("masterSeed")] public ulong MasterSeed { get; set; }
        [JsonProperty("stream")] public string Stream { get; set; }
        [JsonProperty("streamIndex")] public int StreamIndex { get; set; }
        [JsonProperty("values")] public List<uint> Values { get; set; }
    }

    /// <summary>
    /// The independent reference produced by tools/balance/rng_reference.py.
    /// The C# generator has to match it.
    /// </summary>
    public sealed class RngReference
    {
        [JsonProperty("direct")] public List<RngDirectCase> Direct { get; set; }
        [JsonProperty("seeded")] public List<RngSeededCase> Seeded { get; set; }
        [JsonProperty("nextInt10")] public List<int> NextInt10 { get; set; }

        public static RngReference Load()
        {
            string path = Path.Combine(Paths.Golden, "rng.json");
            Assert.True(File.Exists(path),
                "No RNG reference. Run 'python tools/balance/rng_reference.py' first. Expected: " + path);
            return JsonConvert.DeserializeObject<RngReference>(File.ReadAllText(path));
        }
    }
}
