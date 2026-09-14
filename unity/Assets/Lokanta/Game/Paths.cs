using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Salonun YURUME GEOMETRISI: kapi nerede, koridor nereden geciyor,
    /// bir masaya hangi noktadan yaklasilir.
    ///
    /// RoomPlan katin NEREDE oldugunu soyluyor; burasi ICINDE NASIL
    /// DOLASILDIGINI. Ayri, cunku kat plani test ediliyor ve dokunma
    /// hedefi olcumunun kaynagi - yuruyus icin oraya yeni sayilar
    /// eklemek, olculmus bir dosyayi olculmemis kararlarla karistirir.
    ///
    /// YOL BULMA YOK, KORIDOR VAR. Bir A* uygulamak bu sahne icin
    /// gereksiz: kat plani sabit, odalar dikdortgen ve herkes ayni iki
    /// sey arasinda gidip geliyor. Iki parcali bir yol - once on
    /// koridora cik, sonra hedefin hizasindan yukari - hem her masaya
    /// ulasiyor hem de bakildiginda "kapidan girip masasina gitti" diye
    /// okunuyor. Carpisma da yok: figurler birbirinin icinden gecebilir
    /// ve bu, tikanip donup kalmalarindan iyidir.
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// On koridorun z'si. Arsanin on kenari z=0; koridor onun hemen
        /// ustunde ve butun odalarin onunden geciyor.
        ///
        /// 0,55 OLCULEREK secildi: Giris'in saksilari ve Bulasik'in
        /// tezgahi bu seride duruyordu ve ikisi de arkaya alindi
        /// (RestaurantView.BuildRoomProps). Salon1'in on sira masalari
        /// z=1,35'te, sandalye yariyapiyla z=0,9'a kadar iniyor - yani
        /// 0,55 onlarin da onunden geciyor.
        /// </summary>
        public const float LaneZ = 0.55f;

        /// <summary>Kapinin x'i: Giris odasinin ortasi.</summary>
        public static float DoorX
        {
            get
            {
                RoomPlan.Room g = Room("Giris");
                return g.CenterX;
            }
        }

        /// <summary>
        /// Kapinin DISI. Musteri buradan beliriyor ve buraya gidiyor.
        ///
        /// -0,85: kamera cercevesi acik odalarin kutusu ve arsanin on
        /// kenari z=0. Daha uzagi, figuru tamamen cerceve disinda
        /// belirtiyor - "kapidan girdi" degil "alt kenardan bitti" diye
        /// okunuyordu. Bu mesafede govdenin ust yarisi gorunuyor ve
        /// iceri dogru yuruyusu bastan izleniyor.
        /// </summary>
        public static Vector3 Outside
        {
            get { return new Vector3(DoorX, 0f, -0.55f); }
        }

        /// <summary>
        /// Kaldirimin ORTASI.
        ///
        /// -0,57 bir duzeltme: eskiden -1,05 idi ve kaldirim levhasi
        /// z = -0,62 ile -0,02 arasindaydi - yani yayalar ve gelen
        /// musteriler ASFALTTA yuruyordu, bordurun 0,35 m otesinde.
        /// Kaldirim simdi -1,12 ile -0,02 arasi ve bu sayi onun ortasi.
        ///
        /// Kaldirim IKI yaya seridine sigacak kadar genisledi ve bedeli
        /// olculdu: cerceveye giren sokak derinligi 1,10 -> 1,94 m
        /// (CameraFit.StreetInFrame) ve dokunma hedefi olcumu
        /// (Editor/RoomLayout TABAN satiri) yeniden kosturuldu - 59 dp,
        /// Google'in 48 dp asgarisinin ustunde.
        /// </summary>
        public const float PavementZ = -0.72f;

        /// <summary>
        /// Iki yaya seridi arasi mesafenin YARISI.
        ///
        /// 0,35 - yani seritler 0,70 m arayla. Neden serit var: karsi
        /// yonde yuruyen iki figur ayni cizgide olunca birbirinin
        /// icinden geciyordu. Ayri seritler karsilasmayi YAPISAL olarak
        /// cozuyor; ayni seritte birbirine yetisenler icin ayrica
        /// itisme var (StreetLife.LateUpdate).
        ///
        /// 0,70 OLCULEREK secildi: Editor/PlacementAudit figurun
        /// yukseklige gore yatay profilini basiyor ve kollar disinda en
        /// genis bant BAS hizasi - 0,67 m (bu paketin oranlarinda kafa
        /// govdenin ucte biri, yani omuzlardan genis). Omuz olcusu
        /// (0,58) kullanilsa iki basin 7 cm ortusmesi kalirdi.
        ///
        /// KOLLAR BU SAYIYA GIRMIYOR ve bu bilincli: kollar govdeden
        /// acik duruyor ve el hizasinda (y 0,20-0,31) genislik 1,08 m'ye
        /// cikiyor - bir metre boyunda bir figur icin. 1,08 m arayla iki
        /// serit, kaldirimi restoranin yarisi kadar derin yapardi. Iki
        /// govdenin ayri gecmesi yetiyor; bir elin otekinin elinin
        /// icinden gecmesi bu kamera mesafesinde birkac piksel.
        /// </summary>
        public const float LaneHalf = 0.35f;

        /// <summary>
        /// Yonune gore yaya seridi. dir > 0 saga giden, dir < 0 sola.
        ///
        /// Saga giden BORDUR tarafinda, sola giden bina tarafinda -
        /// gercek bir kaldirimda da trafik boyle ayrisir ve hangi
        /// seridin hangisi oldugu degil, AYRI olmasi onemli.
        /// </summary>
        public static float PavementLane(int dir)
        {
            return PavementZ + (dir > 0 ? -LaneHalf : LaneHalf);
        }

        /// <summary>
        /// Sokakta MUSTERININ BELIRDIGI nokta.
        ///
        /// Musteri artik kapinin onunde belirmiyor: sokakta, kapidan
        /// birkac metre uzakta beliriyor ve kaldirimda yuruyerek
        /// geliyor. Kapinin onunde belirmek "geldi" degil "belirdi"
        /// diye okunuyordu - kapidan girme animasyonunun butun anlami
        /// gelisi izleyebilmek.
        ///
        /// Yon SIRAYLA degisiyor: herkes ayni taraftan gelirse sokak
        /// tek yonlu bir bant gibi duruyor.
        /// </summary>
        public static Vector3 Street(int index)
        {
            float yon = (index % 2 == 0) ? -1f : 1f;
            float uzaklik = 2.6f + (index % 3) * 1.3f;
            // Kapinin SOLUNDA beliren saga dogru yuruyecek: seridi de
            // ona gore. Yoksa gelen musteri karsi seritte yurur ve
            // butun yayalarla yuz yuze gelir.
            int dir = yon < 0f ? 1 : -1;
            return new Vector3(DoorX + yon * uzaklik, 0f, PavementLane(dir));
        }

        /// <summary>Kapinin ICI: esigin hemen arkasi.</summary>
        public static Vector3 Inside
        {
            get { return new Vector3(DoorX, 0f, LaneZ); }
        }

        // =====================================================================
        /// <summary>
        /// Kapidan bir noktaya yol. Once esik, sonra koridorda yana,
        /// sonra hedefin hizasindan yukari.
        /// </summary>
        public static void FromDoor(List<Vector3> into, Vector3 target)
        {
            into.Clear();
            into.Add(Outside);
            into.Add(Inside);
            Lane(into, Inside, target);
            into.Add(target);
        }

        /// <summary>
        /// SOKAKTAN masaya. Kaldirimda yuru, kapinin onune gel, gir.
        /// </summary>
        public static void FromStreet(List<Vector3> into, Vector3 target)
        {
            into.Clear();
            into.Add(new Vector3(DoorX, 0f, PavementZ));
            into.Add(Outside);
            into.Add(Inside);
            Lane(into, Inside, target);
            into.Add(target);
        }

        /// <summary>Icerden sokaga: kapidan cik, kaldirimda uzaklas.</summary>
        public static void ToStreet(List<Vector3> into, Vector3 from, int index)
        {
            into.Clear();
            Lane(into, from, Inside);
            into.Add(Inside);
            into.Add(Outside);
            into.Add(new Vector3(DoorX, 0f, PavementZ));
            into.Add(Street(index));
        }

        /// <summary>Bir noktadan kapiya, disari cikana kadar.</summary>
        public static void ToDoor(List<Vector3> into, Vector3 from)
        {
            into.Clear();
            Lane(into, from, Inside);
            into.Add(Inside);
            into.Add(Outside);
        }

        /// <summary>Iki nokta arasinda koridoru kullanan yol.</summary>
        public static void Between(List<Vector3> into, Vector3 from, Vector3 target)
        {
            into.Clear();
            Lane(into, from, target);
            into.Add(target);
        }

        /// <summary>
        /// Iki noktayi koridor uzerinden birlestiren ARA noktalar.
        ///
        /// Ayni hizadalarsa (x farki kucukse) koridora inmeye gerek yok:
        /// duz yukari/asagi gitmek hem kisa hem dogal. Yoksa once
        /// koridora in, yana kay, sonra hedefin hizasina cik.
        /// </summary>
        private static void Lane(List<Vector3> into, Vector3 from, Vector3 target)
        {
            // ARKA ODAYA KAPIDAN GIRILIYOR.
            //
            // Onceden hedefin hizasindan duz yukari cikiliyordu ve
            // duvarlar eklenince bu, figurun duvarin icinden gecmesi
            // demek oldu. Arka odanin kapisi RestaurantView'in actigi
            // boslukla AYNI yerden hesaplaniyor (BackDoorX) - iki yere
            // yazilsaydi kapi bir yerde, gecis baska yerde olurdu.
            int hedefOda = RoomAt(target);
            int kaynakOda = RoomAt(from);
            if (hedefOda == kaynakOda) return;

            Vector3 cikis;
            if (BackDoor(kaynakOda, out cikis))
            {
                into.Add(new Vector3(cikis.x, 0f, cikis.z + 0.5f));
                into.Add(new Vector3(cikis.x, 0f, cikis.z - 0.5f));
                from = new Vector3(cikis.x, 0f, cikis.z - 0.5f);
            }

            Vector3 giris;
            if (BackDoor(hedefOda, out giris))
            {
                if (Mathf.Abs(from.x - giris.x) > 0.35f)
                {
                    into.Add(new Vector3(from.x, 0f, LaneZ));
                    into.Add(new Vector3(giris.x, 0f, LaneZ));
                }
                into.Add(new Vector3(giris.x, 0f, giris.z - 0.5f));
                into.Add(new Vector3(giris.x, 0f, giris.z + 0.5f));
                return;
            }

            if (Mathf.Abs(from.x - target.x) < 0.35f) return;

            into.Add(new Vector3(from.x, 0f, LaneZ));
            into.Add(new Vector3(target.x, 0f, LaneZ));
        }

        /// <summary>Bu noktanin odasi. -1: arsanin disi.</summary>
        public static int RoomAt(Vector3 p)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (p.x >= r.X0 - 0.01f && p.x <= r.X0 + r.W + 0.01f
                    && p.z >= r.Z0 - 0.01f && p.z <= r.Z0 + r.D + 0.01f) return i;
            }
            return -1;
        }

        /// <summary>
        /// ARKA ODANIN KAPISI. On sirada olan odalarda kapi yok
        /// (koridor zaten iceriden geciyor), false donuyor.
        ///
        /// Kat plani iki sirali: her sutunda bir on bir arka oda. Arka
        /// odaya giris, o odanin on kenarinin ORTASINDAN. Duvar bosugu
        /// da tam oraya aciliyor (RestaurantView.Line).
        /// </summary>
        public static bool BackDoor(int room, out Vector3 kapi)
        {
            kapi = Vector3.zero;
            if (room < 0 || room >= RoomPlan.Rooms.Length) return false;

            RoomPlan.Room r = RoomPlan.Rooms[room];
            if (r.Z0 < 0.01f) return false;             // on sira

            kapi = new Vector3(r.CenterX, 0f, r.Z0);
            return true;
        }

        // =====================================================================
        /// <summary>
        /// Masaya YAKLASMA noktasi: masanin on tarafinda, oturaklarin
        /// disinda. Garson buraya geliyor; masanin uzerine basmiyor.
        /// </summary>
        public static Vector3 BesideTable(Vector3 table)
        {
            return new Vector3(table.x, 0f, table.z - 0.95f);
        }

        /// <summary>Verilen noktaya donuk olmak icin gereken aci.</summary>
        public static float FaceFrom(Vector3 at, Vector3 target)
        {
            Vector3 d = target - at;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return 0f;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // =====================================================================
        /// <summary>
        /// Mutfaktaki CALISMA NOKTALARI: ocak sirasinin onu.
        ///
        /// Istasyon indisi dogrudan bir mobilyaya baglanmiyor - mutfakta
        /// alti istasyon var, uc ocak. Istasyon, noktalar uzerinde
        /// dolasiyor: asci her is icin ayni yere degil, isin
        /// istasyonuna gore farkli bir tezgaha gidiyor ve salonda
        /// bakildiginda mutfakta HAREKET oluyor - anlatilan sey bu.
        /// </summary>
        public static Vector3 KitchenPost(int station, int count)
        {
            RoomPlan.Room m = Room("Mutfak");
            if (count < 1) count = 1;
            int i = station < 0 ? 0 : station % count;
            float t = (i + 0.5f) / count;
            return new Vector3(m.X0 + 0.75f + (m.W - 2.0f) * t, 0f,
                               m.Z0 + m.D - 1.35f);
        }

        /// <summary>
        /// HAZIRLIK TEZGAHININ ONU: ascinin yikayip dogradigi yer.
        ///
        /// Tezgahlar mutfagin ON sirasinda (RestaurantView.BuildRoomProps,
        /// LineUp back:false -> z = Z0 + 0,55). Asci onlarin ONUNDE,
        /// yani iceride duruyor ve tezgaha donuyor.
        /// </summary>
        public static Vector3 PrepPost(int index, int count)
        {
            RoomPlan.Room m = Room("Mutfak");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            return new Vector3(m.X0 + 0.75f + (m.W - 2.0f) * t, 0f, m.Z0 + 1.25f);
        }

        /// <summary>Hazirlik tezgahinin kendisi: ascinin bakacagi yon.</summary>
        public static Vector3 PrepCounter(int index, int count)
        {
            Vector3 p = PrepPost(index, count);
            return new Vector3(p.x, 0f, p.z - 0.7f);
        }

        /// <summary>Buzdolabinin onu: ascinin malzeme aldigi yer.</summary>
        public static Vector3 Fridge
        {
            get
            {
                RoomPlan.Room m = Room("Mutfak");
                return new Vector3(m.X0 + m.W - 1.35f, 0f, m.Z0 + m.D - 0.85f);
            }
        }

        /// <summary>
        /// Buzdolabinin KENDISI: ascinin bakacagi nokta.
        ///
        /// Dolap sag duvarda (RestaurantView.BuildRoomProps,
        /// X0 + W - 0,55) ve odaya bakiyor. Asci onun onunde durup
        /// SAGA donmeli; once +Z yazilmisti ve asci dolaba yan
        /// dururken "alma" oynatiyordu.
        /// </summary>
        public static Vector3 FridgeFace
        {
            get
            {
                RoomPlan.Room m = Room("Mutfak");
                return new Vector3(m.X0 + m.W - 0.55f, 0f, m.Z0 + m.D - 0.7f);
            }
        }

        /// <summary>Ascinin bosta bekledigi yer: tezgahin arkasi.</summary>
        public static Vector3 CookHome(int index, int count)
        {
            RoomPlan.Room m = Room("Mutfak");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            return new Vector3(m.X0 + 0.9f + (m.W - 2.2f) * t, 0f, m.CenterZ - 0.3f);
        }

        /// <summary>Garsonun bosta bekledigi yer: giristeki kasanin yani.</summary>
        public static Vector3 SalonHome(int index, int count)
        {
            RoomPlan.Room g = Room("Giris");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            // -2,05: kasa tezgahi z = Z0 + D - 0,8'de ve 0,6 derin,
            // yani 2,9'dan basliyor. Figurun derinligi 1,12 m; 2,4'te
            // dururken yerlesim denetimi 0,14 m cakisma olctu - garson
            // tezgahin icinde bekliyordu.
            return new Vector3(g.X0 + 0.8f + (g.W - 1.6f) * t, 0f, g.Z0 + g.D - 2.05f);
        }

        /// <summary>Masa bekleyen grubun durdugu yer: kapinin yani.</summary>
        public static Vector3 QueueSpot(int index)
        {
            return new Vector3(DoorX + 0.85f + (index % 4) * 0.55f, 0f, LaneZ + 0.15f);
        }

        // =====================================================================
        private static RoomPlan.Room Room(string name)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
                if (RoomPlan.Rooms[i].Name == name) return RoomPlan.Rooms[i];
            return RoomPlan.Rooms[0];
        }
    }
}
