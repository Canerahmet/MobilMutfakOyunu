using Lokanta.Core.Content;
using Lokanta.Core.Sim;

namespace Lokanta.Game
{
    /// <summary>The tone of a notice. Its colour and its sound come from here.</summary>
    public enum NoticeTone
    {
        Info,
        Good,
        Warn,
        Bad,
    }

    /// <summary>
    /// Turns simulation events into A SENTENCE THE PLAYER CAN READ.
    ///
    /// Without this class the game was deaf. The core produces thirty-three
    /// kinds of event (docs/23 6.3) and the view layer read only seven of
    /// them, to play a SOUND: the patience warning, running out of stock, a
    /// resignation, late wages, a tab gone bad, an offended guest - all of
    /// them worked out, and all of them thrown away. The player could not
    /// find out why their angry guest was angry.
    ///
    /// ONLY the text is produced here. Event -> sentence; no decisions, no
    /// state. Events that repeat often (being seated, ordering, paying) are
    /// deliberately left out: a notice strip that has forty lines a minute
    /// flowing through it does not get read.
    /// </summary>
    public static class Notices
    {
        /// <summary>
        /// The notice text for the event. False for events that have no notice.
        /// </summary>
        public static bool Describe(in SimEvent e, ContentSet content, Simulation sim,
                                    out string text, out NoticeTone tone)
        {
            text = null;
            tone = NoticeTone.Info;

            switch (e.Kind)
            {
                // --- hall ---------------------------------------------------
                case SimEventKind.CustomerLeftAngry:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.angry", StageReason(e.B));
                    return true;

                case SimEventKind.PlatesOut:
                    // THE CAUSE AND THE CURE in the same sentence: "why it stopped"
                    // and "what should I do". If there is a dishwasher the cure has
                    // to be a different one - telling that player to "hire a
                    // dishwasher" would send them looking for a button that is not
                    // there.
                    tone = NoticeTone.Warn;
                    text = e.B > 0
                        ? Loc.T("notice.plates_out_busy", e.A)
                        : Loc.T("notice.plates_out", e.A);
                    return true;

                // THE DISH NAME IS NOT PRINTED: the event is not a dish running
                // out, it is the guest finding NO main dish at all on the menu
                // that can be made. It used to take e.A for a dish and print its
                // name; since e.A is the party index, an unrelated dish came out.
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

                // --- regulars ------------------------------------------------
                case SimEventKind.RegularVisited:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.regular", Regular(content, e.A));
                    return true;

                case SimEventKind.RegularUpset:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.regular_upset", Regular(content, e.A), e.B);
                    return true;

                // --- crew ----------------------------------------------------
                case SimEventKind.StaffResigned:
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.resigned", Who(sim, e.A, e.B));
                    return true;

                case SimEventKind.StaffFired:
                    // The name index travels in the event, because by the
                    // time the notice is built the roster has already closed
                    // over the gap and (pool, index) names the wrong person.
                    tone = NoticeTone.Bad;
                    text = Loc.T("notice.fired", Loc.StaffName(e.A), e.B);
                    return true;

                case SimEventKind.StaffRaised:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.raise", Loc.StaffName(e.A), e.B);
                    return true;

                case SimEventKind.StaffDayOff:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.day_off", Loc.StaffName(e.A));
                    return true;

                case SimEventKind.CriticExpected:
                    tone = NoticeTone.Info;
                    text = Loc.T("notice.critic_expected");
                    return true;

                case SimEventKind.CriticVerdict:
                    tone = e.A >= 70 ? NoticeTone.Good : NoticeTone.Bad;
                    text = Loc.T(e.A >= 70 ? "notice.critic_good" : "notice.critic_bad", e.A);
                    return true;

                case SimEventKind.SignatureOpened:
                    tone = NoticeTone.Info;
                    text = Loc.T(e.A == (int)SignatureKind.Credit
                                 ? "notice.signature_open_tab"
                                 : "notice.signature_open_combo");
                    return true;

                case SimEventKind.SeasonChanged:
                    // Literal keys, because the string check reads the code
                    // for the keys it asks for and cannot see a key built at
                    // run time.
                    tone = NoticeTone.Info;
                    text = Loc.T("notice.season", Loc.T(
                        e.A == 1 ? "ui.season.1" : e.A == 2 ? "ui.season.2" : e.A == 3 ? "ui.season.3" : "ui.season.0"));
                    return true;

                case SimEventKind.StaffLeveledUp:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.level_up", Who(sim, e.A, 0), e.B);
                    return true;

                case SimEventKind.StaffTenure:
                    // THE LINE DEPENDS ON THE CUISINE.
                    //
                    // In a tradesman's restaurant the relationship is with the CHEF
                    // and with the work; in a chain it is with the SHIFT and with
                    // the system (docs/53). A single line would have made both of
                    // them generic.
                    //
                    // What tells them apart is the self-service flag - reading the
                    // cuisine's name here would mean writing "which cuisine is
                    // which" into a third place.
                    //
                    // e.A is the pool, e.B the INDEX. The number of days is not in
                    // the event: it is a constant (Simulation.TenureDays) and
                    // writing it in two places has drifted apart silently five times
                    // in this project.
                    tone = NoticeTone.Good;
                    text = Loc.T(
                        content != null && content.SelfService
                            ? "notice.tenure_zincir" : "notice.tenure_lokanta",
                        Who(sim, e.A, e.B), Simulation.TenureDays);
                    return true;

                // --- money ---------------------------------------------------
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

                // --- the book ------------------------------------------------
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

                // --- investment ----------------------------------------------
                case SimEventKind.EquipmentBought:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.equipment", Station(content, e.A), e.B);
                    return true;

                case SimEventKind.StorageBought:
                    tone = NoticeTone.Good;
                    text = Loc.T("notice.storage", e.A);
                    return true;

                // --- the ladder down -----------------------------------------
                //
                // These are the things THE PLAYER DOES NOT WANT, and that is
                // exactly why they have to be visible: if a shop whose equipment
                // is being sold off quietly stays small, the player never learns
                // why.
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

                // --- a rejected command --------------------------------------
                //
                // Showing this is essential: pressing a button and nothing
                // happening makes the player think the game is broken.
                case SimEventKind.CommandRejected:
                    tone = NoticeTone.Warn;
                    // THE REASON IS GIVEN TOO. A single generic sentence made
                    // situations that are nothing like each other look the same:
                    // the player who cannot afford it and the player who has used
                    // up the day's command allowance read the same line, and
                    // neither of them knew what to do.
                    //
                    // The B field is the reason for the refusal (the second number
                    // of Simulation.Emit): 9 not enough money, 12 the day's command
                    // allowance.
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
        /// At which stage the guest gave up. "They got angry" is not
        /// information on its own; this is what tells the player what to
        /// change the NEXT day.
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
        /// The person's NAME, or their role if there is none.
        ///
        /// In a resignation event the person IS NO LONGER ON THE CREW, so
        /// asking for their name comes back empty. That is why the role
        /// stands as the fallback: "the cook has left" is not uninformative,
        /// only cold.
        /// </summary>
        private static string Who(Simulation sim, int pool, int index)
        {
            if (sim != null)
            {
                string name = Loc.StaffName(sim.StaffNameIndex(pool, index));
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
