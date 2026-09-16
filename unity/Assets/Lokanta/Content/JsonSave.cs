using System;
using System.Collections.Generic;
using Lokanta.Core.Save;
using Newtonsoft.Json.Linq;

namespace Lokanta.Content
{
    /// <summary>
    /// Writes the state walk into a JSON tree.
    /// The docs/15-save-system.md format: JSON, gzip, checksum.
    /// This class only produces the tree; compressing it and writing it to
    /// a file is the platform layer's job (the ISaveStore port).
    ///
    /// docs/23 6.1: Newtonsoft ONLY in this assembly.
    /// </summary>
    public sealed class JsonStateWriter : IStateWriter
    {
        private readonly Stack<JObject> _stack = new Stack<JObject>();
        private readonly JObject _root = new JObject();

        public JsonStateWriter() { _stack.Push(_root); }

        public JObject Root { get { return _root; } }
        public string ToJson() { return _root.ToString(Newtonsoft.Json.Formatting.None); }

        private JObject Current { get { return _stack.Peek(); } }

        public void Begin(string key)
        {
            JObject child = new JObject();
            Current[key] = child;
            _stack.Push(child);
        }

        public void End()
        {
            if (_stack.Count <= 1)
                throw new InvalidOperationException("End() called without a Begin()");
            _stack.Pop();
        }

        public void Int(string key, int value) { Current[key] = value; }
        public void Long(string key, long value) { Current[key] = value; }
        public void UInt(string key, uint value) { Current[key] = value; }
        public void Bool(string key, bool value) { Current[key] = value; }
        public void Str(string key, string value) { Current[key] = value; }

        public void IntArray(string key, int[] values, int count)
        {
            JArray a = new JArray();
            for (int i = 0; i < count; i++) a.Add(values[i]);
            Current[key] = a;
        }

        public void LongArray(string key, long[] values, int count)
        {
            JArray a = new JArray();
            for (int i = 0; i < count; i++) a.Add(values[i]);
            Current[key] = a;
        }

        public void BoolArray(string key, bool[] values, int count)
        {
            JArray a = new JArray();
            for (int i = 0; i < count; i++) a.Add(values[i]);
            Current[key] = a;
        }
    }

    /// <summary>The mirror of JsonStateWriter. It MUST walk the SAME ORDER.</summary>
    public sealed class JsonStateReader : IStateReader
    {
        private readonly Stack<JObject> _stack = new Stack<JObject>();

        public JsonStateReader(JObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _stack.Push(root);
        }

        public JsonStateReader(string json) : this(JObject.Parse(json)) { }

        private JObject Current { get { return _stack.Peek(); } }

        private JToken Need(string key)
        {
            JToken t = Current[key];
            if (t == null)
                throw new ContentException("Field missing from the save: " + key);
            return t;
        }

        public void Begin(string key)
        {
            JObject child = Need(key) as JObject;
            if (child == null)
                throw new ContentException("Expected an object in the save: " + key);
            _stack.Push(child);
        }

        public void End()
        {
            if (_stack.Count <= 1)
                throw new InvalidOperationException("End() called without a Begin()");
            _stack.Pop();
        }

        public int Int(string key) { return Need(key).Value<int>(); }
        public long Long(string key) { return Need(key).Value<long>(); }
        public uint UInt(string key) { return Need(key).Value<uint>(); }
        public bool Bool(string key) { return Need(key).Value<bool>(); }
        public string Str(string key) { return Need(key).Value<string>(); }

        /// <summary>Is this key present on the current node? See IStateReader.Has.</summary>
        public bool Has(string key)
        {
            return _stack.Count > 0 && _stack.Peek()[key] != null;
        }

        private JArray Array(string key, int count)
        {
            JArray a = Need(key) as JArray;
            if (a == null)
                throw new ContentException("Expected an array in the save: " + key);
            if (a.Count != count)
                throw new ContentException(
                    "Array length in the save is " + a.Count + ", expected " + count + ": " + key);
            return a;
        }

        public void IntArray(string key, int[] target, int count)
        {
            JArray a = Array(key, count);
            for (int i = 0; i < count; i++) target[i] = a[i].Value<int>();
        }

        public void LongArray(string key, long[] target, int count)
        {
            JArray a = Array(key, count);
            for (int i = 0; i < count; i++) target[i] = a[i].Value<long>();
        }

        public void BoolArray(string key, bool[] target, int count)
        {
            JArray a = Array(key, count);
            for (int i = 0; i < count; i++) target[i] = a[i].Value<bool>();
        }
    }
}
