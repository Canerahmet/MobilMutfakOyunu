using Lokanta.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// NISANLAR - kazanilmis olanlar ve henuz kazanilmamislar.
    ///
    /// Bu bir GOREV LISTESI DEGIL, ve ayrim oyunun tek cumlelik vaadine
    /// dayaniyor: "Patronsun, asci degil." Bir gorev listesi oyuncuya
    /// yarin ne yapacagini soyler ve onu gorunmez bir patronun calisani
    /// yapar. Buradaki hicbir satir "bugun sunu yap" demiyor, hicbirinin
    /// suresi yok ve hicbiri kacirilabilir degil.
    ///
    /// Peki neden kazanilmamislar da gorunuyor: oyuncunun KENDI hedefini
    /// secebilmesi icin neyin mumkun oldugunu bilmesi gerekiyor. Gizli
    /// bir nisan, kazanildiginda surpriz olur ama oyun boyunca hicbir sey
    /// yapmaz.
    ///
    /// Odul PARA DEGIL. Bu projenin yasasi: doymus bir eksene odenen odul
    /// gorunmez, ve harness'a gore iyi oyuncu altmisinci gunu ~21.000
    /// kasayla bitiriyor. Nisanin odulu gorulmek.
    /// </summary>
    public sealed class BadgeScreen : ListScreen
    {
        protected override string Title { get { return Loc.T("ui.badge.title"); } }

        protected override string Subtitle
        {
            get
            {
                return Loc.T("ui.badge.progress",
                             App.Sim.BadgesEarned, App.Sim.BadgeCount);
            }
        }

        protected override void Fill(VisualElement list)
        {
            Simulation sim = App.Sim;

            for (int i = 0; i < sim.BadgeCount; i++)
            {
                bool kazanildi = sim.HasBadge(i);

                VisualElement card = Theme.PanelBox();

                VisualElement head = Theme.Row(Theme.Gap);
                head.style.justifyContent = Justify.SpaceBetween;
                head.style.alignItems = Align.Center;

                // KAZANILMAMIS NISANIN ADI DA GORUNUYOR, yalnizca soluk.
                //
                // Adi gizleyip "???" yazmak, oyuncunun hedef
                // secebilmesini engellerdi - ve bu ekranin tek isi o.
                Label ad = Theme.Text(Loc.T(Badges.NameKey(i)), Theme.FontBody,
                                      kazanildi ? Theme.Accent : Theme.InkFaint);
                if (kazanildi) ad.style.unityFontStyleAndWeight = FontStyle.Bold;
                ad.style.whiteSpace = WhiteSpace.Normal;
                ad.style.flexShrink = 1;
                head.Add(ad);

                head.Add(Theme.Text(
                    Loc.T(kazanildi ? "ui.badge.have" : "ui.badge.open"),
                    Theme.FontSmall, kazanildi ? Theme.Good : Theme.InkFaint));
                card.Add(head);

                Label not = Theme.Text(Loc.T(Badges.NoteKey(i)), Theme.FontSmall,
                                       kazanildi ? Theme.InkDim : Theme.InkFaint);
                not.style.whiteSpace = WhiteSpace.Normal;
                card.Add(not);

                list.Add(card);
            }
        }
    }
}
