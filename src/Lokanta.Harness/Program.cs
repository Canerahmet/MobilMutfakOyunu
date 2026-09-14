using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;

namespace Lokanta.Harness
{
    /// <summary>
    /// Bassiz denge araci.
    ///
    /// docs/04-mimari.md: Faz 0'in urunu. Farkli oyuncu stratejileriyle
    /// kampanyalari simule eder ve docs/12-ekonomi.md 8'deki sorulari
    /// sayilarla cevaplar.
    ///
    /// Calistirma:
    ///   dotnet run --project src/Lokanta.Harness
    ///   dotnet run --project src/Lokanta.Harness -- --seeds 20 --csv out.csv
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Varsayilan mutfak. --mutfak turk ile degistirilebiliyor.
        ///
        /// Butun denge cozumu fast food ile yapildi ve ikinci mutfak hic
        /// olculmedi. Dilim sureleri farkli (576/2304/1200/720), yani Turk
        /// lokantasinin ogle zirvesi cok daha keskin; ayni kadro ve ayni
        /// ekipmanla ayni sonucu vermesi icin bir sebep yok.
        /// </summary>
        private const string DefaultCuisine = "fastfood";

        public static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int seeds = ArgInt(args, "--seeds", 8);

            // Soguk hava merdiveninin basamaklarini tek tek olcmek icin.
            // Varsayilan sinirsiz, yani normal kosuyu etkilemiyor.
            Equipment.StorageCap = ArgInt(args, "--depo-tavan", int.MaxValue);
            int days = ArgInt(args, "--days", 60);
            string csv = ArgStr(args, "--csv", null);
            string root = FindRoot();

            EconomyConfig economy = ContentLoader.LoadEconomy(Path.Combine(root, "content"));
            string cuisine = ArgStr(args, "--mutfak", DefaultCuisine);

            // BILINMEYEN BAYRAK HATA.
            //
            // Once sessizce yutuluyordu ve bu tehlikeliydi: "--cuisine turk"
            // yazan biri ikinci bir fastfood kosusu aliyor, iki mutfagi
            // karsilastirdigini saniyordu. Bir olcum aracinin sessizce
            // yanlis seyi olcmesi, hic olcmemekten kotudur.
            if (!CheckArgs(args)) return 2;
            ContentSet content = ContentSetLoader.Load(Path.Combine(root, "content"), cuisine);
            // Dilim sureleri mutfaktan geliyor (docs/28 Karar G).
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(content.SlotDurationsBp).WithEatMs(content.EatMs)
                : TimingConfig.Default();

            Console.WriteLine("=== Lokanta denge araci ===");
            Console.WriteLine($"mutfak      : {cuisine}");
            Console.WriteLine($"yemek       : {content.Dishes.Length}");
            Console.WriteLine($"arketip     : {content.Archetypes.Length}");
            Console.WriteLine($"gun         : {days}");
            Console.WriteLine($"tohum       : {seeds}");
            Console.WriteLine($"servis gunu : {timing.ServiceDayMs / 1000} sn sim, {timing.ServiceTicks} tick");
            Console.WriteLine($"dilim tick  : {timing.SlotTicks(0)} / {timing.SlotTicks(1)} / " +
                              $"{timing.SlotTicks(2)} / {timing.SlotTicks(3)}");
            Console.WriteLine();

            if (Array.IndexOf(args, "--solve") >= 0)
            {
                RentSolver.Run(economy, content, timing, Math.Min(seeds, 4), days);
                return 0;
            }

            List<StrategyResult> results = new List<StrategyResult>();
            StringBuilder rows = new StringBuilder();
            rows.AppendLine("strateji,tohum,gun,masa,asci,salon,plan,servis,masadan_kizgin,kisi,ciro,memnuniyet,itibar,kasa");

            // "--strateji" ARTIK OKUNUYOR. Bayrak listede duruyordu ama
            // hicbir yerde okunmuyordu; tek bir botu olcmek isteyen kisi
            // yirmi botun tamamini kosuyor ve filtreledigini saniyordu.
            string yalniz = ArgStr(args, "--strateji", null);
            int esles = 0;

            foreach (IStrategy proto in AllStrategies())
            {
                if (yalniz != null
                    && !proto.Name.Equals(yalniz, StringComparison.OrdinalIgnoreCase))
                    continue;
                esles++;

                StrategyResult agg = new StrategyResult(proto.Name, proto.Question)
                {
                    // Mutabakat icin: kasa hareketi buradan olculuyor.
                    StartCash = economy.StartingCash,
                };

                // OLCUM KOLU STRATEJIYE GORE: baskili cift salonu bir
                // kisi eksik calistiriyor. Kol STATIK oldugu icin her
                // strateji basinda yeniden yaziliyor - yoksa bir
                // sonraki strateji eksik kadroyu miras alir ve olcum
                // sessizce baska bir seyi olcer.
                ReasonablePlayer.SalonShort = proto is PressuredPlayer ? 1 : 0;
                Interventionist.ResetCounters();

                // SABIRLI KIP DE STATIK: sifirlanmazsa bir sonraki
                // strateji onu miras alir ve olcum sessizce baska bir
                // seyi olcer - yukaridaki SalonShort ile ayni tuzak.
                Interventionist.OnlyWhenUrgent = proto is PatientInterventionist;

                // Secici kol yalnizca 5+ ziyaretli musteriye yaziyor.
                SignaturePlayer.MinVisits = proto is PickyCreditor ? 5 : 0;

                for (int s = 0; s < seeds; s++)
                {
                    ulong seed = 20260910UL + (ulong)s * 7919UL;
                    IStrategy strategy = NewLike(proto);
                    RunOne(economy, content, timing, seed, days, strategy, agg, rows);
                }
                ReasonablePlayer.SalonShort = 0;
                agg.InterventionsTried = Interventionist.Tried;
                agg.InterventionsApplied = Interventionist.Applied;

                agg.Finish(seeds);
                results.Add(agg);
            }

