using System;

namespace Lokanta.Core.Save
{
    /// <summary>
    /// The state walk. docs/23-core-contract.md 6.2.
    ///
    /// Serialisation is done NOT BY REFLECTION but by a hand-written walk.
    /// The same walk produces two things: the save file and the state hash.
    ///
    /// Why that matters: if a field is forgotten in the save, the hash does
    /// not see it either and the determinism test cannot catch it. Because
    /// there is only ONE walk, no such gap can open.
    ///
    /// FIELD ORDER IS A CONTRACT. If the order changes, old saves cannot be read.
    /// </summary>
    public interface IStateWriter
    {
        void Begin(string key);
        void End();
        void Int(string key, int value);
        void Long(string key, long value);
        void UInt(string key, uint value);
        void Bool(string key, bool value);
        void Str(string key, string value);
        void IntArray(string key, int[] values, int count);
        void LongArray(string key, long[] values, int count);
        void BoolArray(string key, bool[] values, int count);
    }

    public interface IStateReader
    {
        void Begin(string key);
        void End();
        int Int(string key);
        long Long(string key);
        uint UInt(string key);
        bool Bool(string key);
        string Str(string key);
        void IntArray(string key, int[] target, int count);
        void LongArray(string key, long[] target, int count);
        void BoolArray(string key, bool[] target, int count);

        /// <summary>
        /// IS this key PRESENT in the save.
        ///
        /// THE FOUNDATION OF SAVE MIGRATION. The save format changes the
        /// moment a field is added (the version has climbed from 2 to 14)
        /// and the reader used to THROW on a missing key: after release a
        /// single balance patch would have made every player's sixty-day
        /// campaign "corrupt". That is the number one cause of one-star
        /// reviews on mobile management games.
        ///
        /// THE RULE: every newly added field is read through Has() and left
        /// at its default when it is absent. DELETING a field, or changing
        /// what it means, still requires a version-keyed read.
        /// </summary>
        bool Has(string key);
    }

    /// <summary>
    /// FNV-1a 64. A byte-by-byte hash of the state.
    ///
    /// Object GetHashCode IS NOT USED: on .NET Core the string hash differs
    /// in every process and is not deterministic. Here there is an explicit
    /// byte walk.
    ///
    /// The keys go into the hash too: two fields swapping places changes the
    /// hash even when they hand over the same values in the same order.
    /// </summary>
    public sealed class HashStateWriter : IStateWriter
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _hash = Offset;

        public ulong Result { get { return _hash; } }

        public void Reset() { _hash = Offset; }

        private void Byte(byte b)
        {
            unchecked
            {
                _hash ^= b;
                _hash *= Prime;
            }
        }

        private void Key(string key)
        {
            if (key == null) { Byte(0); return; }
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                Byte((byte)(c & 0xFF));
                Byte((byte)((c >> 8) & 0xFF));
            }
            Byte(0);
        }

        private void Raw(ulong v)
        {
            for (int i = 0; i < 8; i++) Byte((byte)((v >> (i * 8)) & 0xFF));
        }

        public void Begin(string key) { Key("{"); Key(key); }
        public void End() { Key("}"); }

        public void Int(string key, int value) { Key(key); Raw(unchecked((ulong)(long)value)); }
        public void Long(string key, long value) { Key(key); Raw(unchecked((ulong)value)); }
        public void UInt(string key, uint value) { Key(key); Raw(value); }
        public void Bool(string key, bool value) { Key(key); Byte(value ? (byte)1 : (byte)0); }

        public void Str(string key, string value)
        {
            Key(key);
            Key(value);
        }

        public void IntArray(string key, int[] values, int count)
        {
            Key(key);
            Raw((ulong)count);
            for (int i = 0; i < count; i++) Raw(unchecked((ulong)(long)values[i]));
        }

        public void LongArray(string key, long[] values, int count)
        {
            Key(key);
            Raw((ulong)count);
            for (int i = 0; i < count; i++) Raw(unchecked((ulong)values[i]));
        }

        public void BoolArray(string key, bool[] values, int count)
        {
            Key(key);
            Raw((ulong)count);
            for (int i = 0; i < count; i++) Byte(values[i] ? (byte)1 : (byte)0);
        }
    }
}
