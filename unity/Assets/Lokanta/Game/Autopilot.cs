// DIKKAT: BU DOSYANIN YORUMLARI KAYIP.
//
// 13 Eylul 2026: dosyaya yazan bir betik kesildi ve dosya sistemi
// 130.716 baytin tamamini NUL ile doldurdu - kaynak tamamen yok oldu.
// Projede surum denetimi yok, golge kopya ve dosya gecmisi de yoktu.
//
// Mantik, bozulmadan yedi dakika once derlenmis
// Library/ScriptAssemblies/Lokanta.Game.dll geri cevrilerek kurtarildi
// (ilspycmd). Kurtarilan sey DAVRANIS; yorumlarin tamami gitti.
//
// Kaybolan gerekcelerin buyuk kismi BASKA yerde duruyor ve oradan
// okunabilir: docs/43-inceleme-ve-olcum.md (turun butun olcum
// hikayesi) ve docs/45-tasarim-incelemesi.md. Yeniden yazilan
// yorumlar asagida, kaynagi belli olanlardan baslayarak ekleniyor.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Lokanta.Game
{
    public sealed class Autopilot : MonoBehaviour
    {
        public const string Flag = "-lokanta-tur";

        private const string OutFlag = "-lokanta-cikti";

        private GameApp _app;

        private UIDocument _doc;

        private string _dir;

        private int _shot;

        private readonly Dictionary<string, string> _shotHash = new Dictionary<string, string>();

        private string _shotDup;

        private int _stories;

        private readonly List<string> _log = new List<string>();

        private int _krizGorulen;

        private int _secilenMasa = -1;

        private bool _cikiyor;

        private int _gecti;

        private int _kaldi;

        private int _olculemedi;

        private int _doluEnCok;

        public static bool Requested
        {
            get
            {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                for (int i = 0; i < commandLineArgs.Length; i++)
                {
                    if (commandLineArgs[i] == "-lokanta-tur")
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Begin(GameApp app, UIDocument doc)
        {
            _app = app;
            _doc = doc;
            _dir = OutDir();
            Directory.CreateDirectory(_dir);
            MatchPhoneDp();
            if (OlcekBayragi() > 1.01f)
            {
                Hints.MarkAllSeen();
            }
            else
            {
                Hints.Reset();
            }
            Application.runInBackground = true;
            GameApp.SmokeTour = true;
            ((MonoBehaviour)this).StartCoroutine(Tour());
        }

        private void MatchPhoneDp()
        {
            if (!(_doc == null) && !(_doc.panelSettings == null))
            {
                float num = OlcekBayragi();
                float num2 = ((Screen.dpi > 1f) ? Screen.dpi : 96f);
                _doc.panelSettings.referenceDpi = num2 / num;
                _doc.panelSettings.fallbackDpi = num2 / num;
                Debug.Log((object)("  tur olcegi : " + num.ToString("0.##") + " piksel = 1 dp (ekran " + Screen.width + "x" + Screen.height + ", yogunluk " + num2.ToString("0") + ", yani " + ((float)Screen.width / num).ToString("0") + "x" + ((float)Screen.height / num).ToString("0") + " dp)"));
            }
        }

        /// <summary>
        /// HANGI MUTFAK OYNANACAK. 0 hizli yemek, 1 Turk lokantasi.
        ///
        /// Tur mutfagi SABIT olarak 1 seciyordu (Turk). Yani hizli
        /// yemek mutfaginin gorunusu - kendi duvari, paleti, zemin
        /// deseni, disarisinin rengi - turun HIC gormedigi bir sey oldu.
        /// Bu projede ayni aileden kac hata ciktigini docs/48-49 sayiyor:
        /// kosmayan bir kontrol, gecen bir kontrolle ayni gorunuyor.
        ///
        /// Varsayilan 1 birakildi: bayrak verilmezse davranis eskisiyle
        /// birebir ayni, yani mevcut kosular ve goruntuler kaymiyor.
        /// </summary>
        private static int MutfakBayragi()
        {
            string[] a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] != "-lokanta-mutfak") continue;
                string v = a[i + 1];
                if (v == "fastfood") return 0;
                if (v == "turk") return 1;
                Debug.LogWarning("Tur: -lokanta-mutfak degeri okunamadi ("
                                 + v + "), turk kullaniliyor");
                return 1;
            }
            return 1;
        }

        private static float OlcekBayragi()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (!(commandLineArgs[i] != "-lokanta-olcek"))
                {
                    if (float.TryParse(commandLineArgs[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result >= 0.5f && result <= 6f)
                    {
                        return result;
                    }
                    Debug.LogWarning((object)("Tur: -lokanta-olcek degeri okunamadi (" + commandLineArgs[i + 1] + "), 1 kullaniliyor"));
                    return 1f;
                }
            }
            return 1f;
        }

        private static float Isik(Color c)
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private static float CurrentRenderScale()
        {
            UniversalRenderPipelineAsset universalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!(universalRenderPipelineAsset != null))
            {
                return -1f;
            }
            return universalRenderPipelineAsset.renderScale;
        }

        private bool HalaDolu(int t)
        {
            if (t < 0 || t >= _app.Sim.TableCount)
            {
                return false;
            }
            CustomerStage customerStage = _app.Sim.TableStage(t);
            if (customerStage != CustomerStage.None && customerStage != CustomerStage.Done)
            {
                return customerStage != CustomerStage.LeftAngry;
            }
            return false;
        }

        private int DoluMasa()
        {
            for (int i = 0; i < _app.Sim.TableCount; i++)
            {
                CustomerStage customerStage = _app.Sim.TableStage(i);
                if (customerStage != CustomerStage.None && customerStage != CustomerStage.Done && customerStage != CustomerStage.LeftAngry)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string OutDir()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (commandLineArgs[i] == "-lokanta-cikti")
                {
                    return commandLineArgs[i + 1];
                }
            }
            return Path.Combine(Application.dataPath, "..", "render");
        }

        private IEnumerator Tour()
        {
            Loc.SetLanguage(0);
            string text = Loc.T("ui.menu.new");
            int count = Loc.Count;
            Loc.SetLanguage(1);
            string text2 = Loc.T("ui.menu.new");
            int count2 = Loc.Count;
            Note(!text2.StartsWith("[") && text2 != text && count2 == count, "Ingilizce metin yuklendi (" + text + " / " + text2 + ", " + count2 + " anahtar)");
            string text3 = Loc.Money(800000L);
            Loc.SetLanguage(0);
            string text4 = Loc.Money(800000L);
            Note(text3 != text4, "Sayi bicimi dile bagli (" + text4 + " / " + text3 + ")");
            Loc.SetLanguage(0);
            for (int i = 0; i < 4; i++)
            {
                SaveStore.Delete(i);
            }
            yield return BozukYuva();
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            yield return Shot("01-ana-menu");
            Note(Click(Loc.T("ui.menu.new")), "Yeni oyun dugmesi");
            yield return Settle();
            yield return Shot("02-mutfak-secimi");
            int mutfak = MutfakBayragi();
            Note(Click(Loc.T("ui.cuisine.start"), mutfak),
                 mutfak == 0 ? "Hizli yemek dugmesi" : "Turk lokantasi dugmesi");
            yield return Settle();
            yield return Shot("03-yuva-secimi");
            Note(Click(Loc.T("ui.cuisine.start")), "Birinci yuva");
            yield return Settle();
            Note(_app.InGame, "Oyun basladi");
            yield return Shot("04-oyun-sabah");
            yield return CheckStripsBothLanguages("sabah");
            string[] array = new string[4]
            {
                Loc.T("ui.morning.market"),
                Loc.T("ui.morning.menu"),
                Loc.T("ui.morning.staff"),
                Loc.T("ui.morning.equipment")
            };
            int once;
            bool basildi;
            foreach (string s in array)
            {
                if (!Click(s))
                {
                    Note(ok: false, s + " dugmesi");
                    continue;
                }
                yield return Settle();
                if (s == Loc.T("ui.morning.staff"))
                {
                    once = _app.Sim.Dishwashers;
                    basildi = Click(Loc.T("ui.staff.sink_add"));
                    yield return Settle();
                    Note(basildi && _app.Sim.Dishwashers == once + 1, "Bulasik nobetine atama (" + once + " -> " + _app.Sim.Dishwashers + ")");
                    Click(Loc.T("ui.staff.sink_remove"));
                    yield return Settle();
                    Note(_app.Sim.Dishwashers == once, "Bulasik nobeti geri alindi (" + _app.Sim.Dishwashers + ")");

                    // SALON ROLUNUN ADI MUTFAGA GORE.
                    //
                    // Hizli yemek self servis: masaya garson gelmiyor,
                    // salondaki kisi tepsileri topluyor - yani
                    // TEMIZLIKCI (docs/51). Ad ekranda degismezse
                    // oyuncu ise aldigi kisinin ne yaptigini bilemez.
                    //
                    // Kontrol MUTFAGA GORE ters yonu de ariyor: yanlis
                    // etiketin GORUNMEDIGINI de dogruluyor, yoksa iki
                    // adi birden basan bir ekran yesil gecerdi.
                    bool self = _app.Content != null && _app.Content.SelfService;
                    string dogru = self ? Loc.T("ui.staff.busser") : Loc.T("role.garson");
                    string yanlis = self ? Loc.T("role.garson") : Loc.T("ui.staff.busser");
                    Note(HasText(dogru) && !HasText(yanlis),
                         "Salon rolunun adi mutfaga uygun (" + dogru + ")");
                }
                yield return Shot("05-" + Slug(s));
                Back();
                yield return Settle();
            }
            Hints.Hint hint = Hints.Current(_app.Sim);
            NoteIf(OlcekBayragi() <= 1.01f, hint != null, "1. gun sabahinda ipucu var (" + ((hint != null) ? hint.Id : "yok") + ")");
            bool acKomutu = Click(Loc.T("ui.morning.open"));
            yield return Settle();
            bool sordu = _app.Sim.Phase == DayPhase.Morning;
            if (sordu)
            {
                Click(Loc.T("ui.morning.open"));
                yield return Settle();
            }
            Note(acKomutu && _app.Sim.Phase == DayPhase.Service, "Servisi ac" + (sordu ? " (hazirlik uyarisi sordu, ikinci dokunus acti)" : ""));
            yield return Settle();
            yield return Shot("06-servis-basi");
            float was = _app.TimeScale;
            _app.TimeScale = 240f;
            float guard = 0f;
            while (_app.Sim.OccupiedTables < 2 && guard < 12f && _app.Sim.ServiceProgressBp < 2500)
            {
                guard += Time.deltaTime;
                yield return null;
            }
            Debug.Log((object)("  TANI salon dolma beklemesi: " + guard.ToString("0.0") + " sn, servis " + _app.Sim.ServiceProgressBp / 100 + "%, dolu masa " + _app.Sim.OccupiedTables));
            bool oncekiDurum = _app.Paused;
            _app.Paused = true;
            yield return Shot("07-servis-yogun");
            _app.TimeScale = 16f;
            yield return CheckStripsBothLanguages("servis");
            if (_app.Rig != null)
            {
                Vector3 once2 = ((Component)_app.Rig).transform.position;
                for (int k = 0; k < 40; k++)
                {
                    _app.Rig.ApplyGesture(0.2f, 0f);
                }
                yield return null;
                Note(_app.Rig.Zoom >= CameraRig.MinZoomLimit - 0.001f, "Yakinlastirma tavani tutuyor (" + _app.Rig.Zoom.ToString("0.00") + ")");
                Note(Vector3.Distance(((Component)_app.Rig).transform.position, once2) > 0.5f, "Yakinlastirma kamerayi gercekten yaklastirdi");
                Note(CurrentRenderScale() > 0.95f, "Yaklasinca render olcegi yukseldi (" + CurrentRenderScale().ToString("0.00") + ")");
                _app.Rig.ApplyGesture(0f, 5f);
                yield return null;
                Note(_app.Rig.YawOffset > 0.1f, "Iki parmak donduruyor (" + _app.Rig.YawOffset.ToString("0.0") + " derece)");
                for (int l = 0; l < 40; l++)
                {
                    _app.Rig.ApplyGesture(0f, 5f);
                }
                yield return null;
                Note(Mathf.Approximately(_app.Rig.YawOffset, CameraRig.MaxYawLimit), "Donus siniri tutuyor (" + _app.Rig.YawOffset.ToString("0") + " / " + CameraRig.MaxYawLimit.ToString("0") + " derece)");
                _app.Rig.Overview();
                yield return Settled();
                Note(Mathf.Approximately(_app.Rig.Zoom, 1f) && Mathf.Approximately(_app.Rig.YawOffset, 0f), "Genel gorunum yakinlastirmayi ve donusu sifirladi");
                float num = Quaternion.Angle(((Component)_app.Rig).transform.rotation, CameraFit.Rotation);
                Note(num < 1f, "Kamera gercekten temel aciya dondu (" + num.ToString("0.0") + " derece sapma)");
                float num2 = Vector3.Distance(((Component)_app.Rig).transform.position, once2);
                Note(num2 < 0.5f, "Kamera genel cerceveye geri geldi (" + num2.ToString("0.00") + " m)");
                Note(CurrentRenderScale() < 0.95f, "Uzaklasinca render olcegi geri dustu (" + CurrentRenderScale().ToString("0.00") + ")");
            }
            _app.Paused = oncekiDurum;
            float turHiz = _app.TimeScale;
            _app.TimeScale = GameApp.BaseTimeScale;
            float bekle = 0f;
            int j = 0;
            while (_app.Sim.OccupiedTables < 2 && bekle < 20f && _app.Sim.ServiceProgressBp < 3000)
            {
                if (_app.Sim.OccupiedTables > j)
                {
                    j = _app.Sim.OccupiedTables;
                }
                bekle += Time.deltaTime;
                yield return null;
            }
            if (_app.Sim.OccupiedTables > j)
            {
                j = _app.Sim.OccupiedTables;
            }
            Debug.Log((object)("  TANI ikinci masa beklemesi: " + bekle.ToString("0.0") + " sn, servis " + _app.Sim.ServiceProgressBp / 100 + "%, dolu masa " + _app.Sim.OccupiedTables + " (en cok " + j + ")"));
            _doluEnCok = j;
            RestaurantView cv = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            j = 0;
            once = 0;
            int disarida = 0;
            int cakisanYaya = 0;
            int direkteYaya = 0;
            int kirliEnCok = 0;
            int yikayan = 0;
            int yiyenMasa = 0;
            int masaTabagi = 0;
            bekle = 0f;
            int temizEnAz = int.MaxValue;
            int calisan = 0;
            int islenen = 0;
            int gorevliAsci = 0;
            int simGorev = 0;
            float aciEnKotu = -1f;
            int tabaksizKare = 0;
            int yiyenKare = 0;
            int isyapmayanKare = 0;
            int calisanKare = 0;
            Walker.AnimAdvanced = 0;
            Walker.AnimStalled = 0;
            RestaurantView.WorkAnimAdvanced = 0;
            RestaurantView.WorkAnimStalled = 0;
            int gunBasi = _app.Sim.ServiceProgressBp;
            float sure = 0f;
            basildi = false;
            // PENCERE GUNUN YARISINDA BIRAKIYOR.
            //
            // Tavan once %80 idi ve gun DUZ oldugu surece sorun yoktu:
            // masalar gun boyu dolu kaldigi icin sonraki masa kontrolleri
            // her an calisabiliyordu. Gun sivriltilince (docs/48) bu
            // varsayim coktu - zirve %12-40 arasinda yasaniyor ve pencere
            // %80'e kadar kosunca masa avi bos bir salona yetisiyordu:
            // "servis %70, bugun 12 kisi bekleniyordu".
            //
            // Tur artik zirveyi SONRAKI kontrollere birakiyor. Gercek
            // zaman butcesi (40 sn) degismedi; yalnizca gunun neresinde
            // durdugu degisti.
            //
            // UZATMA: OLCUM HENUZ YAPILAMADIYSA.
            //
            // 40 sn tek basina sessizce olcumun TANIMI olmustu. Turk
            // mutfaginin zirvesi bir onceki islemede 1. dilimden 2.
            // dilime tasindi (export.py: [1200,4800,2500,1500] ->
            // [1200,2800,4500,1500]) ve Turkce tur o islemeden sonra hic
            // kosturulmadi. Kosunca:
            //
            //   TANI canlilik penceresi: 37,7 sn, servis %30 -> %61
            //   HATA: Mutfakta is yapiliyor (0 kisi; simulasyon is verdi
            //         156 kez)
            //
            // Ayni yapi ikinci kosuda GECTI. Yani kirmizi da yesil de
            // olcumun degil ORNEKLEME SANSININ sonucuydu: tek asci
            // zamanin cogunu istasyona YURUYEREK geciriyor (simulasyon
            // gorev veriyor, durus Walk), pencere de kapanmadan once o
            // dar araliga denk gelebiliyor ya da gelmiyor.
            //
            // Cozum pencereyi herkes icin uzatmak DEGIL - o, %80'e kadar
            // kosup zirveyi yiyen eski davranisi geri getirirdi. Uzatma
            // yalnizca olculecek sey HENUZ GORULMEDIYSE ve simulasyon
            // gercekten is veriyorsa devreye giriyor. Kontrol zaten
            // saglandiysa pencere eskisi gibi 40 sn'de kapaniyor, yani
            // sonraki kontroller zirveyi aynen buluyor.
            while ((sure < 40f || (calisan == 0 && simGorev > 0 && sure < 75f))
                   && cv != null)
            {
                // PENCERE KOSULA BAKIYOR, SABIT BIR DILIME DEGIL.
                //
                // Once tavan %80 idi ve zirveyi yiyip sonraki masa
                // kontrollerini bos salona birakiyordu; %50'ye cektim ve
                // bu kez TERSI oldu - gun sivrildikten sonra birinci gun
                // dort masa ve on iki kisi demek, yani mutfagin calistigi
                // an dar bir pencereye denk gelmeyebiliyor. Bir kosuda
                // "Mutfakta is yapiliyor (0 kisi; simulasyon is verdi 536
                // kez)" cikti: is vardi, pencere baska yere bakiyordu.
                //
                // Dogrusu sabit dilim degil KOSUL: pencere, gormesi
                // gereken seyi gorunce birakiyor. Gormediyse gunun
                // %80'ine kadar aramaya devam ediyor - yani eski tavan
                // yalnizca EN KOTU durumda devreye giriyor ve masa
                // kontrolleri normalde erken sirasini aliyor.
                bool gorduk = gorevliAsci > 0 && j > 0 && yiyenMasa > 0;
                if (gorduk && _app.Sim.ServiceProgressBp > 5000)
                {
                    basildi = true;
                    break;
                }
                if (_app.Sim.ServiceProgressBp > 8000)
                {
                    basildi = true;
                    break;
                }
                j = Mathf.Max(j, cv.MovingCount);
                once = Mathf.Max(once, cv.OpenDoorCount);
                disarida = Mathf.Max(disarida, cv.OutsideCount);
                gorevliAsci = Mathf.Max(gorevliAsci, cv.BusyCooks);
                cakisanYaya = Mathf.Max(cakisanYaya, cv.StreetOverlaps);
                direkteYaya = Mathf.Max(direkteYaya, cv.PostOverlaps);
                yiyenMasa = Mathf.Max(yiyenMasa, cv.EatingTables);
                masaTabagi = Mathf.Max(masaTabagi, cv.TablePlatesVisible);
                if (cv.EatingTables > 0)
                {
                    yiyenKare++;
                    if (cv.TablePlatesVisible == 0)
                    {
                        tabaksizKare++;
                    }
                }
                if (_app.Sim.OccupiedTables > _doluEnCok)
                {
                    _doluEnCok = _app.Sim.OccupiedTables;
                }
                kirliEnCok = Mathf.Max(kirliEnCok, _app.Sim.PlatesDirty);
                temizEnAz = Mathf.Min(temizEnAz, _app.Sim.PlatesClean);
                yikayan = Mathf.Max(yikayan, cv.WashingCount);
                bekle = Mathf.Max(bekle, cv.WalkSlipWorst);
                GameScreen gameScreen = ((_app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
                if (gameScreen != null)
                {
                    _krizGorulen = Mathf.Max(_krizGorulen, gameScreen.CrisisTables);
                }
                cv.KitchenWork(out var calisan2, out var islenen2);
                calisan = Mathf.Max(calisan, calisan2);
                islenen = Mathf.Max(islenen, islenen2);
                if (calisan2 > 0)
                {
                    calisanKare++;
                    if (islenen2 == 0)
                    {
                        isyapmayanKare++;
                    }
                }
                float cookFacingErrorDeg = cv.CookFacingErrorDeg;
                if (cookFacingErrorDeg >= 0f && cookFacingErrorDeg > aciEnKotu)
                {
                    aciEnKotu = cookFacingErrorDeg;
                }
                for (int m = 0; m < _app.Sim.Cooks; m++)
                {
                    if (_app.Sim.CookTaskStation(m) >= 0)
                    {
                        simGorev++;
                    }
                }
                _secilenMasa = DoluMasa();
                if (j > 0 && once > 0 && disarida > 0 && calisan > 0 && islenen > 0 && _secilenMasa >= 0 && RestaurantView.WorkAnimAdvanced + RestaurantView.WorkAnimStalled >= 30)
                {
                    break;
                }
                sure += Time.deltaTime;
                yield return null;
            }
            Debug.Log((object)("  TANI canlilik penceresi: " + sure.ToString("0.0") + " sn, servis " + gunBasi / 100 + "% -> " + _app.Sim.ServiceProgressBp / 100 + "%" + (basildi ? " (GUN TAVANI)" : "")));
            Note(gunBasi < 3500, "Canlilik penceresi gunun basinda basladi (" + gunBasi / 100 + "%)");
            Note(_doluEnCok > 0, "Servis dolu masa uretiyor (en cok " + _doluEnCok + ")");
            Note(j > 0, "Salonda hareket var (" + j + " figur yolda)");
            Note(cv != null && cv.WallCount >= 8, "Oda duvarlari kuruldu (" + ((cv != null) ? cv.WallCount : 0) + " levha)");
            Note(cv != null && cv.WallsClear, "Duvarlar saydam ve carpisansiz");
            Note(cv != null && cv.DoorCount == 2, "Kanatli kapi yalnizca giris ve mutfak (" + ((cv != null) ? cv.DoorCount : 0) + ")");
            Note(cv != null && cv.GapCount >= cv.LinkCount + 1, "Her komsuluk icin gecis bosugu var (" + ((cv != null) ? cv.GapCount : 0) + " bosluk / " + ((cv != null) ? cv.LinkCount : 0) + " komsuluk)");
            Note(once > 0, "Yaklasan figur kapiyi acti (" + once + " kapi)");
            Note(disarida > 0, "Musteri sokaktan geliyor (" + disarida + " figur disarida)");
            Note(calisan > 0, "Mutfakta is yapiliyor (" + calisan + " kisi; simulasyon is verdi " + simGorev + " kez)");
            Debug.Log((object)("  TANI asci duruslari: " + ((cv != null) ? cv.CookPoses : "yok")));
            NoteIf(calisanKare > 0, isyapmayanKare == 0, "Calisan figurun animasyonu AYNI KAREDE isliyor (" + isyapmayanKare + " issiz kare / " + calisanKare + ")");
            string report = "gorunum yok";
            bool ok = cv != null && cv.AccessOk(out report);
            Note(ok, "Oda erisim kurallari (" + report + ")");
            Note(cv != null && cv.TrayCount <= cv.StaffCount, "Tepsi sayisi kadroyu asmiyor (" + ((cv != null) ? cv.TrayCount : (-1)) + " tepsi / " + ((cv != null) ? cv.StaffCount : 0) + " personel)");
            Note(cv != null && cv.PotCount > 0, "Ocaklarin ustunde kap var (" + ((cv != null) ? cv.PotCount : 0) + " ocak)");
            NoteIf(Wardrobe.Attempted > 0, Wardrobe.Dressed == Wardrobe.Attempted, "Butun personel giydirildi (" + Wardrobe.Dressed + "/" + Wardrobe.Attempted + ")");
            Note(cv != null && cv.StreetWalkers >= 3, "Sokakta yoldan gecenler var (" + ((cv != null) ? cv.StreetWalkers : 0) + " kisi)");
            Note(cv != null && bekle < 0.15f, "Yuruyuste ayak kaymiyor (en kotu %" + (bekle * 100f).ToString("0") + " sapma)");
            Note(cv != null && cakisanYaya == 0, "Yayalar birbirinin icinden gecmiyor (en kotu " + cakisanYaya + " cift)");
            Debug.Log((object)("  TANI tabak (pencere): en az temiz " + ((temizEnAz != int.MaxValue) ? temizEnAz : 0) + "/" + _app.Sim.PlatesTotal + ", en cok kirli " + kirliEnCok + ", lavaboda gorulen " + yikayan));
            Note(cv != null && direkteYaya == 0 && cv.StreetPostsKnown >= 3, "Yayalar lamba diregine girmiyor (en kotu " + direkteYaya + " kisi, " + ((cv != null) ? cv.StreetPostsKnown : 0) + " direk biliniyor)");
            Note(cv != null && cv.LampPostCount >= 3 && cv.LampSpacingError < 0.05f, "Sokak lambalari esit aralikli (" + ((cv != null) ? cv.LampPostCount : 0) + " direk, sapma " + ((cv != null) ? cv.LampSpacingError.ToString("0.00") : "?") + " m)");
            int animAdvanced = Walker.AnimAdvanced;
            int animStalled = Walker.AnimStalled;
            int num3 = animAdvanced + animStalled;
            Debug.Log((object)("  TANI yuruyus klibi: " + animAdvanced + " ilerledi, " + animStalled + " dondu"));
            NoteIf(num3 > 0, animAdvanced * 100 / Mathf.Max(1, num3) > 60, "Yuruyen figurun klibi ilerliyor (" + ((num3 != 0) ? (animAdvanced * 100 / num3) : 0) + "%, " + num3 + " kare)");
            int workAnimAdvanced = RestaurantView.WorkAnimAdvanced;
            int workAnimStalled = RestaurantView.WorkAnimStalled;
            int num4 = workAnimAdvanced + workAnimStalled;
            Debug.Log((object)("  TANI is klibi: " + workAnimAdvanced + " ilerledi, " + workAnimStalled + " dondu"));
            NoteIf(num4 > 0, workAnimAdvanced * 100 / Mathf.Max(1, num4) > 60, "Calisan personelin klibi ilerliyor (" + ((num4 != 0) ? (workAnimAdvanced * 100 / num4) : 0) + "%, " + num4 + " kare)");
            NoteIf(yiyenKare > 0, tabaksizKare == 0, "Yiyen masada AYNI KAREDE tabak var (" + tabaksizKare + " tabaksiz kare / " + yiyenKare + ")");
            Note(cv != null && cv.LampLanternBottom > 1.3f, "Fener bas hizasinin ustunde (" + ((cv != null) ? cv.LampLanternBottom.ToString("0.00") : "?") + " m)");
            Note(cv != null && cv.LampGlowCount == cv.LampPostCount * 3, "Her lambanin huzmesi, halesi ve havuzu var (" + ((cv != null) ? cv.LampGlowCount : 0) + " parca, " + ((cv != null) ? cv.LampPostCount : 0) + " lamba)");
            NoteIf(aciEnKotu >= 0f, aciEnKotu < 45f, "Asci ocaga donuk (" + ((aciEnKotu < 0f) ? "calisan yok" : (aciEnKotu.ToString("0") + " derece")) + ")");
            if (_app.Light != null)
            {
                DayLight light = _app.Light;
                light.Apply(DayPhase.Morning, 0f);
                Color val = ((Camera.main != null) ? Camera.main.backgroundColor : Color.black);
                Quaternion val2 = ((light.Sun != null) ? ((Component)light.Sun).transform.rotation : Quaternion.identity);
                float num5 = ((light.Sun != null) ? light.Sun.intensity : 0f);
                bool lampsOn = light.LampsOn;
                bool roomLightsOn = light.RoomLightsOn;
                RestaurantView restaurantView = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
                float num6 = ((restaurantView != null) ? restaurantView.StreetTint : (-1f));
                float num7 = Isik(RenderSettings.ambientLight);
                light.Apply(DayPhase.Evening, 1f);
                Color val3 = ((Camera.main != null) ? Camera.main.backgroundColor : Color.black);
                float num8 = ((light.Sun != null) ? light.Sun.intensity : 0f);
                float num9 = ((light.Sun != null) ? Quaternion.Angle(val2, ((Component)light.Sun).transform.rotation) : 0f);
                float num10 = Mathf.Abs(val.r - val3.r) + Mathf.Abs(val.g - val3.g) + Mathf.Abs(val.b - val3.b);
                Note(num10 > 0.25f, "Sabah ile aksam arka plani farkli (" + num10.ToString("0.00") + ")");
                Note(num9 > 20f, "Gunesin acisi gun icinde degisiyor (" + num9.ToString("0") + " derece)");
                Note(num5 > num8 * 1.8f, "Aksam gunes zayifliyor (" + num5.ToString("0.00") + " -> " + num8.ToString("0.00") + ")");
                Note(!lampsOn && light.LampsOn, "Sokak lambalari yalnizca aksam yaniyor");
                Note(light.LampCount >= 3, "Sokak lambasi kuruldu (" + light.LampCount + " adet)");
                Note(light.RoomLightCount >= 8, "Ic tavan isigi kuruldu (" + light.RoomLightCount + " adet)");
                Note(!roomLightsOn && light.RoomLightsOn, "Ic isiklar yalnizca gun ilerledikce yaniyor");
                Note(light.Warm != null && ((Behaviour)light.Warm).enabled && light.Warm.intensity > 1.5f, "Aksam ic dolgusu yaniyor (" + ((light.Warm != null) ? light.Warm.intensity.ToString("0.00") : "yok") + ")");
                // SABITLER GERI GELDI: geri cevirme bu kontrolu OLDURMUSTU.
                //
                // Orijinali DayLight.RoomThreshold < DayLight.LampThreshold
                // idi; ikisi de const oldugu icin derleyici karsilastirmayi
                // DERLEME ANINDA yapip sonucu "true" olarak gomdu. Kontrol
                // kalici yesil kaldi - hicbir sey olcmuyordu ve bunu hicbir
                // sey soylemezdi. Bu projenin en sik hata sinifi, bu kez
                // derleyici eliyle.
                Note(DayLight.RoomThreshold < DayLight.LampThreshold,
                     "Ic isiklar sokak lambalarindan once yaniyor ("
                     + DayLight.RoomThreshold.ToString("0.00") + " < "
                     + DayLight.LampThreshold.ToString("0.00") + ")");
                float num11 = ((restaurantView != null) ? restaurantView.StreetTint : (-1f));
                float num12 = Isik(RenderSettings.ambientLight);
                Note(restaurantView != null && num6 > 0.99f && num11 < 0.5f, "Sokak gece koyulasiyor (sabah " + num6.ToString("0.00") + " -> aksam " + num11.ToString("0.00") + ")");
                Note(num12 > num7 * 1.3f, "Aksam ortam isigi yansimanin yerini tutuyor (" + num7.ToString("0.00") + " -> " + num12.ToString("0.00") + ")");
                light.Apply(_app.Sim.Phase, (float)_app.Sim.ServiceProgressBp / 10000f);
            }
            RestaurantView restaurantView2 = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            string text5 = ((restaurantView2 != null) ? restaurantView2.BadgeShaderName : null);
            Note(text5 == "Universal Render Pipeline/Unlit", "Rozet malzemesi isiksiz (" + (text5 ?? "yok") + ")");
            int num13 = (HalaDolu(_secilenMasa) ? _secilenMasa : DoluMasa());
            float bekleme = 0f;
            while (num13 < 0 && bekleme < 12f && _app.Sim.ServiceProgressBp < 7000)
            {
                bekleme += Time.deltaTime;
                yield return null;
                num13 = DoluMasa();
            }
            if (num13 >= 0)
            {
                basildi = _app.Paused;
                _app.Paused = true;
                _app.SelectedTable = num13;
                Note(_app.ValidSelection() == num13, "Masa secimi tutuyor");
                gunBasi = _app.Sim.InterventionsLeft;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return Settle();
                yield return Shot("16-masa-secili");
                sure = 0f;
                while (_app.Sim.WaitingParties <= 0 && sure < 12f)
                {
                    sure += Time.deltaTime;
                    yield return null;
                }
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return Settle();
                calisanKare = _app.Sim.WaitingParties;
                NoteIf(calisanKare > 0, Click(Loc.T("ui.service.tea")), "Salona cay (" + calisanKare + " bekleyen masa)");
                yield return Settle();
                NoteIf(calisanKare > 0, _app.Sim.InterventionsLeft < gunBasi, "Mudahale hakki dustu");
                _app.SelectedTable = _app.Sim.TableCount + 5;
                Note(_app.ValidSelection() < 0, "Gecersiz secim dusuyor");
                _app.SelectedTable = -1;
                _app.Paused = basildi;
            }
            else
            {
                // "BULUNAMADI" IKI AYRI SEY OLABILIR.
                //
                // Gun daha gencken hicbir masa dolmadiysa bu bir HATA:
                // servis calismiyor demektir. Ama bekleme gunun %70'ine
                // dayandigi icin bittiyse olculememis demektir - gun
                // zaten kapanmak uzere ve son misafirler kalkmis.
                //
                // Ikisini tek kirmiziya katlamak yaniltiyordu: talebe
                // gunluk oynaklik eklenince sakin bir 1. gun bu kontrolu
                // duserdi ve sebebi "servis bozuk" diye okunurdu.
                NoteIf(_app.Sim.ServiceProgressBp < 7000, false,
                       "Servis sirasinda dolu masa bulunamadi (servis %"
                       + (_app.Sim.ServiceProgressBp / 100) + ", bugun "
                       + _app.Sim.PlannedPeopleToday + " kisi bekleniyordu)");
            }
            _app.TimeScale = turHiz;
            if (_app.Rig != null)
            {
                Vector3 once2 = ((Component)_app.Rig).transform.position;
                calisanKare = RoomPlan.FirstDiningRoom();
                _app.Rig.FocusOn(calisanKare);
                yield return Settle();
                yield return Settled();
                Note(_app.Rig.FocusRoom == calisanKare, "Odaya yaklasildi");
                Note(Vector3.Distance(((Component)_app.Rig).transform.position, once2) > 1f, "Kamera gercekten hareket etti");
                yield return Shot("17-oda-gorunumu");
                _app.Rig.Overview();
                yield return Settled();
                float num14 = Vector3.Distance(((Component)_app.Rig).transform.position, _app.Rig.OverviewPosition);
                Note(_app.Rig.FocusRoom < 0 && num14 < 0.5f, "Genel gorunume donuldu (" + num14.ToString("0.00") + " m)");
            }
            else
            {
                Note(ok: false, "Kamera bulunamadi");
            }
            _app.TimeScale = was;
            yield return CheckAudio();
            if (ClickNamed("pause"))
            {
                yield return Settle();
            }
            Note(ClickNamed("menu"), "Menu dugmesi");
            yield return Settle();
            yield return Shot("08-duraklatma");
            if (Click(Loc.T("ui.menu.settings")))
            {
                yield return Settle();
                yield return Shot("09-ayarlar");
                Back();
                yield return Settle();
            }
            Back();
            yield return Settle();
            bool paused = _app.Paused;
            Note(paused, "Duraklat oyunu durdurdu");
            if (paused)
            {
                Note(ClickNamed("pause"), "Devam dugmesi");
                yield return Settle();
                Note(!_app.Paused, "Oyun yeniden akiyor");
            }
            if (_app.Ui != null)
            {
                _app.Ui.Push(new LoanScreen());
            }
            yield return Settle();
            Note(HasText(Loc.T("ui.loan.repay")), "Kredi ekraninda geri odeme");
            Note(HasText(Loc.T("ui.loan.installment")), "Kredi ekraninda taksit");
            basildi = _app.Sim.HasLoan;
            Note(Click(Loc.T("ui.loan.take")), "Kredi cekme dugmesi");
            yield return Settle();
            Note(_app.Sim.HasLoan && !basildi, "Kredi cekildi");
            Note(HasText(Loc.T("ui.loan.one_at_a_time")), "Ikinci kredi kapali");
            Back();
            yield return Settle();
            sure = _app.TimeScale;
            _app.TimeScale = 16f;
            _app.Paused = false;
            calisanKare = _app.Sim.ServiceProgressBp;
            aciEnKotu = 0f;
            while (!_app.Sim.ServiceComplete && aciEnKotu < 60f)
            {
                aciEnKotu += Time.deltaTime;
                yield return null;
            }
            _app.TimeScale = sure;
            Debug.Log((object)("  TANI gun sonu: " + aciEnKotu.ToString("0.0") + " sn, servis " + calisanKare / 100 + "% -> " + _app.Sim.ServiceProgressBp / 100 + "%, bitti=" + _app.Sim.ServiceComplete));
            Note(_app.Sim.ServiceComplete, "Servis gunu sonuna geldi (" + _app.Sim.ServiceProgressBp / 100 + "%)");
            yield return Settle();
            int angrySeatedParties = _app.Sim.AngrySeatedParties;
            int crisisBuilds = GameScreen.CrisisBuilds;
            Debug.Log((object)("  TANI kriz seridi: " + angrySeatedParties + " masadan kizgin, " + _app.Sim.AngryParties + " toplam kizgin, serit " + crisisBuilds + " kez kuruldu, " + _krizGorulen + " kritik masa goruldu"));
            // BURADAKI IKI KONTROL SILINDI - YAPISAL OLARAK KOSAMAZLARDI.
            //
            // Birinci gun dort masa ve ~12 kisi: kriz imkansiz. Ikisi de
            // her kosuda "0 kizgin, 0 kritik masa" deyip OLCULEMEDI
            // donuyordu ve bu, aylarca kapsama YANILSAMASI uretti - kriz
            // seridinin arayuzu sinaniyor sanildi, hic sinanmiyordu.
            //
            // Hicbir zaman kosamayan bir kontrol, hic olmayan bir
            // kontrolden KOTUDUR: ikincisi eksik oldugunu soyler.
            //
            // Yerine kampanya boyu olcen tek bir kontrol kondu (asagida,
            // dongunun sonunda): 60 gunu ve 20-21. gunlerde bilerek eksik
            // calisilan hafta sonunu goruyor. Ilk kosusunda 30 kizgin ve
            // serit 186 kez buldu.
            //
            // Tani satiri duruyor: birinci gunun sakinligi de bir bilgi.
            _ = angrySeatedParties;
            Note(Click(Loc.T("ui.service.close")), "Gunu kapat");
            int platesDirtiedToday = _app.Sim.PlatesDirtiedToday;
            int platesWashedToday = _app.Sim.PlatesWashedToday;
            int platesTotal = _app.Sim.PlatesTotal;
            Note(platesDirtiedToday > 0, "Tabaklar kirleniyor (" + platesDirtiedToday + " adet)");
            RestaurantView restaurantView3 = UnityEngine.Object.FindFirstObjectByType<RestaurantView>();
            Note(restaurantView3 != null && restaurantView3.WashSeenFrames > 0, "Lavaboda yikayan goruldu (" + ((restaurantView3 != null) ? restaurantView3.WashSeenFrames : 0) + " kare)");
            Note(platesWashedToday > 0, "Tabaklar yikaniyor (" + platesWashedToday + " adet)");
            Note(_app.Sim.PlatesClean + _app.Sim.PlatesInUse + _app.Sim.PlatesDirty == platesTotal, "Tabak sayisi korunuyor (" + _app.Sim.PlatesClean + "+" + _app.Sim.PlatesInUse + "+" + _app.Sim.PlatesDirty + "=" + platesTotal + ")");
            yield return Settle();
            yield return Shot("10-aksam");
            yield return CheckStripsBothLanguages("aksam");
            if (_app.Sim.BuildDayReport().SpoiledValue > 0)
            {
                Note(HasText(Loc.T("ui.evening.spoiled")), "Aksam seridinde cope giden");
                if (Click(Loc.T("ui.evening.title")))
                {
                    yield return Settle();
                    Note(HasText(Loc.T("ui.evening.spoiled")), "Gun raporunda cope giden");
                    // DEFTER KONTROLU BURADAN KALKTI.
                    //
                    // Burasi BIRINCI gunun raporu; veresiye ise 16. gunde
                    // aciliyor, yani defter burada HER ZAMAN bos ve
                    // kontrol her kosuda OLCULEMEDI donuyordu. Kapsam
                    // kaybi yesil bir turda gorunmuyor: ozet yine
                    // "0 kaldi" diyor. Kontrol artik kampanya dongusune,
                    // hesap ACILDIKTAN sonraya tasindi.
                    Back();
                    yield return Settle();
                }
            }
            if (_app.InGame)
            {
                Note(_app.SaveToSlot(0), "Yuvaya kayit");
                int day = _app.Sim.Day;
                long cash = _app.Sim.Cash;
                Note(_app.LoadSlot(0), "Yuvadan yukleme");
                Note(_app.InGame && _app.Sim.Day == day && _app.Sim.Cash == cash, "Kayit ayni durumu geri veriyor");
            }
            else
            {
                Note(ok: false, "Kayit denemesi - oyun baslamamisti");
            }
            yield return Settle();
            yield return LongRun((_app.Sim != null) ? (_app.Sim.CampaignDays + 1) : 61);
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            if (Click(Loc.T("ui.menu.credits")))
            {
                yield return Settle();
                yield return Shot("11-yapimci");
                if (Click("lisans"))
                {
                    yield return Settle();
                    Note(LicenseTextsLoaded(), "Lisans metinleri yapida");
                    yield return Shot("14-lisanslar");
                    Back();
                    yield return Settle();
                }
                Back();
                yield return Settle();
            }
            NoteIf(_shotHash.Count >= 2, _shotDup == null, (_shotDup == null) ? ("Her goruntu ayri bir ekran (" + _shot + " goruntu, " + _shotHash.Count + " ayri resim)") : ("Ayni resim iki ad altinda: " + _shotDup));
            Debug.Log((object)"=== Lokanta tur ===");
            foreach (string item in _log)
            {
                Debug.Log((object)("  " + item));
            }
            Debug.Log((object)("  " + _shot + " goruntu: " + _dir));
            Debug.Log((object)("  ozet: " + _gecti + " gecti, " + _kaldi + " kaldi, " + _olculemedi + " olculemedi"));
            if (_kaldi > 0)
            {
                Debug.LogError((object)("SORUNLAR: turda " + _kaldi + " kontrol kaldi"));
            }
            try
            {
                File.WriteAllText(Path.Combine(_dir, "ozet.txt"), "gecti=" + _gecti + Environment.NewLine + "kaldi=" + _kaldi + Environment.NewLine + "olculemedi=" + _olculemedi + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning((object)("Tur ozeti yazilamadi: " + ex.Message));
            }
            Debug.Log((object)"=== tur tamam ===");
            yield return (object)new WaitForSeconds(0.5f);
            _cikiyor = true;
        }

        private void Update()
        {
            if (_cikiyor)
            {
                _cikiyor = false;
                Application.Quit();
            }
        }

        private IEnumerator LongRun(int days)
        {
            if (!_app.InGame)
            {
                Note(ok: false, "Uzun kosu - oyun yok");
                yield break;
            }
            float was = _app.TimeScale;
            _app.TimeScale = 1800f;
            int yarimGun = 0;
            int stokBasarisiz = 0;
            int gunlukServis = 0;
            int baslangicMasa = _app.Sim.TableCount;
            bool magazaAlindi = false;
            bool veresiyeYazildi = false;
            bool defterOlculdu = false;
            int kizginToplam = 0;
            bool kaliteOlculdu = false;
            bool karneOlculdu = false;
            bool nisanOlculdu = false;
            int started = _app.Sim.Day;
            string trouble = null;
            for (int d = 0; d < days; d++)
            {
                if (trouble != null)
                {
                    break;
                }
                if (_app.Sim.Day > _app.Sim.CampaignDays)
                {
                    break;
                }
                yield return ToGameScreen();
                if (_app.Sim.Phase == DayPhase.Evening)
                {
                    Click(Loc.T("ui.evening.next"));
                    yield return Settle();
                    yield return ToGameScreen();
                }
                // KRIZ YOLUNU BILEREK KOSTURUYORUZ.
                //
                // Tur hep gereken kadroyu kuruyor ve tam kadro krizi
                // soguruyor - o yuzden "masadan kizgin ayrilan varsa kriz
                // seridi kuruldu" ve "kritik masa goruldugunde serit
                // kurulmus" kontrolleri AYLARDIR "0 kizgin, 0 kritik masa"
                // diyip olculemeden geciyordu. Kriz seridinin arayuzu hic
                // sinanmamisti.
                //
                // 20. ve 21. gunler hafta sonu (haftanin son iki gunu) ve
                // dukkan o zamana kadar buyumus oluyor: bir garson eksik
                // calismak gercek bir kuyruk uretiyor. Bu ayni zamanda
                // oyunun odullendirdigi oyun - "Zirveyi eksik kadroyla
                // gectin" nisani (docs/47) tam da bunu taniyor.
                //
                // Kadro 22. gunde Buyu() tarafindan kendiliginden geri
                // kuruluyor; ayrica bir sey yapmak gerekmiyor.
                bool kriziKostur = _app.Sim.Day == 20 || _app.Sim.Day == 21;
                if (kriziKostur && _app.Sim.SalonStaff > 1)
                    _app.Send(CommandKind.Fire, 1, _app.Sim.SalonStaff - 1);
                else
                    Buyu();

                if (Click(Loc.T("ui.morning.market")))
                {
                    yield return Settle();

                    // MALZEME KALITESI: mekanik cekirdekte eksiksizdi ve
                    // hicbir ekranda dugmesi yoktu (docs/49). Tur artik
                    // kademeyi GERCEKTEN degistirip simulasyona gectigini
                    // dogruluyor - dugmenin var olmasi yetmez, ayni hata
                    // bu projede iki kez "dugme var ama komut gitmiyor"
                    // seklinde cikti.
                    if (!kaliteOlculdu)
                    {
                        int onceki = _app.Sim.Quality;
                        int hedef = onceki == 2 ? 1 : 2;
                        if (Click(Loc.T(hedef == 2 ? "ui.quality.2" : "ui.quality.1")))
                        {
                            yield return Settle();
                            Note(_app.Sim.Quality == hedef,
                                 "Hal'de kalite kademesi degisiyor (" + onceki
                                 + " -> " + _app.Sim.Quality + ")");
                            // Eski kademeye donuluyor: tur kampanyanin
                            // geri kalanini olcuyor ve kaliteyi kalici
                            // degistirmek butun sonraki sayilari kaydirirdi.
                            Click(Loc.T(onceki == 0 ? "ui.quality.0"
                                      : onceki == 1 ? "ui.quality.1" : "ui.quality.2"));
                            yield return Settle();
                            Note(_app.Sim.Quality == onceki,
                                 "Kalite kademesi geri alinabiliyor");
                            kaliteOlculdu = true;
                        }
                    }

                    if (!Click(Loc.T("ui.morning.restock")))
                    {
                        stokBasarisiz++;
                    }
                    yield return Settle();
                    Click(Loc.T("ui.common.ok"));
                    yield return Settle();
                }
                if (!Click(Loc.T("ui.morning.open")))
                {
                    trouble = "servis acilmadi, gun " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
                if (!magazaAlindi && OlcekBayragi() > 1.01f && _app.Sim.Day >= 40)
                {
                    float eskiHiz = _app.TimeScale;
                    float ileri = 0f;
                    int hedefMasa = _app.Sim.TableCount / 2;
                    while (ileri < 45f && _app.Sim.ServiceProgressBp < 8000 && (_app.Sim.OccupiedTables < hedefMasa || _app.Sim.BuildDayReport().Revenue <= 0))
                    {
                        ileri += Time.deltaTime;
                        yield return null;
                    }
                    _app.TimeScale = GameApp.BaseTimeScale;
                    if (_app.Ui != null)
                    {
                        _app.Ui.Refresh();
                    }
                    yield return Settle();
                    // ARAMA SABRI 30 -> 75 SANIYE.
                    //
                    // Bar DUSURULMEDI, arama uzatildi. Self servis
                    // masa devrini hizlandiriyor (servis ve odeme
                    // beklemesi yok), yani ayni anda dolu masa sayisi
                    // dusuyor ve "yarisi dolu" ani daha DAR bir pencerede
                    // yasaniyor. Bir kosu 4/14'te sureye takildi; ayni
                    // yapida baska kosular 8/14 ve 13/14 gordu, yani esik
                    // ulasilabilir - eksik olan sabirdi.
                    //
                    // Esigi dusurmek yanlis olurdu: magaza goruntusunun
                    // isi dolu bir lokanta gostermek ve "self serviste
                    // zaten bos olur" demek, goruntuyu oyunun en sakin
                    // anina razi etmek olurdu.
                    float bek = 0f;
                    while (bek < 75f && (_app.Sim.OccupiedTables < _app.Sim.TableCount / 2 || _app.Sim.BuildDayReport().Revenue <= 0 || _app.NoticeCount > 0))
                    {
                        bek += Time.deltaTime;
                        yield return null;
                    }
                    yield return Shot("20-magaza-servis");
                    Note(_app.NoticeCount == 0, "Magaza goruntusunde salonun ustu acik (" + _app.NoticeCount + " balon)");
                    Note(_app.Sim.OccupiedTables >= _app.Sim.TableCount / 2, "Magaza goruntusunde salon dolu (" + _app.Sim.OccupiedTables + "/" + _app.Sim.TableCount + " masa)");
                    GameScreen gameScreen = ((_app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
                    if (gameScreen != null)
                    {
                        int overlappingButtons = gameScreen.OverlappingButtons;
                        Note(overlappingButtons == 0, "ust uste binen dugme (gun " + _app.Sim.Day + ", " + _app.Sim.TableCount + " masa): " + overlappingButtons + " cift" + ((overlappingButtons > 0) ? (" - " + gameScreen.OverflowDetail) : ""));
                        int clippedButtons = gameScreen.ClippedButtons;
                        Note(clippedButtons == 0, "kirpilan yazi (gun " + _app.Sim.Day + "): " + clippedButtons + ((clippedButtons > 0) ? (" - " + gameScreen.ClipDetail) : ""));
                    }
                    Debug.Log((object)("  MAGAZA goruntusu: gun " + _app.Sim.Day + ", " + _app.Sim.TableCount + " masa, " + _app.Sim.OccupiedTables + " dolu"));
                    _app.TimeScale = eskiHiz;
                    magazaAlindi = true;
                }
                float butce = 6f + (float)_app.Sim.TableCount * 0.8f;
                float guard = 0f;
                while (!_app.Sim.ServiceComplete && guard < butce)
                {
                    // VERESIYE: GUNUN ICINDE, SERVIS ACILISINDA DEGIL.
                    //
                    // Iki kez yanlis yere koydum. Once 1. gundeydi ve
                    // mekanik 16. gunde aciliyor; sonra servis acilisina
                    // aldim ve o anda henuz OTURMUS musteri yok, yani
                    // veresiye isteyen de yok. Ikisinde de blok sessizce
                    // atlandi, defter bos kaldi ve defter EKRANI hic
                    // sinanmadi - tur her seferinde "0 kaldi" dedi.
                    // Kapsam kaybi yesil bir turda gorunmuyor.
                    //
                    // Dogru yer gunun ICI: musteri oturuyor, istiyor,
                    // ve tur o ani yakaliyor.
                    if (!veresiyeYazildi && _app.Sim.HasCredit
                        && _app.Sim.FirstCreditAsker() >= 0)
                    {
                        // Duraklatiliyor: dugme "isteyen var mi"ya gore
                        // KURULUYOR ve kurulusla tiklama arasinda bir
                        // kare geciyor. O karede musteri kalkarsa komut
                        // reddedilir ve olculen sey mekanik degil turun
                        // kendi gecikmesi olur.
                        bool duraklatildiOnce = _app.Paused;
                        _app.Paused = true;
                        _app.SelectedTable = -1;
                        if (_app.Ui != null)
                        {
                            _app.Ui.Refresh();
                        }
                        yield return Settle();

                        int isteyen = _app.Sim.FirstCreditAsker();
                        if (isteyen >= 0 && Click(Loc.T("ui.service.credit")))
                        {
                            yield return Settle();

                            // OLCUT "DEFTERDE PARA" DEGIL, GRUBUN ISARETI.
                            // ExtendCredit deftere HEMEN yazmiyor; kayit
                            // hesap ODENDIGINDE olusuyor. OpenCredit'e
                            // bakmak, komutu reddedilmis sanmak demekti -
                            // bir kez tam bunu yaptim ve tur kirmizi verdi.
                            Note(_app.Sim.PartyHasCredit(isteyen),
                                 "Masaya veresiye acildi");
                            veresiyeYazildi = true;
                        }
                        _app.Paused = duraklatildiOnce;
                    }

                    guard += Time.deltaTime;
                    yield return null;
                }
                if (!_app.Sim.ServiceComplete)
                {
                    yarimGun++;
                }
                gunlukServis += _app.Sim.BuildDayReport().ServedParties;

                if (!Click(Loc.T("ui.service.close")))
                {
                    trouble = "gun kapanmadi, gun " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
                if (_app.Ui.Top is StoryScreen)
                {
                    _stories++;
                    if (_stories == 1)
                    {
                        yield return Shot("15-hikaye");
                    }
                    Click(Loc.T("ui.story.continue"));
                    yield return Settle();
                }
                // VERESIYE DEFTERI EKRANI.
                //
                // BURADA, cunku "Gun raporu" dugmesi AKSAM ekraninda -
                // gun kapandiktan sonra. Bir onceki denemem gunu
                // kapatmadan once ariyordu ve dugme yoktu: blok sessizce
                // atlandi, kontrol hic kosmadi ve tur yine "0 kaldi"
                // dedi.
                //
                // Defter kaydi hesap ODENDIGINDE olusuyor, yani veresiye
                // acilan gunun sonunda; o yuzden OpenCredit'e bakiliyor.
                if (!defterOlculdu && _app.Sim.OpenCredit > 0
                    && Click(Loc.T("ui.evening.title")))
                {
                    yield return Settle();
                    if (Click(Loc.T("ui.ledger.title")))
                    {
                        yield return Settle();
                        Note(HasText(Loc.T("ui.ledger.chance_wait")),
                             "Defterde odeme sansi yaziyor ("
                             + _app.Sim.TabCount + " hesap)");
                        Note(HasText(Loc.T("ui.ledger.chase")),
                             "Defterde kovalama dugmesi var");

                        // DUGMEYE BASILIYOR - varligini gormek yetmiyor.
                        //
                        // Bu kontrol yillardir dugmenin EKRANDA oldugunu
                        // dogruluyordu ve hicbir sey ona BASMIYORDU. Oysa
                        // bu projede iki kez "dugme var ama komut
                        // gitmiyor" cikti; varlik testi o hatayi
                        // goremezdi.
                        //
                        // Kovalamak hesabi KAPATIYOR (SettleTab
                        // halfChance) - tahsil edilse de edilmese de.
                        // Olcut bu yuzden hesap sayisi: tutar degisimi
                        // tahsilatin tutmasina bagli ve kontrolu zara
                        // baglamak olurdu.
                        int hesapOnce = _app.Sim.TabCount;
                        if (Click(Loc.T("ui.ledger.chase")))
                        {
                            yield return Settle();
                            Note(_app.Sim.TabCount < hesapOnce,
                                 "Kovalama hesabi kapatiyor (" + hesapOnce
                                 + " -> " + _app.Sim.TabCount + " hesap)");
                        }
                        // CheckStrips BURADA YANLIS DENETIM: alt SERIDI
                        // olcuyor ve defter bir liste ekrani, seridi yok.
                        // "serit olculemedi" diye kirmizi veriyordu -
                        // olcum ekranin degil kendisinin hatasiydi.
                        defterOlculdu = true;
                        Back();
                        yield return Settle();
                    }
                    Back();
                    yield return Settle();
                }

                // HAFTALIK KARNE VE NISANLAR.
                //
                // Ikisi de AKSAM ekraninda, "Gun raporu" dugmesinin
                // ardinda - yani gun kapandiktan sonra, ertesi gune
                // gecmeden once. Defter kontrolunun dort kez yanlis yere
                // konmasindan ogrenilen ders: kontrolun kosabilecegi TEK
                // an burasi.
                //
                // "Bugun kazanildi" isareti AdvanceToNextDay'de
                // siliniyor, yani ertesi gun bakmak hep sifir gorurdu ve
                // kontrol sessizce hic kosmazdi.
                // GUN KAPANDIKTAN SONRA sayiliyor. Ilk yazdigimda sabaha
                // koymustum ve AdvanceToNextDay sayaci yeni sifirladigi
                // icin toplam hep 0 kaliyordu - kontrol kosar ama hicbir
                // sey olcmezdi.
                kizginToplam += _app.Sim.AngrySeatedParties;

                bool karneVar = _app.Sim.WeekReportReady;
                bool nisanVar = false;
                for (int b = 0; b < _app.Sim.BadgeCount; b++)
                    if (_app.Sim.BadgeEarnedToday(b)) { nisanVar = true; break; }

                if ((karneVar && !karneOlculdu) || (nisanVar && !nisanOlculdu))
                {
                    if (Click(Loc.T("ui.evening.title")))
                    {
                        yield return Settle();
                        if (karneVar && !karneOlculdu)
                        {
                            Note(HasText(Loc.T("ui.week.note")),
                                 "Haftalik karne aksam raporunda (" 
                                 + _app.Sim.WeekNumber + ". hafta)");
                            // Eksenin ADI da gorunmeli: yalnizca basligin
                            // olmasi karnenin BOS cikmasini yakalamaz.
                            Note(HasText(Loc.T(SeasonScore.AxisKey(1))),
                                 "Karnede itibar ekseni yaziyor");
                            karneOlculdu = true;
                        }
                        if (nisanVar && !nisanOlculdu)
                        {
                            Note(HasText(Loc.T("ui.badge.earned")),
                                 "Kazanilan nisan aksam raporunda");
                            nisanOlculdu = true;
                        }
                        Back();
                        yield return Settle();
                    }
                }

                if (!Click(Loc.T("ui.evening.next")))
                {
                    trouble = "ertesi gune gecilmedi, gun " + _app.Sim.Day;
                    break;
                }
                yield return Settle();
            }
            _app.TimeScale = was;
            int num = _app.Sim.Day - started;
            Note(trouble == null, trouble ?? (num + " gun kesintisiz oynandi"));
            Note(_app.Sim.Day > started, "Gun ilerledi: " + started + " -> " + _app.Sim.Day);
            Note(_stories > 0, _stories + " mudavim hikaye sahnesi goruldu");
            Note(yarimGun == 0, "Her gun servis tamamlandi (" + yarimGun + " gun yarida kesildi)");
            Note(stokBasarisiz == 0, "Her sabah stok tazelendi (" + stokBasarisiz + " gun alinamadi)");
            Note(gunlukServis > 0, "Altmis gunde musteri agirlandi (" + gunlukServis + " grup)");
            Note(_app.Sim.TableCount > 4, "Kampanya boyunca genisledi (" + baslangicMasa + " -> " + _app.Sim.TableCount + " masa)");

            // KARNE VE NISAN HIC OLCULMEDIYSE BUNU SOYLE.
            //
            // Bayrak false kalirsa dongu icindeki blok hic kosmamis
            // demektir - ve kosmayan bir kontrol, gecen bir kontrolle
            // disaridan AYNI gorunuyor. Sessiz kalmak, ozelligin
            // sinandigini sanmak olurdu.
            NoteIf(kaliteOlculdu, kaliteOlculdu,
                   "Hal'de kalite secicisi goruldu");
            NoteIf(karneOlculdu, karneOlculdu,
                   "Haftalik karne goruldu (60 gunde en az bir hafta)");
            NoteIf(nisanOlculdu, nisanOlculdu,
                   "Kazanilan nisan aksam raporunda goruldu");
            Note(_app.Sim.BadgesEarned > 0,
                 "Kampanyada nisan kazanildi (" + _app.Sim.BadgesEarned
                 + " / " + _app.Sim.BadgeCount + ")");

            // KRIZ SERIDI KAMPANYA BOYUNCA OLCULUYOR.
            //
            // Ayni kontrol 1. gunun teftis blogunda da var ama orada ASLA
            // kosamiyor: birinci gun dort masa ve ~12 kisi, yani kriz
            // yapisal olarak imkansiz. Aylardir "0 kizgin, 0 kritik masa"
            // deyip olculemeden geciyordu - kriz seridinin arayuzu hic
            // sinanmamisti.
            //
            // Burasi 60 gunu goruyor, 20-21. gunlerde bilerek eksik
            // calisilan hafta sonu dahil. CrisisBuilds birikimli bir
            // sayac oldugu icin kampanyanin tamamini kapsiyor.
            Debug.Log("  TANI kampanya krizi: " + kizginToplam
                      + " masadan kizgin, serit " + GameScreen.CrisisBuilds + " kez");
            NoteIf(kizginToplam > 0, GameScreen.CrisisBuilds > 0,
                   "Kampanyada kizgin olunca kriz seridi kuruldu ("
                   + kizginToplam + " kizgin, serit "
                   + GameScreen.CrisisBuilds + " kez)");
            int num2 = 0;
            long num3 = 0L;
            Renderer[] array = UnityEngine.Object.FindObjectsByType<Renderer>((FindObjectsSortMode)0);
            foreach (Renderer val in array)
            {
                if (!val.enabled || !((Component)val).gameObject.activeInHierarchy)
                {
                    continue;
                }
                num2++;
                Mesh val2 = null;
                MeshFilter component = ((Component)val).GetComponent<MeshFilter>();
                if (component != null)
                {
                    val2 = component.sharedMesh;
                }
                SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((val is SkinnedMeshRenderer) ? val : null);
                if (val3 != null)
                {
                    val2 = val3.sharedMesh;
                }
                if (!(val2 == null))
                {
                    for (int j = 0; j < val2.subMeshCount; j++)
                    {
                        num3 += val2.GetIndexCount(j) / 3;
                    }
                }
            }
            Debug.Log((object)("  TANI sahne butcesi: " + num2 + " cizici, " + num3 + " ucgen (" + _app.Sim.TableCount + " masa)"));
            Note(num2 < 400, "Taban sahnenin cizici sayisi butcede (" + num2 + " < 400, " + _app.Sim.TableCount + " masa)");
            Note(num3 < 80000, "Taban sahnenin ucgen sayisi butcede (" + num3 + " < 80 bin, " + _app.Sim.TableCount + " masa)");
            yield return Settle();
            bool flag = _app.Ui.Top is EndScreen;
            Note(flag, "Yil sonu degerlendirmesi acildi");
            Note(_app.Ui.Depth == 2, "Yil sonu ekrani TEK kez acildi (yigin " + _app.Ui.Depth + ", beklenen 2)");
            if (!flag)
            {
                yield break;
            }
            yield return Shot("13-degerlendirme");
            // EKSEN SAYISI SIMGESEL. Geri cevirmeden sonra burada "7"
            // SABITI duruyordu (uc yerde): dongu, son eksenin indisi ve
            // iddia. Eksen sayisi degisirse tur yeni ekseni HIC aramaz
            // ve eski sayiya gore konusurdu.
            int gorunen = 0;
            for (int k = 0; k < SeasonScore.AxisCount; k++)
            {
                string key = (k == SeasonScore.AxisCount - 1)
                    ? _app.Content.ScoreAxis.NameKey
                    : SeasonScore.AxisKey(k);
                if (VisibleText(Loc.T(key)))
                {
                    gorunen++;
                }
            }
            Note(gorunen == SeasonScore.AxisCount,
                 $"degerlendirmede {gorunen}/{SeasonScore.AxisCount} eksen gorunuyor");
            SeasonScore seasonScore = _app.Sim.Score();
            Debug.Log((object)$"  puan: toplam {seasonScore.Total}, plaket {seasonScore.Plaque}  (varlik {seasonScore.Wealth} itibar {seasonScore.Reputation} duzenli {seasonScore.Regulars} ekip {seasonScore.Crew} mekan {seasonScore.Place} saglamlik {seasonScore.Resilience} mutfak {seasonScore.Signature})");
        }

        private IEnumerator ToGameScreen()
        {
            int guard = 0;
            while (_app.Ui.Depth > 1 && guard++ < 8)
            {
                _app.Ui.Pop();
                yield return null;
            }
            yield return Settle();
        }

        private IEnumerator CheckAudio()
        {
            AudioSource sfx = null;
            AudioSource[] components = ((Component)_app).GetComponents<AudioSource>();
            foreach (AudioSource val in components)
            {
                if (!val.loop)
                {
                    sfx = val;
                }
            }
            Note(sfx != null, "Ses kaynagi kurulu");
            if (sfx != null)
            {
                Sfx.Coin();
                yield return null;
                Note(sfx.isPlaying, "Ses efekti caliyor");
            }
            Log($"ses dosyasi: {Sfx.FileBackedCount()}/10 " + "(gerisi sentez; Art/ATIF.md)");
            Note(_app.Music != null, "Muzik bileseni var");
            if (_app.Music != null)
            {
                AudioSource component = ((Component)_app.Music).GetComponent<AudioSource>();
                Note(component != null && component.clip != null, "Muzik klibi uretildi");
                Note(component != null && component.isPlaying, "Muzik caliyor");
            }
            yield return null;
        }

        private IEnumerator BozukYuva()
        {
            string text = Path.Combine(Application.persistentDataPath, "kayit");
            Directory.CreateDirectory(text);
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            // SURUM SIMGESEL OLMALI.
            //
            // Geri cevirmeden sonra burada "19" SABITI duruyordu: C#
            // const'u CAGIRANA gomuyor ve SaveVersion o gun 18'di. Sayi
            // SaveVersion ile birlikte artmayi birakinca "yanlis surum"
            // yazan satir DOGRU surumu yazar oldu ve uc kontrol kirildi.
            string contents = string.Join("\u001f", "turk", "12", "500", "7000", DateTime.UtcNow.Ticks.ToString(invariantCulture), (Simulation.SaveVersion + 1).ToString(invariantCulture));
            File.WriteAllText(Path.Combine(text, "yuva" + 3 + ".ozet.json"), contents);
            File.WriteAllText(Path.Combine(text, "yuva" + 3 + ".json"), "{}");
            _app.Ui.Replace(new MainMenuScreen());
            yield return Settle();
            Note(Click(Loc.T("ui.menu.continue")), "Bozuk kayit varken ana menude Devam");
            yield return Settle();
            Note(HasText(Loc.T("ui.slot.broken")), "Bozuk yuva bozuk gorunuyor");
            Note(HasText(Loc.T("ui.slot.unloadable")), "Bozuk yuvanin dugmesi Yuklenemiyor diyor");
            Note(!Click(Loc.T("ui.menu.continue")), "Bozuk yuva yuklenmiyor");
            yield return Settle();
            SaveStore.Delete(3);
            Note(!SaveStore.Read(3).Exists, "Bozuk yuva silinebiliyor");
        }

        private IEnumerator Settled()
        {
            float bek = 0f;
            while (_app != null && _app.Rig != null && _app.Rig.Moving && bek < 3f)
            {
                bek += Time.deltaTime;
                yield return null;
            }
            yield return null;
        }

        private IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return (object)new WaitForEndOfFrame();
        }

        private void RecordShot(string name, byte[] png)
        {
            if (png == null || png.Length == 0)
            {
                return;
            }
            ulong num = 14695981039346656037uL;
            for (int i = 0; i < png.Length; i++)
            {
                num ^= png[i];
                num *= 1099511628211L;
            }
            string key = png.Length + ":" + num.ToString("x16");
            if (_shotHash.TryGetValue(key, out var value))
            {
                if (_shotDup == null)
                {
                    _shotDup = value + " = " + name;
                }
            }
            else
            {
                _shotHash[key] = name;
            }
        }

        private IEnumerator Shot(string name)
        {
            float hiz = ((_app != null) ? _app.TimeScale : 0f);
            if (_app != null && hiz > 64f)
            {
                _app.TimeScale = GameApp.BaseTimeScale;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                }
                yield return null;
            }
            yield return (object)new WaitForEndOfFrame();
            Texture2D obj = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] array = ImageConversion.EncodeToPNG(obj);
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), array);
            RecordShot(name, array);
            UnityEngine.Object.Destroy(obj);
            _shot++;
            if (_app != null && !Mathf.Approximately(_app.TimeScale, hiz))
            {
                _app.TimeScale = hiz;
                if (_app.Ui != null)
                {
                    _app.Ui.Refresh();
                    yield return Settle();
                }
            }
        }

        private IEnumerator CheckStripsBothLanguages(string asama)
        {
            CheckStrips(asama);
            Loc.SetLanguage(1);
            if (_app != null && _app.Ui != null)
            {
                _app.Ui.Refresh();
            }
            yield return Settle();
            CheckStrips(asama + "/en");
            Loc.SetLanguage(0);
            if (_app != null && _app.Ui != null)
            {
                _app.Ui.Refresh();
            }
            yield return Settle();
        }

        private void CheckStrips(string asama)
        {
            GameScreen gameScreen = ((_app != null && _app.Ui != null) ? (_app.Ui.Top as GameScreen) : null);
            if (gameScreen == null)
            {
                Note(ok: false, "serit olculemedi: " + asama);
                return;
            }
            float stripHeight = gameScreen.StripHeight;
            Note(stripHeight > 0f && stripHeight <= 220f, $"serit butcesi ({asama}): {stripHeight:0} dp");
            int overlappingButtons = gameScreen.OverlappingButtons;
            Note(overlappingButtons == 0, (overlappingButtons < 0) ? ("ust uste binen dugme (" + asama + "): OLCULEMEDI, serit kurulmamis") : ($"ust uste binen dugme ({asama}): {overlappingButtons} cift" + ((overlappingButtons > 0) ? (" - " + gameScreen.OverflowDetail) : "")));
            int clippedButtons = gameScreen.ClippedButtons;
            Note(clippedButtons == 0, (clippedButtons < 0) ? ("kirpilan yazi (" + asama + "): OLCULEMEDI, serit kurulmamis") : ($"kirpilan yazi ({asama}): {clippedButtons}" + ((clippedButtons > 0) ? (" - " + gameScreen.ClipDetail) : "")));
        }

        private bool VisibleText(string contains)
        {
            //IL_0051: Unknown result type (might be due to invalid IL or missing references)
            //IL_0056: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            //IL_0095: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null || string.IsNullOrEmpty(contains))
            {
                return false;
            }
            float height = val.resolvedStyle.height;
            foreach (Label item in UQueryExtensions.Query<Label>(val, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text) && ((TextElement)item).text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Rect worldBound = ((VisualElement)item).worldBound;
                    if (!(worldBound.height <= 0f) && !(worldBound.yMax <= 0f) && (!(height > 0f) || !(worldBound.yMin >= height)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasText(string contains)
        {
            //IL_0045: Unknown result type (might be due to invalid IL or missing references)
            //IL_004a: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null || string.IsNullOrEmpty(contains))
            {
                return false;
            }
            foreach (Label item in UQueryExtensions.Query<Label>(val, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text) && ((TextElement)item).text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool Click(string contains, int index = 0)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null)
            {
                return false;
            }
            int num = 0;
            foreach (Button item in UQueryExtensions.Query<Button>(val, (string)null, (string)null).ToList())
            {
                string text = ButtonText(item);
                if (!string.IsNullOrEmpty(text) && text.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0 && num++ == index)
                {
                    NavigationSubmitEvent pooled = NavigationEventBase<NavigationSubmitEvent>.GetPooled((EventModifiers)0);
                    try
                    {
                        ((EventBase)pooled).target = (IEventHandler)(object)item;
                        ((CallbackEventHandler)item).SendEvent((EventBase)(object)pooled);
                    }
                    finally
                    {
                        ((IDisposable)pooled)?.Dispose();
                    }
                    return true;
                }
            }
            return false;
        }

        private static string ButtonText(Button b)
        {
            //IL_0019: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            if (!string.IsNullOrEmpty(((TextElement)b).text))
            {
                return ((TextElement)b).text;
            }
            string text = null;
            foreach (Label item in UQueryExtensions.Query<Label>((VisualElement)(object)b, (string)null, (string)null).ToList())
            {
                if (!string.IsNullOrEmpty(((TextElement)item).text))
                {
                    text = ((text == null) ? ((TextElement)item).text : (text + " " + ((TextElement)item).text));
                }
            }
            return text ?? string.Empty;
        }

        private bool ClickNamed(string name)
        {
            VisualElement val = ((_app != null && _app.Ui != null) ? _app.Ui.TopView : null);
            if (val == null)
            {
                return false;
            }
            Button val2 = UQueryExtensions.Q<Button>(val, name, (string)null);
            if (val2 == null)
            {
                return false;
            }
            NavigationSubmitEvent pooled = NavigationEventBase<NavigationSubmitEvent>.GetPooled((EventModifiers)0);
            try
            {
                ((EventBase)pooled).target = (IEventHandler)(object)val2;
                ((CallbackEventHandler)val2).SendEvent((EventBase)(object)pooled);
            }
            finally
            {
                ((IDisposable)pooled)?.Dispose();
            }
            return true;
        }

        private static bool LicenseTextsLoaded()
        {
            string[] array = new string[3] { "lisans/rubik-ofl", "lisans/kenney-cc0", "lisans/motor-bilesenleri" };
            foreach (string text in array)
            {
                TextAsset val = Resources.Load<TextAsset>(text);
                if (val == null || val.text.Length < 200)
                {
                    Debug.LogWarning((object)("Lisans metni eksik: " + text));
                    return false;
                }
                Resources.UnloadAsset(val);
            }
            return true;
        }

        private void Buyu()
        {
            Simulation sim = _app.Sim;
            for (int i = 1; i < sim.TierCount; i++)
            {
                if (sim.TablesAtTier(i) > sim.TableCount)
                {
                    long num = sim.UpgradeCostFor(i);
                    if (num > 0 && sim.Cash >= num * 3)
                    {
                        _app.Send(CommandKind.Expand, i);
                    }
                    break;
                }
            }
            Crew crew = sim.RequiredCrewTomorrow();
            int num2 = 0;
            while (sim.Cooks < crew.Cooks && sim.Cooks + sim.SalonStaff < sim.StaffCap && num2++ < 12)
            {
                _app.Send(CommandKind.Hire);
            }
            while (sim.SalonStaff < crew.Salon && sim.Cooks + sim.SalonStaff < sim.StaffCap && num2++ < 24)
            {
                _app.Send(CommandKind.Hire, 1);
            }
        }

        private void Back()
        {
            if (_app != null && _app.Ui != null && _app.Ui.Depth > 1)
            {
                _app.Ui.Pop();
            }
        }

        private void Log(string what)
        {
            _log.Add("olcum : " + what);
        }

        private void Note(bool ok, string what)
        {
            if (ok)
            {
                _gecti++;
            }
            else
            {
                _kaldi++;
            }
            _log.Add((ok ? "tamam : " : "HATA  : ") + what);
            if (!ok)
            {
                Debug.LogWarning((object)("Tur: " + what + " calismadi"));
            }
        }

        private void Skip(string what)
        {
            _olculemedi++;
            _log.Add("OLCULEMEDI: " + what);
        }

        private void NoteIf(bool olctu, bool ok, string what)
        {
            if (olctu)
            {
                Note(ok, what);
            }
            else
            {
                Skip(what);
            }
        }

        private static string Slug(string s)
        {
            return s.ToLowerInvariant().Replace("ü", "u").Replace("ö", "o")
                .Replace("ı", "i")
                .Replace("ş", "s")
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace(" ", "-");
        }
    }
}
