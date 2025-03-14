﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VisibleSmithingStaminaWhileWaiting
{
    internal class Utilities
    {
        public static  List<Hero> ListOfHeroesInParty(Hero hero)
        {
            List<Hero> listHeroes = new List<Hero>();
            MBList<TroopRosterElement> listTroops = hero.PartyBelongedTo.MemberRoster.GetTroopRoster();

            if (hero.PartyBelongedTo != null)
            {
                foreach (var member in listTroops)
                {
                    if (member.Character.IsHero)
                    {
                        var partyHero = member.Character.HeroObject;
                        if (partyHero != null && !listHeroes.Contains(partyHero))
                        {
                            listHeroes.Add(partyHero);
                        }
                    }
                }
            }
            return listHeroes;
        }
    }
}