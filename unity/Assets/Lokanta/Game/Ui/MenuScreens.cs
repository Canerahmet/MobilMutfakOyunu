using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// Icerik yuklenemedi. Oyun ACILMIYOR (docs/23 9.1) ve sebebi
    /// EKRANDA yaziyor - kullanici Unity gunlugu okumak zorunda degil.
    /// </summary>
    public sealed class ErrorScreen : UiScreen
    {
        private readonly string _message;
        public ErrorScreen(string message) { _message = message; }

        public override VisualElement Build()
        {
            VisualElement root = Backdrop();
            VisualElement card = Theme.PanelBox();
            card.style.maxWidth = 640;
            card.style.alignSelf = Align.Center;
            card.style.marginTop = 80;

            Label title = Theme.Title(Loc.T("ui.error.title"));
            title.style.color = Theme.Bad;
            card.Add(title);
            card.Add(Theme.Text(Loc.T("ui.error.content"),
                                Theme.FontBody, Theme.InkDim));
            card.Add(Theme.Divider());

            Label detail = Theme.Text(_message, Theme.FontSmall, Theme.Warn);
            detail.style.whiteSpace = WhiteSpace.Normal;
            card.Add(detail);

            root.Add(card);
            return root;
        }

        /// <summary>
        /// Menu sayfalarinin ortak zemini. KAYDIRILABILIR.
        ///
        /// Once duz bir kutuydu ve icerik dikey ORTALANIYORDU. Sigmadiginda
        /// iki ucundan birden kesiliyor ve kaydirma da olmadigi icin
        /// erisilemez hale geliyordu: yuva ekraninda dorduncu kayit yuvasi
        /// ve "Geri" dugmesi ekran disinda kaliyordu - yani oyuncu oyuna
        /// hic giremiyordu. Ustelik bu, oyunun IKINCI ekrani.
        ///
        /// Ortalama yalnizca icerik sigdiginda guvenli, ve sigip
        /// sigmadigini yerlesim onceden bilmiyor.
        /// </summary>
        internal static VisualElement Backdrop()
        {
            ScrollView v = Theme.Mobile(new ScrollView(ScrollViewMode.Vertical));
            v.style.flexGrow = 1;
            v.style.minHeight = 0;
            v.style.backgroundColor = Theme.Bg;
            v.style.paddingLeft = Theme.Pad * 2;
            v.style.paddingRight = Theme.Pad * 2;
            v.style.paddingTop = Theme.Pad * 2;
            v.style.paddingBottom = Theme.Pad * 2;

            // Icerik dikeyde ortada DURABILIR ama sigmadiginda yukaridan
            // baslamali; ScrollView'in kabi buyudukce ortalama kendiliginden
            // kalkiyor.
            v.contentContainer.style.flexGrow = 1;
            v.contentContainer.style.justifyContent = Justify.Center;
            return v;
        }
    }

    /// <summary>
    /// Ana menu.
    ///
    /// "Devam et" ustte ve KAYIT VARSA gorunuyor. Bir yonetim oyununa
    /// donen oyuncunun ilk istedigi sey kaldigi yer; yeni oyunu one
    /// koymak, her acilista yanlis dugmeye basma riski demek.
    /// </summary>
    public sealed class MainMenuScreen : UiScreen
    {
        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();

            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 420;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            Label title = Theme.Text(Loc.T("ui.game.title"), Theme.FontHuge, Theme.Accent);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = Theme.Pad;
            col.Add(title);

            Label sub = Theme.Text(Loc.T("ui.menu.tagline"), Theme.FontBody, Theme.InkDim);
            sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            sub.style.marginBottom = Theme.Pad * 2;
            col.Add(sub);

            if (AnySave())
                col.Add(Theme.Btn(Loc.T("ui.menu.continue"),
                    () => Ui.Push(new SlotScreen(SlotScreen.Mode.Load)), primary: true));

            col.Add(Theme.Btn(Loc.T("ui.menu.new"),
                () => Ui.Push(new CuisineScreen())));
            col.Add(Theme.Btn(Loc.T("ui.menu.settings"),
                () => Ui.Push(new SettingsScreen())));
            col.Add(Theme.Btn(Loc.T("ui.menu.credits"),
                () => Ui.Push(new CreditsScreen())));

#if UNITY_STANDALONE || UNITY_EDITOR
            col.Add(Theme.Btn(Loc.T("ui.menu.quit"), Quit));
#endif

            root.Add(col);
            return root;
        }

        private static bool AnySave()
        {
            for (int i = 0; i < SaveStore.SlotCount; i++)
                if (SaveStore.Read(i).Exists) return true;
            return false;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Ana menude geri tusu cikis DEGIL: kazayla oyundan atmak,
        // mobilde en can sikici sey.
        public override bool OnBack() { return false; }
    }

    /// <summary>
    /// Mutfak secimi. docs/07: secim kayda BAGLI ve degistirilemiyor,
    /// o yuzden secim aninda ne aldigini gormeli - imza mekanigi dahil.
    /// </summary>
    public sealed class CuisineScreen : UiScreen
    {
        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.cuisine.pick")));

            col.Add(Card("fastfood", Loc.T("cuisine.fastfood"),
                Loc.T("ui.cuisine.combo"),
                Loc.T("ui.cuisine.fastfood_desc")));
            col.Add(Card("turk", Loc.T("cuisine.turk"),
                Loc.T("ui.cuisine.credit"),
                Loc.T("ui.cuisine.turk_desc")));

            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }

        private VisualElement Card(string id, string name, string signature, string flavour)
        {
            VisualElement card = Theme.PanelBox();

            Label t = Theme.Text(name, Theme.FontTitle, Theme.Accent);
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(t);
            card.Add(Theme.Text(flavour, Theme.FontSmall, Theme.InkDim));
            card.Add(Theme.Divider());
            card.Add(Theme.Text(Loc.T("ui.cuisine.signature"), Theme.FontSmall, Theme.InkFaint));
            card.Add(Theme.Text(signature, Theme.FontBody));
            card.Add(Theme.Btn(Loc.T("ui.cuisine.start"),
                () => Ui.Push(new SlotScreen(SlotScreen.Mode.New, id)), primary: true));

            return card;
        }
    }

    /// <summary>
    /// Kayit yuvalari. docs/21 dort yuva.
    ///
    /// Dolu bir yuvanin ustune yazmak ONAY istiyor. Bir kampanya altmis
    /// gun; onaysiz silinmesi kabul edilemez.
    /// </summary>
    public sealed class SlotScreen : UiScreen
    {
        public enum Mode { New, Load }

        private readonly Mode _mode;
        private readonly string _cuisine;
        private int _confirmDelete = -1;
        private int _confirmSlot = -1;

        public SlotScreen(Mode mode, string cuisine = null)
        {
            _mode = mode;
            _cuisine = cuisine;
        }

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.slot.title")));

            for (int i = 0; i < SaveStore.SlotCount; i++) col.Add(Row(i));

            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }

        private VisualElement Row(int slot)
        {
            SlotInfo info = SaveStore.Read(slot);
            VisualElement card = Theme.PanelBox();

            VisualElement head = Theme.Row(0);
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Theme.Text((slot + 1) + ". " + Loc.T("ui.slot.title"),
                                Theme.FontBody, Theme.InkDim));

            if (info.Broken)
                head.Add(Theme.Text(Loc.T("ui.slot.broken"), Theme.FontSmall, Theme.Bad));
            else if (info.Exists)
                head.Add(Theme.Text(info.Saved.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Loc.Culture),
                                    Theme.FontSmall, Theme.InkFaint));
            card.Add(head);

            if (info.Exists && !info.Broken)
            {
                card.Add(Theme.Text(Loc.T("cuisine." + info.Cuisine), Theme.FontTitle));
                VisualElement stats = Theme.Row(Theme.Pad);
                stats.Add(Theme.Text(Loc.T("ui.slot.day", info.Day), Theme.FontSmall, Theme.InkDim));
                stats.Add(Theme.Text(Loc.Money(info.Cash), Theme.FontSmall,
                                     Theme.CashColor(info.Cash)));
                stats.Add(Theme.Text(Loc.T("ui.hud.reputation") + " "
                                     + Loc.Reputation(info.ReputationCenti),
                                     Theme.FontSmall,
                                     Theme.ReputationColor(info.ReputationCenti)));
                card.Add(stats);
            }
            else if (!info.Broken)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.empty"), Theme.FontTitle, Theme.InkFaint));
            }

            // --- eylemler -----------------------------------------------------
            if (_confirmDelete == slot)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.delete_confirm"),
                                    Theme.FontSmall, Theme.Bad));
                VisualElement ask = Theme.Row(Theme.Gap);
                ask.Add(Theme.Btn(Loc.T("ui.common.cancel"),
                    () => { _confirmDelete = -1; Ui.Refresh(); },
                    primary: true, wide: true));
                ask.Add(Theme.Btn(Loc.T("ui.slot.delete"), () =>
                {
                    SaveStore.Delete(slot);
                    Sfx.Cancel();
                    _confirmDelete = -1;
                    Ui.Refresh();
                }, wide: true, danger: true));
                card.Add(ask);
                return card;
            }

            if (_confirmSlot == slot)
            {
                card.Add(Theme.Text(Loc.T("ui.slot.overwrite"), Theme.FontSmall, Theme.Warn));
                VisualElement ask = Theme.Row(Theme.Gap);
                ask.Add(Theme.Btn(Loc.T("ui.common.cancel"),
                    () => { _confirmSlot = -1; Ui.Refresh(); },
                    primary: true, wide: true));
                ask.Add(Theme.Btn(Loc.T("ui.common.yes"), () => Begin(slot),
                                  wide: true, danger: true));
                card.Add(ask);
            }
            else
            {
                VisualElement actions = Theme.Row(Theme.Gap);
                if (_mode == Mode.Load)
                {
                    if (info.Exists && !info.Broken)
                        actions.Add(Theme.Btn(Loc.T("ui.menu.continue"),
                            () => Begin(slot), primary: true, wide: true));
                    else
                        // Bozuk yuva "Bos" DEGIL. Ustteki rozet "bozuk
                        // kayit" derken alttaki pasif dugme "Bos" diyordu;
                        // ayni kartin iki yarisi celisiyordu ve oyuncu
                        // kampanyasini bulamadigi anda okudugu ekran buydu.
                        actions.Add(Disabled(Loc.T(info.Broken
                            ? "ui.slot.unloadable" : "ui.slot.empty")));
                }
                else
                {
                    actions.Add(Theme.Btn(info.Exists ? Loc.T("ui.menu.new") : Loc.T("ui.cuisine.start"),
                        () =>
                        {
                            if (info.Exists) { _confirmSlot = slot; Ui.Refresh(); }
                            else Begin(slot);
                        }, primary: true, wide: true));
                }

                // Silme de ONAY istiyor.
                //
                // Uzerine yazma onay istiyordu, silme istemiyordu - ayni
                // yikiciliktaki iki eylemden biri korumali, digeri tek
                // dokunusla altmis gunluk bir kampanyayi siliyordu.
                if (info.Exists)
                    actions.Add(Theme.Btn(Loc.T("ui.slot.delete"), () =>
                    {
                        _confirmDelete = slot;
                        Ui.Refresh();
                    }));
                card.Add(actions);
            }

            return card;
        }

        private static VisualElement Disabled(string text)
        {
            Label l = Theme.Text(text, Theme.FontBody, Theme.InkFaint);
            l.style.minHeight = Theme.Touch;
            l.style.unityTextAlign = TextAnchor.MiddleLeft;
            l.style.flexGrow = 1;
            return l;
        }

        private void Begin(int slot)
        {
            bool ok = _mode == Mode.New
                ? App.StartNew(_cuisine, slot)
                : App.LoadSlot(slot);

            if (!ok)
            {
                Sfx.Cancel();
                Ui.Push(new ErrorScreen(App.LoadError ?? Loc.T("ui.error.save")));
                return;
            }
            if (_mode == Mode.New) App.SaveToSlot(slot);

            Sfx.Confirm();
            Ui.Replace(new GameScreen());
        }
    }

    /// <summary>Ayarlar. Ses seviyeleri kalici; oyuncu her acilista ayarlamasin.</summary>
    public sealed class SettingsScreen : UiScreen
    {
        private const string KeySfx = "lokanta.ses";
        private const string KeyMusic = "lokanta.muzik";

        public static void Apply(Music music)
        {
            Sfx.Volume = PlayerPrefs.GetFloat(KeySfx, 0.7f);
            if (music != null) music.Volume = PlayerPrefs.GetFloat(KeyMusic, 0.22f);
        }

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 480;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.settings.title")));

            col.Add(Level(Loc.T("ui.settings.sound"),
                PlayerPrefs.GetFloat(KeySfx, 0.7f), v =>
                {
                    PlayerPrefs.SetFloat(KeySfx, v);
                    Sfx.Volume = v;
                    Sfx.Click();            // yeni seviye HEMEN duyulsun
                }));

            col.Add(Level(Loc.T("ui.settings.music"),
                PlayerPrefs.GetFloat(KeyMusic, 0.22f), v =>
                {
                    PlayerPrefs.SetFloat(KeyMusic, v);
                    if (App.Music != null) App.Music.Volume = v;
                }));

            // Ipuclari bir kez gorununce bir daha gelmiyor ve bu
            // CIHAZDA saklaniyor, kayitta degil. Oyunu birine gosteren
            // ya da uzun bir aradan sonra donen oyuncu icin geri getirme
            // yolu lazim - yoksa o ilk cumleler bir daha okunamaz.
            col.Add(Theme.Btn(Loc.T("ui.settings.hints_reset"),
                              () => Hints.Reset()));

            // DIL ARTIK BIR SECIM.
            //
            // Once yalnizca "Dil: Türkçe" yazan bir SATIRDI - oyun tek
            // dilliydi ve satir bir bilgi bile degil, bir sozdu.
            //
            // Her dil KENDI adiyla yaziyor: "Turkish" yazan bir satiri
            // arayan kisi zaten Ingilizce biliyor demektir.
            col.Add(Theme.Head(Loc.T("ui.settings.language")));
            VisualElement diller = Theme.Row(Theme.Gap);
            // SERIT SARIYOR: bes dil tek satira sigmiyor.
            //
            // Iki dilken sigiyordu ve satir sabitti. Bes dilde telefon
            // eninde (~400 dp) tasardi - ve tasan ogeyi turdaki "ekran
            // disina tasan oge" kontrolu yakalardi.
            diller.style.flexWrap = Wrap.Wrap;

            for (int i = 0; i < Loc.Languages.Length; i++)
            {
                int idx = i;
                bool secili = Loc.Language == i;
                Button dugme = Theme.Btn(Loc.LanguageNames[i], () =>
                {
                    Loc.SetLanguage(idx);
                    // Yazi tipi de dile bagli: Cince'ye gecerken kok
                    // ogenin fontu degismezse metin bos kutu cikar.
                    Ui.ApplyLanguage();
                    // Butun ekranlar metni kurulusta okuyor: yigini
                    // tazelemek yeni dili her yere tasiyor.
                    Ui.Refresh();
                }, primary: secili, wide: true);

                // HER DIL KENDI ADINI OKUNABILDIGI YAZI TIPIYLE YAZIYOR.
                //
                // "中文" Rubik'te YOK. Oyun Turkce'yken o dugme bos kutu
                // gosterirdi - yani Cince'yi secmek isteyen kisi hangi
                // dugmeye basacagini goremezdi. Yazi tipi denetimi tam
                // bunu yakaladi (U+4E2D, U+6587 - Loc.cs).
                //
                // Cozum YEREL: yalnizca o dugme. Butun ekrani CJK
                // fontuna cevirmek, Turkce ekranin yazi tipini bir dil
                // dugmesi yuzunden degistirmek olurdu.
                if (Loc.Languages[idx] == "zh" && Ui.FontCJK != null)
                    dugme.style.unityFontDefinition =
                        FontDefinition.FromFont(Ui.FontCJK);

                // AYNI GEREKCE, BASKA ENGEL: Arapca dil adinin harfleri
                // Rubik'te VAR ama olcunlu uretici onlari birlestirmiyor ve ters
                // diziyor. Arapca okuyan biri, dilini aradigi dugmede
                // dagilmis harfler gorurdu. Yon ve uretici yalnizca o
                // dugmede ceviriliyor - ekranin kalani oldugu gibi.
                if (Loc.Languages[idx] == "ar")
                {
                    dugme.style.unityTextGenerator = TextGeneratorType.Advanced;
                    dugme.languageDirection = LanguageDirection.RTL;
                }

                diller.Add(dugme);
            }
            col.Add(diller);

            col.Add(Theme.Btn(Loc.T("ui.settings.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }

        /// <summary>
        /// Sifir-bir arasi bir seviyeyi on kademeli denetime baglar.
        /// Kademe sayisi ON: daha ince ayar mobilde ne okunuyor ne de
        /// parmakla tutturuluyor.
        /// </summary>
        /// <summary>
        /// Ses kademesi sayisi.
        ///
        /// 10 -> 5: OLCULDU. On kutucuk x 52 dp taban genislik, 480
        /// dp'lik panele SIGMIYOR ve satir sariyordu - sekiz kutucuk
        /// ustte, iki kutucuk altta ve alttakiler flexGrow yuzunden
        /// yarim ekran genisliginde iki bos kutuya donusuyordu. Ekran
        /// goruntusunde bu bir HATA gibi okunuyor, ses ayari gibi degil.
        ///
        /// Bes kademe hem sigiyor hem de yeterli: ses seviyesi %10
        /// hassasiyetle ayarlanan bir sey degil.
        /// </summary>
        private const int Steps = 5;

        private VisualElement Level(string label, float value, Action<float> onChange)
        {
            VisualElement box = Theme.PanelBox();
            int start = Mathf.Clamp(Mathf.RoundToInt(value * Steps), 0, Steps);

            box.Add(Theme.Level(label, Steps, start, step =>
            {
                onChange(step / (float)Steps);
                PlayerPrefs.Save();
                Ui.Refresh();
            }));
            return box;
        }
    }

    /// <summary>
    /// Acik kaynak lisanslari.
    ///
    /// Ayri bir ekran, cunku metinlerin KENDISI gerekiyor: OFL telif
    /// bildirimi istiyor, MIT lisans metninin kopyalanmasini istiyor,
    /// Apache-2.0 bildirimi istiyor. "Rubik - SIL OFL 1.1" yazmak lisans
    /// ADINI veriyor, sartini degil.
    ///
    /// Metinler Resources altinda, yani YAPIYA GIRIYOR. Denetimde
    /// bulundu: yazi tipinin ikili verisi APK'nin icindeydi ama tek bir
    /// lisans metni yoktu.
    /// </summary>
    public sealed class LicenseScreen : UiScreen
    {
        private static readonly string[] Files =
        {
            "lisans/rubik-ofl",
            // IKINCI YAZI TIPI = IKINCI LISANS METNI.
            //
            // Noto Sans SC ayri bir telif sahibinin eseri ve OFL,
            // metnin URUNLE BIRLIKTE dagitilmasini istiyor. "Rubik OFL
            // var, o da OFL" demek lisansi karsilamiyor - iki ayri
            // bildirimdir.
            "lisans/noto-sans-sc-ofl",
            "lisans/kenney-cc0",
            "lisans/motor-bilesenleri",
        };

        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = Theme.ReadWidth;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.credits.licenses")));

            foreach (string f in Files)
            {
                TextAsset t = Resources.Load<TextAsset>(f);
                if (t == null) continue;

                VisualElement box = Theme.PanelBox();
                Label body = Theme.Text(t.text, Theme.FontSmall, Theme.InkDim);
                body.style.whiteSpace = WhiteSpace.Normal;
                box.Add(body);
                col.Add(box);
                Resources.UnloadAsset(t);
            }

            col.Add(Theme.Btn(Loc.T("ui.settings.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }
    }

    /// <summary>
    /// Yapimci. Atif ZORUNLU olmasa da yaziliyor - CC0 varliklar icin
    /// tesekkur etmemek ucuzluk olur (vendor/ATIF.md).
    /// </summary>
    public sealed class CreditsScreen : UiScreen
    {
        public override VisualElement Build()
        {
            VisualElement root = ErrorScreen.Backdrop();
            VisualElement col = Theme.Column(Theme.Gap);
            col.style.maxWidth = 560;
            col.style.alignSelf = Align.Center;
            col.style.width = Length.Percent(100);
            col.style.flexShrink = 0;

            col.Add(Theme.Title(Loc.T("ui.menu.credits")));

            VisualElement box = Theme.PanelBox();
            box.Add(Theme.Text(Loc.T("ui.credits.design"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.author"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.models"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.models_by"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.audio"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.audio_by"), Theme.FontBody));
            box.Add(Theme.Divider());
            box.Add(Theme.Text(Loc.T("ui.credits.font"), Theme.FontSmall, Theme.InkFaint));
            box.Add(Theme.Text(Loc.T("ui.credits.font_by"), Theme.FontBody));
            box.Add(Theme.Text(Loc.T("ui.credits.font_copyright"),
                               Theme.FontSmall, Theme.InkFaint));
            col.Add(box);

            col.Add(Theme.Btn(Loc.T("ui.credits.licenses"),
                              () => Ui.Push(new LicenseScreen())));
            col.Add(Theme.Btn(Loc.T("ui.hud.back"), () => Ui.Pop()));
            root.Add(col);
            return root;
        }
    }
}
