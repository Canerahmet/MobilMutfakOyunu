using System;

namespace Lokanta.Core.Save
{
    /// <summary>
    /// Durum yuruyusu. docs/23-cekirdek-sozlesmesi.md 6.2.
    ///
    /// Serilestirme YANSIMAYLA degil, elle yazilmis yuruyusle yapiliyor.
    /// Ayni yuruyus iki sey uretiyor: kayit dosyasi ve durum ozeti.
    ///
    /// Bunun onemi: bir alan kayitta unutulursa ozet de onu gormez ve
    /// determinizm testi yakalayamaz. Tek yuruyus oldugu icin boyle bir
    /// bosluk acilamiyor.
    ///
    /// ALAN SIRASI SOZLESMEDIR. Sira degisirse eski kayitlar okunamaz.
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
        /// Bu anahtar kayitta VAR MI.
        ///
        /// KAYIT GOCUNUN TEMELI. Kayit bicimi bir alan eklendigi anda
        /// degisiyor (surum 2'den 14'e cikmis) ve okuyucu eksik
        /// anahtarda ISTISNA atiyordu: yayindan sonra tek bir denge
        /// yamasi, her oyuncunun altmis gunluk kampanyasini "bozuk"
        /// yapardi. Mobil yonetim oyunlarinda tek yildizli yorumlarin
        /// bir numarali sebebi bu.
        ///
        /// KURAL: yeni eklenen her alan Has() ile okunur ve yoksa
        /// varsayilanda birakilir. Alan SILINMESI ya da anlaminin
        /// degismesi hala surum anahtarli okuma gerektirir.
        /// </summary>
        bool Has(string key);
    }

    /// <summary>
    /// FNV-1a 64. Durumun bayt bayt ozeti.
    ///
    /// Nesne GetHashCode'u KULLANILMIYOR: .NET Core'da string hash'i her
    /// surecte farkli ve deterministik degil. Burada acik bayt yuruyusu var.
    ///
    /// Anahtarlar da ozete giriyor: iki alanin yer degistirmesi ayni
    /// degerleri ayni sirada verse bile ozeti degistirir.
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
