using Lokanta.Core.Content;
using Lokanta.Core.Sim;

namespace Lokanta.Game
{
    /// <summary>Bildirimin tonu. Rengi ve sesi buradan geliyor.</summary>
    public enum NoticeTone
    {
        Info,
        Good,
        Warn,
        Bad,
    }

    /// <summary>
    /// Simulasyon olaylarini OYUNCUNUN OKUYACAGI CUMLEYE cevirir.
    ///
    /// Bu sinif olmadan oyun sagirdi. Cekirdek otuz uc tur olay uretiyor
    /// (docs/23 6.3) ve gorunum katmani bunlarin yalnizca yedisini
    /// okuyup SES caliyordu: sabir uyarisi, stok bitmesi, istifa, gecikmis
    /// maas, yanan veresiye, kusen musteri - hepsi hesaplaniyor ve
    /// atiliyordu. Oyuncu kizgin musterisinin neden kizdigini
    /// ogrenemiyordu.
    ///
    /// Burada YALNIZCA metin uretiliyor. Olay -> cumle; karar yok, durum
    /// yok. Sik tekrarlayan olaylar (oturma, siparis, odeme) bilerek
    /// disarida: bir bildirim seridi, dakikada kirk satir akarsa okunmuyor.
    /// </summary>
    public static class Notices
    {
        /// <summary>
        /// Olayin bildirim metni. Bildirimi olmayan olaylar icin false.
        /// </summary>
        public static bool Describe(in SimEvent e, ContentSet content, Simulation sim,
                                    out string text, out NoticeTone tone)
        {
            text = null;
            tone = NoticeTone.Info;

            switch (e.Kind)
            {
                // --- salon ---------------------------------------------------
                case SimEventKind.CustomerLeftAngry:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.angry", StageReason(e.B));
                    return true;

                case SimEventKind.PlatesOut:
                    // SEBEBI VE CARESI ayni cumlede: "neden durdu" ve
                    // "ne yapmaliyim". Bulasikci varsa care farkli
                    // olmali - ona "bulasikci tut" demek, olmayan bir
                    // dugmeyi aramasina yol acardi.
                    tone = NoticeTone.Warn;
                    text = e.B > 0
                        ? Loc.T("notice.plates_out_busy", e.A)
                        : Loc.T("notice.plates_out", e.A);
                    return true;

                // YEMEK ADI YAZILMIYOR: olay bir yemegin bitmesi degil,
                // musterinin menuede yapilabilir HICBIR ana yemek
                // bulamamasi. Eskiden e.A yemek sanilip isim basiliyordu;
                // e.A parti indisi oldugu icin ilgisiz bir yemek cikiyordu.
                case SimEventKind.TurnedAway:
                    tone = NoticeTone.Warn;
                    text = Loc.T("notice.turned_away");
                    return true;

                case SimEventKind.DishRequested:
                    tone = NoticeTone.Warn;
                    text = Loc.T("notice.requested", Dish(content, e.A));
                    return true;

                case SimEventKind.DishUnlocked:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.unlocked", Dish(content, e.A));
                    return true;

                // --- duzenli musteriler --------------------------------------
                case SimEventKind.RegularVisited:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.regular", Regular(content, e.A));
                    return true;

                case SimEventKind.RegularUpset:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.regular_upset", Regular(content, e.A), e.B);
                    return true;

                // --- kadro ---------------------------------------------------
                case SimEventKind.StaffResigned:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.resigned", Who(sim, e.A, e.B));
                    return true;

                case SimEventKind.StaffLeveledUp:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.level_up", Who(sim, e.A, 0), e.B);
                    return true;

                case SimEventKind.StaffTenure:
                    // SATIR MUTFAGA GORE.
                    //
                    // Esnaf lokantasinda iliski USTAYA ve ise; zincirde
                    // VARDIYAYA ve sisteme (docs/53). Tek satir ikisini
                    // de genel yapardi.
                    //
                    // Ayirt eden sey self servis bayragi - mutfak adini
                    // burada okumak, ucuncu bir yere "hangi mutfak
                    // hangisi" bilgisi yazmak olurdu.
                    //
                    // e.A havuz, e.B SIRA. Gun sayisi olayda degil:
                    // sabit (Simulation.TenureDays) ve iki yere yazmak
                    // bu projede bes kez sessizce ayristi.
                    tone = NoticeTone.Good;
                    text = Loc.T(
                        content != null && content.SelfService
                            ? "notice.tenure_zincir" : "notice.tenure_lokanta",
                        Who(sim, e.A, e.B), Simulation.TenureDays);
                    return true;

                // --- para ----------------------------------------------------
                case SimEventKind.WeeklyCostsPaid:
                    tone = NoticeTone.Info;
                    text = Loc.T("notice.weekly", Loc.Money(e.A), Loc.Money(e.B));
                    return true;

                case SimEventKind.WagesLate:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.wages_late", Loc.Money(e.B));
                    return true;

                case SimEventKind.CashWentNegative:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.debt", Loc.Money(e.B));
                    return true;

                // --- veresiye ------------------------------------------------
                case SimEventKind.CreditExtended:
                    tone = NoticeTone.Info;
                    text = Loc.T("notice.credit_given", Loc.Money(e.B));
                    return true;

                case SimEventKind.CreditCollected:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.credit_paid", Loc.Money(e.A));
                    return true;

                case SimEventKind.CreditDefaulted:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.credit_lost", Loc.Money(e.A));
                    return true;

                // --- yatirim -------------------------------------------------
                case SimEventKind.EquipmentBought:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.equipment", Station(content, e.A), e.B);
                    return true;

                case SimEventKind.StorageBought:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.storage", e.A);
                    return true;

                // --- batma merdiveni -----------------------------------------
                //
                // Bunlar OYUNCUNUN ISTEMEDIGI seyler ve tam da bu yuzden
                // gorunmeleri sart: ekipmani satilan, kuculen bir dukkan
                // sessizce kucuk kalirsa oyuncu sebebini hic ogrenemez.
                case SimEventKind.EquipmentSold:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.equipment_sold", Station(content, e.A), e.B);
                    return true;

                case SimEventKind.Downsized:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.downsized", e.A);
                    return true;

                case SimEventKind.StationRushed:
                    tone = NoticeTone.Info;
                    text = Loc.T("notice.rushed", Station(content, e.A), e.B);
                    return true;

                // --- reddedilen komut ----------------------------------------
                //
                // Bunu gostermek sart: bir dugmeye basip hicbir sey
                // olmamasi, oyuncuya oyunun bozuk oldugunu dusunduruyor.
                case SimEventKind.CommandRejected:
                    tone = NoticeTone.Warn;
                    // SEBEP DE SOYLENIYOR. Tek bir jenerik cumle, birbirine
                    // hic benzemeyen durumlari ayni gosteriyordu: parasi
                    // yetmeyen oyuncu ile gunluk komut hakkini bitiren
                    // oyuncu ayni yaziyi okuyor ve ikisi de ne
                    // yapacagini bilmiyordu.
                    //
                    // B alani red sebebi (Simulation.Emit'in ikinci
                    // sayisi): 9 para yetmedi, 12 gunluk komut hakki.
                    if (e.B == 9) text = Loc.T("notice.rejected_cash");
                    else if (e.B == 12) text = Loc.T("notice.rejected_budget");
                    else if (e.B == 16) text = Loc.T("notice.rejected_book");
                    else text = Loc.T("notice.rejected");
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Musteri hangi asamada birakti. "Kizdi" tek basina bilgi degil;
        /// oyuncunun BIR SONRAKI gun ne degistirecegini bu soyluyor.
        /// </summary>
        private static string StageReason(int stage)
        {
            switch ((CustomerStage)stage)
            {
                case CustomerStage.WaitingForTable: return Loc.T("notice.why.table");
                case CustomerStage.WaitingToOrder:  return Loc.T("notice.why.order");
                case CustomerStage.WaitingForFood:  return Loc.T("notice.why.food");
                default:                            return Loc.T("notice.why.other");
            }
        }

        private static string Role(int pool)
        {
            return Loc.T(pool == 0 ? "role.asci" : "role.garson");
        }

        /// <summary>
        /// Kisinin ADI, yoksa rolu.
        ///
        /// Istifa olayinda kisi ARTIK KADRODA DEGIL, yani adini sormak
        /// bos donuyor. O yuzden rol yedek olarak duruyor: "Asci birakti"
        /// bilgisiz degil, sadece soguk.
        /// </summary>
        private static string Who(Simulation sim, int pool, int index)
        {
            if (sim != null)
            {
                string name = sim.StaffName(pool, index);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return Role(pool);
        }

        private static string Dish(ContentSet content, int dish)
        {
            if (content == null || dish < 0 || dish >= content.Dishes.Length)
                return Loc.T("notice.unknown");
            return Loc.T(content.Dishes[dish].NameKey);
        }

        private static string Station(ContentSet content, int station)
        {
            if (content == null || station < 0 || station >= content.Stations.Length)
                return Loc.T("notice.unknown");
            return Loc.T("station." + content.Stations[station].Id);
        }

        private static string Regular(ContentSet content, int index)
        {
            if (content == null || index < 0 || index >= content.Regulars.Length)
                return Loc.T("notice.unknown");
            return Loc.T(content.Regulars[index].NameKey);
        }
    }
}