            // SESSIZ BOS TABLO YOK. Yazim hatasi yapan kisi bos bir
            // rapor gorup "demek bu bot hicbir sey yapmiyor" der.
            if (yalniz != null && esles == 0)
            {
                Console.WriteLine($"HATA: '{yalniz}' diye bir strateji yok.");
                return 2;
            }

            Report(results);
            PlateReport(results);

            if (csv != null)
            {
                File.WriteAllText(csv, rows.ToString());
                Console.WriteLine($"\nCSV yazildi: {csv}");
            }
            return 0;
        }

        private static IEnumerable<IStrategy> AllStrategies()
        {
            yield return new PassivePlayer();
            yield return new RestockOnly();
            yield return new ReasonablePlayer();
            yield return new ReasonablePlayer(expand: false);
            yield return new Expansionist();
            yield return new PlannerSchedule();
            yield return new GreedyPricer();
            yield return new GreedyPricer(11000, "orta_fiyat");
            yield return new ExtrasGouger();
            yield return new OverStaffer();
            yield return new CheapIngredients();
            yield return new Interventionist();
            yield return new PatientInterventionist();
            yield return new SignaturePlayer();
            yield return new PickyCreditor();
            yield return new ComboGouger();
            yield return new OneDishPlayer();
            yield return new WideMenuPlayer();
            yield return new NoLoanPlayer();
            yield return new CheapPricer();

            // BASKI CIFTI: aralarindaki TEK fark mudahale.
            //
            // Rahat bir restoranda mudahalenin olculemedigi olculmustu
            // (1440 mudahalenin 1440'i gecti, sonuc degismedi). Bu cift,
            // "mekanik zayif mi" ile "kurtarilacak bir sey yok mu"
            // sorularini ayiriyor.
            yield return new PressuredPlayer(intervene: false);
            yield return new PressuredPlayer(intervene: true);
        }

        private static IStrategy NewLike(IStrategy proto)
        {
            switch (proto.Name)
            {
                case "pasif": return new PassivePlayer();
                case "sadece_hal": return new RestockOnly();
                case "makul": return new ReasonablePlayer();
                case "genislemeyen": return new ReasonablePlayer(expand: false);
                case "atilgan": return new Expansionist();
                case "planci": return new PlannerSchedule();
                case "yuksek_fiyat": return new GreedyPricer();
                case "orta_fiyat": return new GreedyPricer(11000, "orta_fiyat");
                case "pahali_ekstra": return new ExtrasGouger();
                case "fazla_kadro": return new OverStaffer();
                case "ucuz_malzeme": return new CheapIngredients();
                case "mudahaleci": return new Interventionist();
                case "sabirli_mudahale": return new PatientInterventionist();
                case "imzaci": return new SignaturePlayer();
                case "secici_veresiye": return new PickyCreditor();
                case "kombo_sismesi": return new ComboGouger();
                case "tek_yemek": return new OneDishPlayer();
                case "genis_menu": return new WideMenuPlayer();
                case "kredisiz": return new NoLoanPlayer();
                case "ucuz_fiyat": return new CheapPricer();
                case "baskili": return new PressuredPlayer(intervene: false);
                case "baskili_mudahale": return new PressuredPlayer(intervene: true);
                default: throw new InvalidOperationException(proto.Name);
            }
        }

        // -------------------------------------------------------------------
        private static void RunOne(EconomyConfig economy, ContentSet content,
                                   TimingConfig timing, ulong seed, int days,
                                   IStrategy strategy, StrategyResult agg,
                                   StringBuilder rows)
        {
            Simulation sim = new Simulation(economy, content, timing, seed);
            int limit = timing.ServiceTicks + 6000;

            long partiesLost = 0, peopleServed = 0, totalRevenue = 0, totalIngredients = 0;
            long plannedParties = 0, servedParties = 0, turnedAway = 0;
            int trivialWeek = 0;

            // "Restoran gozle gorulur bicimde bosaldi" gunu: itibar, talep
            // egrisinin kirilma noktasinin altina indigi gun.
            //
            // docs/08 kapanisi reddediyor ("kayit silinmez, oyun bitmez"),
            // yani ihmalin bedeli oyunu bitirmek degil. Ama oyuncu isin
            // bittigini GORMELI. Bu sutun onu olcuyor: kasadaki para
            // cenazeyi geciktirse de dukkan ne zaman bosaldi.
            const int CollapseCenti = 2000;
            int collapseDay = 0;

            // TABAK BASINCI: darbogaz gercekten isiriyor mu.
            //
            // docs/14 bulasikciyi bir darbogaz olarak tarif ediyor ama
            // mekanik eklendiginde dogrudan olculmesi gerekti: "var olmak"
            // ile "hissedilmek" ayri seyler ve bu projede dekoratif bir
            // mekanik en kotu sonuctur.
            int plateBlocked = 0, plateMinClean = int.MaxValue, plateMaxDirty = 0;

            for (int day = 1; day <= days; day++)
            {
                strategy.OnMorning(sim);

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();

                    // Servis SIRASINDA mudahale. docs/02 cekirdek dongusu:
                    // "servis sirasinda sadece krizlere mudahale edersin".
                    // Yalnizca o stratejiye acik; digerleri hic kullanmiyor
                    // ki farki olculebilsin.
                    // SABIRLI KOL HER TIK SORULUYOR.
                    //
                    // 200 tiklik aralik (20 sim-saniye) mudahaleci
                    // icin yeterliydi cunku o zaten ilk firsatta
                    // harciyor. Sabirli kol ise KRIZI BEKLIYOR ve
                    // kriz penceresi 3 saniye: yirmi saniyede bir
                    // bakan bir bot o pencereyi cogu zaman kacirir
                    // ve olcum "saklamak ise yaramiyor" derdi -
                    // olctugu sey aslinda kendi goz kirpmasi olurdu.
                    if (strategy is PatientInterventionist)
                        Interventionist.DuringService(sim);
                    else if ((t % 200) == 0
                        && (strategy is Interventionist
                            || (strategy is PressuredPlayer pp && pp.Intervenes)))
                        Interventionist.DuringService(sim);

                    // Imza mekanigi de servis sirasinda isliyor: veresiye
                    // odeme aninda aciliyor, kombo siparis aninda.
                    if ((t % 50) == 0)
                    {
                        if (strategy is SignaturePlayer sp) sp.DuringService(sim);
                        else if (strategy is PickyCreditor pc) pc.DuringService(sim);
                    }

                    if (sim.PlatesClean < plateMinClean) plateMinClean = sim.PlatesClean;
                    if (sim.PlatesDirty > plateMaxDirty) plateMaxDirty = sim.PlatesDirty;
                    if (sim.ServiceComplete) break;
                }
                plateBlocked += sim.PlateBlockedTicks;
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

                DayReport r = sim.BuildDayReport();
                strategy.OnEvening(sim, r);

                peopleServed += r.ServedPeople;
                plannedParties += r.PlannedParties;
                servedParties += r.ServedParties;
                turnedAway += r.TurnedAwayParties;
                totalRevenue += r.Revenue;
                totalIngredients += r.IngredientCost;
                // GRUP sayiliyor, kisi degil - alanin adi da oyle.
                // Eskiden "peopleLost" deniyordu ve Warnings() bunu
                // KISI sayisiyla karsilastiriyordu: makul oyuncuda
                // 2.042 kisi / 16 grup, esik 408 - koruma matematiksel
                // olarak hic atesleyemezdi.
                //
                // Ayrica kapidan donenler ARTIK AYRI: masadan kizgin
                // ayrilan bir servis sorunu, kapidan donen bir kapasite
                // sorunu. Ikisi tek sayiya katlanirsa hangi derdin
                // buyudugu okunamiyor.
                partiesLost += r.AngrySeatedParties;

                // "Para sorun olmaktan cikti": kasa, geriye kalan BUTUN
                // satin alinabilirleri tek seferde odeyebiliyorsa oyuncunun
                // biriktirecegi bir sey kalmamis demektir.
                //
                // Iki eski tanim da yanlisti. Once "en pahali genislemenin
                // uc kati" deniyordu; ekipmani hic saymiyordu. Sonra ayni
                // esige ekipman eklendi ama "uc kat" keyfi kaldi: elde
                // 30.000 varken 20.000'lik iki ekipman duruyorsa para hala
                // onemli.
                long remaining = sim.RemainingPurchaseCost();
                if (trivialWeek == 0 && remaining > 0 && sim.Cash > remaining)
                    trivialWeek = (day + 6) / 7;

                if (collapseDay == 0 && sim.ReputationCenti < CollapseCenti)
                    collapseDay = day;

                rows.Append(strategy.Name).Append(',').Append(seed).Append(',')
                    .Append(day).Append(',').Append(sim.TableCount).Append(',')
                    .Append(sim.Cooks).Append(',').Append(sim.SalonStaff).Append(',')
                    .Append(r.PlannedParties).Append(',').Append(r.ServedParties).Append(',')
                    .Append(r.AngrySeatedParties).Append(',').Append(r.ServedPeople).Append(',')
                    .Append(r.Revenue).Append(',').Append(r.AverageSatisfactionCenti).Append(',')
                    .Append(r.ReputationCenti).Append(',').Append(r.Cash).AppendLine();

                sim.AdvanceToNextDay();
            }

            // CIRO KUMULATIF OKUNUYOR, gun raporlarindan toplanmiyor.
            //
            // Toplama bir sinir artigi uretiyordu: AdvanceToNextDay()
            // vadesi gelen veresiyeyi tahsil ediyor ama gunun raporu
            // ondan once aliniyor, yani son gunun tahsilati hicbir
            // rapora girmiyor ve mutabakat kapanmiyordu. Kuyrugu elle
            // eklemek de tam tutmadi; dogru cevap, ucret ve kirada
            // oldugu gibi simulasyonun kendi kumulatif sayacini
            // okumak. Boylece sinir diye bir sey kalmiyor.
            totalRevenue = sim.TotalRevenue;

            agg.Add(sim, peopleServed, partiesLost, trivialWeek, totalRevenue,
                    totalIngredients, collapseDay);
            agg.AddFlow(plannedParties, servedParties, turnedAway);
            agg.AddPlates(plateBlocked,
                          plateMinClean == int.MaxValue ? 0 : plateMinClean,
                          plateMaxDirty);
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// TABAK BASINCI TABLOSU.
        ///
        /// docs/14 bulasikciyi bir darbogaz olarak tarif ediyor. Bir
        /// darbogazin "var olmasi" yetmez, bir yerde HISSEDILMESI gerekir -
        /// hissedilmeyen mekanik dekorasyondur ve bu projede en kotu
        /// sonuctur. Bu tablo tam o soruyu soruyor.
        /// </summary>
        private static void PlateReport(List<StrategyResult> results)
        {
            Console.WriteLine();
            Console.WriteLine("=== tabak basinci (60 gun, ortalama) ===");
            Console.WriteLine("| strateji      | tabaksiz bekleme | en az temiz | en cok kirli |");
            Console.WriteLine("|---------------|-----------------:|------------:|-------------:|");
            foreach (StrategyResult r in results)
                Console.WriteLine($"| {r.Name,-13} | {r.AvgPlateBlocked,16:0} | "
                                  + $"{r.AvgPlateMinClean,11:0.0} | {r.AvgPlateMaxDirty,12:0.0} |");
        }

        private static void Report(List<StrategyResult> results)
        {
            Console.WriteLine("| strateji      | son kasa | defter | itibar | masa | kadro | servis | kayip | bosaldi | ilk borc | onemsiz |");
            Console.WriteLine("|---------------|---------:|-------:|-------:|-----:|------:|-------:|------:|--------:|---------:|--------:|");
            foreach (StrategyResult r in results)
            {
                Console.WriteLine(
                    $"| {r.Name,-13} | {Coin(r.AvgFinalCash),8} | " +
                    $"{(r.AvgOpenCredit > 0 ? Coin(r.AvgOpenCredit) : "-"),6} | " +
                    $"{r.AvgReputation / 100,6:0.0} | " +
                    $"{r.AvgTables,4:0.0} | {r.AvgStaff,5:0.0} | {r.AvgServed,6:0} | " +
                    $"{r.AvgLost,5:0} | {(r.AvgCollapseDay > 0 ? r.AvgCollapseDay.ToString("0") : "-"),7} | " +
                    $"{(r.AvgFirstDebtDay > 0 ? r.AvgFirstDebtDay.ToString("0") : "-"),8} | " +
                    $"{(r.AvgTrivialWeek > 0 ? r.AvgTrivialWeek.ToString("0.0") : "-"),7} |");
            }

            Console.WriteLine();
            Console.WriteLine("=== 60 gunun gelir tablosu (ortalama, sikke) ===");
            // MUTABAKAT SUTUNU var: net ile gercek kasa hareketi
            // arasindaki fark. Sifir olmali.
            //
            // Once zayiat, ekipman ve genisleme satirlari YOKTU ve "net"
            // gercek kasa hareketinin 2-20 kati cikiyordu. Uc tasarim
            // sorusunun cevabi ters isaretliydi: arac "yuksek fiyat yine
            // de kazandiriyor" diyordu, gercekte kaybettiriyordu. Bu
            // tablolara bakarak alinmis her denge karari supheliydi.
            Console.WriteLine("| strateji      |    ciro | kurtarma | malzeme |  zayiat |"
                              + "  cay |    maas |    kira | yatirim |     net |  fark |");
            Console.WriteLine("|---------------|--------:|---------:|--------:|--------:|"
                              + "-----:|--------:|--------:|--------:|--------:|------:|");
            foreach (StrategyResult r in results)
            {
                // Mutabakat MALZEME HARCAMASIYLA, satilan malin
                // maliyetiyle degil: kasadan cikan para satin almadir.
                // Zayiat ayri bir sutun ve NET'TEN DUSULMUYOR - o para
                // zaten satin alirken cikti; zayiat, alinan malin ne
                // kadarinin bosa gittigini gosteren bir BILGI satiri.
                double ing = r.AvgIngredientSpend;
                double invest = r.AvgEquipment + r.AvgExpansion;

                // Kurtarma ve kredi de NAKIT HAREKETI: batma merdiveni
                // ekipman satiyor, dukkan kuculuyor ve kalan borc
                // siliniyor - hepsi kasaya para sokuyor. Gorunmezse
                // mutabakat tutmuyor ve fark aciklanamiyor.
                double inflow = r.AvgRescue + r.AvgLoan;

                // Kredi taksitleri kira sutununda DEGIL: TotalRentPaid
                // yalnizca kirayi sayiyor. Ayri bir cikis olarak
                // dusulmezse mutabakat kredinin geri odemesi kadar
                // sapiyor - makul oyuncuda 6.117 sikke.
                // CAY DA BIR GIDER.
                //
                // Veresiye acilirken ikram edilen cayin bedeli kasadan
                // cikiyor ve HICBIR sutunda sayilmiyordu: Turk
                // mutfaginda imzaci botun farki tam olarak o kadardi
                // (134 sikke). Gorunmez bir gider, mutabakati bozmakla
                // kalmiyor - mekanigi oldugundan ucuz gosteriyor.
                double net = r.AvgRevenue + inflow - ing - r.AvgWages
                             - r.AvgRent - invest - r.AvgLoanRepaid - r.AvgTea;

                // Kasa hareketi: baslangic kasasindan bugune.
                //
                // ACIK VERESIYE BURAYA GIRMIYOR. Bir zamanlar ekleniyordu
                // ve mutabakati BOZUYORDU: veresiyeye yazilan fis
                // _revenue'ya hic girmiyor (ancak tahsil edilince
                // giriyor), yani ciro tarafinda karsiligi yok. Turk
                // mutfaginda imzaci oyuncunun farki -2.441 cikiyordu ve
                // sutun "sifir olmali" diyordu.
                //
                // Acik veresiye kaybolmus degil ama HENUZ GELIR DEGIL:
                // ne kasada, ne ciroda. Mutabakat ikisini de saymayinca
                // kapaniyor.
                double moved = r.AvgFinalCash - r.StartCash;
                double gap = net - moved;

                // Fark SIFIR OLMALI ve olmadigini soyleyen bir sey
                // olmali: sutun uzun sure -2.441 yaziyordu ve hicbir
                // uyari cikmiyordu, cunku Warnings() farka hic bakmiyordu.
                // Kendi kirildigini soylemeyen bir olcum araci, yanlis
                // olcumu dogru sanmaktan daha kotu.
                if (System.Math.Abs(gap) > 100)
                    r.Reconciliation = gap;

                Console.WriteLine(
                    $"| {r.Name,-13} | {Coin(r.AvgRevenue),7} | {Coin(inflow),8} | " +
                    $"{Coin(ing),7} | {Coin(r.AvgSpoiled),7} | {Coin(r.AvgTea),5} | " +
                    $"{Coin(r.AvgWages),7} | " +
                    $"{Coin(r.AvgRent),7} | {Coin(invest),7} | {Coin(net),7} | " +
                    $"{Coin(gap),5} |");
            }

            // YIL SONU PUANI: kasadan BASKA bir cevap.
            //
            // docs/08 kampanyayi yedi eksende puanliyor ve oyuncunun
            // gordugu sonuc bu. "Son kasa" tek basina yaniltici: eksik
            // kadroyla calisip parayi biriktiren oyuncu itibar, ekip ve
            // mudavim eksenlerinde kaybediyor olabilir - ya da
            // olmayabilir. Tablo o soruyu cevapliyor.
            Console.WriteLine();
            Console.WriteLine("=== yil sonu puani (docs/08, 0-100) ===");
            Console.WriteLine("| strateji      | puan | varlik | itibar | mudavim "
                              + "| ekip | mekan | saglam | imza | kombo% |");
            Console.WriteLine("|---------------|-----:|-------:|-------:|--------:"
                              + "|-----:|------:|-------:|-----:|-------:|");
            foreach (StrategyResult r in results)
                Console.WriteLine(
                    $"| {r.Name,-13} | {r.AvgScore,4:0} | {r.AvgScoreWealth,6:0} | "
                    + $"{r.AvgScoreRep,6:0} | {r.AvgScoreRegulars,7:0} | "
                    + $"{r.AvgScoreCrew,4:0} | {r.AvgScorePlace,5:0} | "
                    + $"{r.AvgScoreResilience,6:0} | {r.AvgScoreSignature,4:0} | "
                    + $"{r.AvgComboShareBp / 100,6:0.0} |");

            Console.WriteLine();
            Console.WriteLine("=== docs/12 8: aracin cevapladigi sorular ===");
            foreach (StrategyResult r in results)
            {
                Console.WriteLine($"\n{r.Question}");
                Console.WriteLine($"  -> {r.Verdict()}");
            }

            // MUDAHALE GERCEKTEN OLDU MU.
            //
            // "Reddedilen bir bot, bot degildir": fiyat tavani gelince
            // yuksek_fiyat botunun komutlari reddediliyordu ve bot
            // sessizce makul oyuncunun kopyasi olmustu. Mudahalenin
            // KAZANDIRIP kazandirmadigini sormadan once, mudahalenin
            // olup olmadigi sorulmali.
            // KOL BASINA. Tek satirda basilirken iki kolun toplamiydi
            // ve hangi kolun kac mudahalesinin gectigi okunamiyordu -
            // oysa sayaclarin yazilma sebebi tam olarak buydu.
            Console.WriteLine();
            Console.WriteLine("=== mudahale (strateji basina) ===");
            bool hicMudahale = false;
            foreach (StrategyResult r in results)
            {
                if (r.InterventionsTried == 0) continue;
                hicMudahale = true;
                Console.WriteLine($"  {r.Name,-20} {r.InterventionsApplied,6} gecti / "
                                  + $"{r.InterventionsTried,6} denendi");
            }
            if (!hicMudahale) Console.WriteLine("  yok");

            Console.WriteLine();
            Console.WriteLine("=== Uyarilar ===");
            int warnings = 0;
            foreach (StrategyResult r in results)
            {
                foreach (string w in r.Warnings())
                {
                    Console.WriteLine("  ! " + w);
                    warnings++;
                }
            }
            if (warnings == 0) Console.WriteLine("  yok");
        }

        private static string Coin(double centi)
        {
            return (centi / 100.0).ToString("N0", CultureInfo.InvariantCulture);
        }

        // -------------------------------------------------------------------
        private static string FindRoot()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "Lokanta.slnx"))
                    || File.Exists(Path.Combine(d.FullName, "Lokanta.sln")))
                    return d.FullName;
                d = d.Parent;
            }
            throw new InvalidOperationException("Depo koku bulunamadi");
        }

        /// <summary>
        /// Tanidigimiz butun bayraklar. Baskasi varsa hata.
        ///
        /// "--nologo" bizim degil: "dotnet run --nologo" onu tanimadigi
        /// icin uygulamaya GECIRIYOR. Denetimin ilk kosusunda kalibrasyon
        /// aracinin tamami bu yuzden durdu; bayragi listeye almak, kendi
        /// koruma araclarimizin birbirini engellememesi icin.
        /// </summary>
        private static readonly string[] KnownFlags =
        {
            "--seeds", "--days", "--csv", "--mutfak", "--strateji",
            "--depo-tavan", "--solve",
            "--nologo",
        };

        // "--solve" LISTEDE OLMADIGI ICIN RentSolver ERISILEMEZDI:
        // CheckArgs bayragi tanimiyor, arac "Bilinmeyen bayrak" deyip 2
        // ile cikiyordu. Kira cozucu yazildi, hic calistirilamadi.
        //
        // "--tohum" LISTEDEN CIKTI: kabul ediliyordu ama kodda tek bir
        // okuma yoktu. "--tohum 3" yazan kisi hata almiyor, tam kosuyu
        // aliyor ve filtreledigini saniyordu - CheckArgs'in kendi
        // yorumundaki tehlikenin aynisi, listenin KENDI ICINDE.

        private static bool CheckArgs(string[] a)
        {
            bool ok = true;
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].StartsWith("--", StringComparison.Ordinal)) continue;

                bool known = false;
                for (int k = 0; k < KnownFlags.Length; k++)
                    if (a[i] == KnownFlags[k]) { known = true; break; }

                if (!known)
                {
                    Console.Error.WriteLine("Bilinmeyen bayrak: " + a[i]);
                    ok = false;
                }
            }
            if (!ok)
                Console.Error.WriteLine("Taninan bayraklar: " + string.Join(" ", KnownFlags));
            return ok;
        }

        private static int ArgInt(string[] a, string name, int fallback)
        {
            for (int i = 0; i + 1 < a.Length; i++)
                if (a[i] == name && int.TryParse(a[i + 1], NumberStyles.Integer,
                                                 CultureInfo.InvariantCulture, out int v))
                    return v;
            return fallback;
        }

        private static string ArgStr(string[] a, string name, string fallback)
        {
            for (int i = 0; i + 1 < a.Length; i++)
                if (a[i] == name) return a[i + 1];
            return fallback;
        }
    }

    // ======================================================================
    public sealed class StrategyResult
    {
        public string Name { get; }
        public string Question { get; }

        private long _cash, _reputation, _tables, _staff, _served, _lost, _openCredit;
        private long _spoiled, _equipment, _expansion, _ingredientSpend;
        private long _rescue, _loan, _loanRepaid;
        private int _debtDays, _debtCount, _trivialWeeks, _trivialCount, _runs;
        private int _collapseDays, _collapseCount;

        public double AvgFinalCash, AvgReputation, AvgTables, AvgStaff, AvgServed, AvgLost;
        /// <summary>Kosu basina agirlanan GRUP sayisi; AvgLost ile ayni birim.</summary>
        public double AvgServedParties;

        /// <summary>Ana yemeklerin yuzde kaci komboya dondu (bin-puan).</summary>
        public double AvgComboShareBp;
        private long _comboShareBp;

        /// <summary>Bu stratejinin kendi mudahale sayaclari.</summary>
        public int InterventionsTried, InterventionsApplied;
        /// <summary>
        /// Altmisinci gunde hala DEFTERDE duran para. Kasada degil ama
        /// kaybolmus da degil - docs/08 yil sonu degerlendirmesi net
        /// varligi olcuyor, ve tahsil edilmemis veresiye net varligin
        /// parcasi. Ayri sutun cunku "son kasa" hedefleri buna gore
        /// kalibre edilmedi.
        /// </summary>
        public double AvgOpenCredit;
        public double AvgFirstDebtDay, AvgTrivialWeek;
        /// <summary>Itibarin 20 puanin altina indigi gun; dukkanin bosaldigi an.</summary>
        public double AvgCollapseDay;
        public double AvgRevenue, AvgWages, AvgRent, AvgTicket, AvgIngredients;
        public double AvgSpoiled, AvgEquipment, AvgExpansion, AvgIngredientSpend;

        /// <summary>Veresiye cayinin bedeli - GORUNMEYEN bir giderdi.</summary>
        public double AvgTea;
        private long _teaSpend;

        /// <summary>Gelir tablosu mutabakat farki. Sifir disi = arac kirik.</summary>
        public double Reconciliation;
        public double AvgRescue, AvgLoan, AvgLoanRepaid;
        public double StartCash;
        public double DebtShare;

        public StrategyResult(string name, string question)
        {
            Name = name; Question = question;
        }

        private long _revenue, _wages, _rent, _ingredients;
        private long _planned, _servedP, _turned;

        public double ServiceRate => _planned > 0 ? (double)_servedP / _planned : 0;
        public double TurnAwayRate => _planned > 0 ? (double)_turned / _planned : 0;

        /// <summary>Talebin ne kadari agirlandi. Kapali form model %100 varsayiyor.</summary>
        public void AddFlow(long planned, long served, long turned)
        {
            _planned += planned; _servedP += served; _turned += turned;
        }

        private long _score, _scoreWealth, _scoreRep, _scoreRegulars;
        private long _scoreCrew, _scorePlace, _scoreResilience, _scoreSignature;

        /// <summary>Yil sonu puani (0-100) ve yedi ekseni.</summary>
        public double AvgScore { get { return _runs == 0 ? 0 : (double)_score / _runs; } }
        public double AvgScoreWealth { get { return _runs == 0 ? 0 : (double)_scoreWealth / _runs; } }
        public double AvgScoreRep { get { return _runs == 0 ? 0 : (double)_scoreRep / _runs; } }
        public double AvgScoreRegulars { get { return _runs == 0 ? 0 : (double)_scoreRegulars / _runs; } }
        public double AvgScoreCrew { get { return _runs == 0 ? 0 : (double)_scoreCrew / _runs; } }
        public double AvgScorePlace { get { return _runs == 0 ? 0 : (double)_scorePlace / _runs; } }
        public double AvgScoreResilience { get { return _runs == 0 ? 0 : (double)_scoreResilience / _runs; } }
        public double AvgScoreSignature { get { return _runs == 0 ? 0 : (double)_scoreSignature / _runs; } }

        private long _plateBlocked, _plateMinClean, _plateMaxDirty;
        private int _plateRuns;

        /// <summary>Tabak basinci: darbogaz isiriyor mu.</summary>
        public void AddPlates(int blocked, int minClean, int maxDirty)
        {
            _plateBlocked += blocked;
            _plateMinClean += minClean;
            _plateMaxDirty += maxDirty;
            _plateRuns++;
        }

        public double AvgPlateBlocked
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateBlocked / _plateRuns; }
        }
        public double AvgPlateMinClean
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateMinClean / _plateRuns; }
        }
        public double AvgPlateMaxDirty
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateMaxDirty / _plateRuns; }
        }

        public void Add(Simulation sim, long served, long lost, int trivialWeek,
                        long revenue, long ingredients, int collapseDay)
        {
            _runs++;
            if (collapseDay > 0) { _collapseDays += collapseDay; _collapseCount++; }
            _revenue += revenue;
            _ingredients += ingredients;
            _wages += sim.TotalWagesPaid;
            _rent += sim.TotalRentPaid;
            _cash += sim.Cash;
            _spoiled += sim.SpoiledValue;
            _teaSpend += sim.TeaSpend;
            _ingredientSpend += sim.IngredientSpend;
            _rescue += sim.RescueValue;
            _loan += sim.LoanTaken;
            _loanRepaid += sim.LoanRepaid;
            _equipment += sim.EquipmentSpend;
            _expansion += sim.ExpansionSpend;
            _openCredit += sim.OpenCredit;
            // YIL SONU PUANI: docs/08'in asil sonucu.
            //
            // Bu sutun bir IDDIAYI olcmek icin eklendi. docs/42'de
            // "eksik kadro parayi kazaniyor, PUANI kaybediyor" diye
            // yazmistim - ve o cumle olculmemisti. Bu projenin kurali
            // acik: olculmemis bir cumle, belgede duran bir tahmindir.
            SeasonScore puan = sim.Score();
            _score += puan.Total;
            _scoreWealth += puan.Wealth;
            _scoreRep += puan.Reputation;
            _scoreRegulars += puan.Regulars;
            _scoreCrew += puan.Crew;
            _scorePlace += puan.Place;
            _scoreResilience += puan.Resilience;
            _scoreSignature += puan.Signature;

            // HAM KOMBO ORANI. Imza ekseninin HEDEFI bu olcumden
            // gelecek; uydurulmus bir hedef ekseni ya doygun ya
            // erisilmez yapar.
            _comboShareBp += sim.ComboShareBp;

            _reputation += sim.ReputationCenti;
            _tables += sim.TableCount;
            _staff += sim.Cooks + sim.SalonStaff;
            _served += served;
            _lost += lost;
            if (sim.FirstDebtDay > 0) { _debtDays += sim.FirstDebtDay; _debtCount++; }
            if (trivialWeek > 0) { _trivialWeeks += trivialWeek; _trivialCount++; }
        }

        public void Finish(int seeds)
        {
            if (_runs == 0) return;
            AvgFinalCash = (double)_cash / _runs;
            AvgOpenCredit = (double)_openCredit / _runs;
            AvgReputation = (double)_reputation / _runs;
            AvgTables = (double)_tables / _runs;
            AvgStaff = (double)_staff / _runs;
            AvgServed = (double)_served / _runs;
            AvgLost = (double)_lost / _runs;
            AvgServedParties = (double)_servedP / _runs;
            AvgComboShareBp = (double)_comboShareBp / _runs;
            AvgRevenue = (double)_revenue / _runs;
            AvgIngredients = (double)_ingredients / _runs;
            AvgSpoiled = (double)_spoiled / _runs;
            AvgTea = (double)_teaSpend / _runs;
            AvgIngredientSpend = (double)_ingredientSpend / _runs;
            AvgRescue = (double)_rescue / _runs;
            AvgLoan = (double)_loan / _runs;
            AvgLoanRepaid = (double)_loanRepaid / _runs;
            AvgEquipment = (double)_equipment / _runs;
            AvgExpansion = (double)_expansion / _runs;
            AvgWages = (double)_wages / _runs;
            AvgRent = (double)_rent / _runs;
            AvgTicket = _served > 0 ? (double)_revenue / _served : 0;
            AvgFirstDebtDay = _debtCount > 0 ? (double)_debtDays / _debtCount : 0;
            AvgTrivialWeek = _trivialCount > 0 ? (double)_trivialWeeks / _trivialCount : 0;
            AvgCollapseDay = _collapseCount > 0 ? (double)_collapseDays / _collapseCount : 0;
            DebtShare = (double)_debtCount / _runs;
        }

        public string Verdict()
        {
            string cash = (AvgFinalCash / 100.0).ToString("N0", CultureInfo.InvariantCulture);
            if (DebtShare > 0.5)
                return $"kosularin %{DebtShare * 100:0}'i borca dustu, ortalama {AvgFirstDebtDay:0}. gunde; " +
                       $"son kasa {cash}";
            return $"son kasa {cash}, itibar {AvgReputation / 100:0.0}, " +
                   $"{AvgServed:0} kisi agirlandi, {AvgLost:0} grup masadan kizgin ayrildi";
        }

        public IEnumerable<string> Warnings()
        {
            // docs/12 8.3: ekonomi kacinci haftada onemsizlesiyor
            if (AvgTrivialWeek > 0 && AvgTrivialWeek < 8)
                yield return $"{Name}: para {AvgTrivialWeek:0.0}. haftada sorun olmaktan cikiyor " +
                             "(hedef: 8. haftadan once olmamali)";

            if (Name == "makul" && DebtShare > 0.2)
                yield return $"makul oyuncu kosularin %{DebtShare * 100:0}'inde borca dusuyor; " +
                             "ekonomi fazla sert";

            // ESIK AYNI BIRIMDEN OLMALI.
            //
            // AvgLost GRUP, AvgServed KISI sayiyor. Eski satir ikisini
            // dogrudan karsilastiriyordu: olculen kosuda 2.042 kisi / 16
            // grup, yani esik 408 - koruma hic atesleyemezdi. Ustelik
            // metin "dortte bir" diyor, matematik "beste bir" yapiyordu.
            //
            // Simdi grup grupla karsilastiriliyor ve esik metinle uyumlu.
            if (Name == "makul" && AvgServedParties > 0
                && AvgLost > AvgServedParties / 4)
                yield return $"makul oyuncu masaya oturan gruplarin dortte birinden "
                             + $"fazlasini kizgin ugurluyor ({AvgLost:0} / {AvgServedParties:0})";

            // ZAYIAT gorunur bir uyari.
            //
            // Gelir tablosuna eklenene kadar hicbir yerde sayilmiyordu ve
            // "net" sutunu gercek kasa hareketinin katlariydi. Simdi
            // sayiliyor ve ortaya cikan sey su: iyi oynayan bir oyuncu bile
            // aldigi malzemenin yarisini cope atiyor. Bu bir denge sorusu
            // ve en azindan gorunmesi gerekiyor.
            // HER STRATEJI ICIN. Once yalnizca "makul" icin bakiliyordu
            // ve asil ihlal edenler sessizdi: olculdu - Turk mutfaginda
            // sadece_hal %50,6, fazla_kadro %47,7, yuksek_fiyat %65,7
            // zayiat veriyor ve arac "Uyarilar: yok" yaziyordu, cunku
            // makul %29 ile esigin altindaydi.
            if (AvgIngredientSpend > 0 && AvgSpoiled > AvgIngredientSpend * 0.4)
                yield return $"{Name} aldigi malzemenin "
                             + $"%{AvgSpoiled * 100 / AvgIngredientSpend:0}'ini cope atiyor";

            // Arac kendi kirildigini SOYLEMELI. Fark sutunu uzun sure
            // -2.441 yaziyordu ve hicbir uyari cikmiyordu.
            if (Reconciliation != 0)
                yield return $"{Name} gelir tablosu TUTMUYOR: fark "
                             + $"{Reconciliation / 100.0:N0} sikke";

            if (Name == "pasif" && DebtShare < 0.5)
                yield return "pasif oyuncu hic borca dusmuyor; "
                             + "mudahale etmemenin bedeli yok";
        }
    }
}
