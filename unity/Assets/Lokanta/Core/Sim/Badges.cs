namespace Lokanta.Core.Sim
{
    /// <summary>
    /// NISANLAR - gorev degil, TANIMA.
    ///
    /// Ayrim bu dosyanin butun gerekcesi. Bir gorev "yarin sunu yap"
    /// der ve oyuncuyu gorunmez bir patronun calisani yapar; oyunun tek
    /// cumlelik vaadi ise "Patronsun, asci degil". Bir nisan "bugun sunu
    /// basardin" der - geriye donuktur, bu yuzden oyuncunun planiyla
    /// ASLA catismaz. Bilerek kadrosu eksik calisan oyuncuyu bir gorev
    /// cezalandirir, bir nisan odullendirir.
    ///
    /// Ikinci gerekce olcumden: bu projenin yasasi "doymus bir eksene
    /// odenen odul gorunmez". Klasik gunluk gorev odulu paradir ve
    /// harness'a gore iyi oyuncu altmisinci gunu ~21.000 kasayla
    /// bitiriyor - yani odul, tam da hissedilecegi anda hissedilmiyor.
    /// Nisanlarin odulu para DEGIL: gorulmek.
    ///
    /// Hepsi simulasyonun ZATEN bildigi seylerden kuruldu; tek yeni
    /// takip "defter bir kez acildi mi" bayragi. Uydurma kosul yok,
    /// cunku uydurma bir kosul olculemez.
    /// </summary>
    public static class Badges
    {
        /// <summary>
        /// Zirve gununde kimse kapidan donmedi ve kimse masadan kizgin
        /// kalkmadi. Zirve sarti onemli: hafta ici bunu yapmak kolay ve
        /// kolay olan bir sey tanima hak etmiyor.
        /// </summary>
        public const int HerkesDoydu = 0;

        /// <summary>
        /// Zirveyi gereken kadronun ALTINDA gecti ve yine de kimse
        /// masadan kizgin kalkmadi. Oyunun merkezi takasini - kadro
        /// kapasite demek ama para demek - dogrudan odullendiriyor.
        /// </summary>
        public const int ZirveEksikKadro = 1;

        /// <summary>
        /// Defter acildi ve tamami tahsil edildi. Turk mutfaginin imza
        /// mekanigi; fast food oynayan oyuncu bunu hic gormez ve
        /// gormemeli.
        /// </summary>
        public const int DefterKapandi = 2;

        /// <summary>Bir mudavimin ilk hikaye sahnesi acildi.</summary>
        public const int IlkSahne = 3;

        /// <summary>Kasada ilk kez on bin sikke.</summary>
        public const int IlkOnBin = 4;

        /// <summary>
        /// Dukkan ilk kez buyudu.
        ///
        /// Bu ikisi (genisleme ve itibar) OLCULEREK eklendi. Ilk bes
        /// nisanla makul oyuncu gunleri 5, 6 ve 8'de uc tanima aliyordu
        /// ve sonra ELLI IKI GUN sessizlik vardi - yani basari egrisi
        /// birinci haftada oluyordu. Nisanlarin isi kampanyaya yayilmak.
        /// </summary>
        public const int IlkGenisleme = 5;

        /// <summary>
        /// Itibar 90'a cikti. Gec geliyor cunku itibar masa kademesinin
        /// tavanina kirpiliyor: 90'i gormek once GENISLEMEYI gerektiriyor.
        /// </summary>
        public const int SemtinKonustugu = 6;

        public const int Count = 7;

        /// <summary>Itibar esigi, SANTI. Tek kaynak.</summary>
        public const int ReputationMilestoneCenti = 9000;

        /// <summary>
        /// Kasa esigi, SANTI-SIKKE. Tek kaynak: hem kosul hem metin buradan.
        ///
        /// Birim olculerek duzeldi. Once 10000 yaziyordu ve bu, oyunun
        /// para biriminde 100 sikke demek: baslangic kasasi 800.000 santi
        /// (8.000 sikke) oldugu icin nisan BIRINCI GUNUN sonunda
        /// dagitiliyordu. Hicbir sey kirilmiyordu - yalnizca "ilk on bin"
        /// diye bir tanima, hic kazanilmadan veriliyordu.
        /// </summary>
        public const long CashMilestone = 1000000;   // 10.000 sikke

        /// <summary>
        /// Nisanin adi. Metni gorunum Loc'tan cozuyor - cekirdekte
        /// metin YOK (docs/23 6.3).
        /// </summary>
        public static string NameKey(int i)
        {
            switch (i)
            {
                case HerkesDoydu: return "badge.full_house";
                case ZirveEksikKadro: return "badge.short_peak";
                case DefterKapandi: return "badge.book_closed";
                case IlkSahne: return "badge.first_beat";
                case IlkOnBin: return "badge.first_ten_k";
                case IlkGenisleme: return "badge.first_expand";
                default: return "badge.renowned";
            }
        }

        /// <summary>Nisanin altindaki tek satirlik aciklama.</summary>
        public static string NoteKey(int i)
        {
            return NameKey(i) + ".note";
        }
    }
}
