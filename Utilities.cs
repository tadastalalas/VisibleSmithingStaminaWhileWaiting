using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VisibleSmithingStaminaWhileWaiting
{
    internal static class Utilities
    {
        public static void FillHeroesInParty(Hero hero, List<Hero> buffer)
        {
            buffer.Clear();
            MBList<TroopRosterElement>? roster = hero.PartyBelongedTo?.MemberRoster?.GetTroopRoster();
            if (roster == null) return;
            for (int i = 0; i < roster.Count; i++)
            {
                Hero? member = roster[i].Character?.HeroObject;
                if (member != null && !buffer.Contains(member)) buffer.Add(member);
            }
        }
    }
}