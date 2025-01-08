using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace VisibleSmithingStaminaWhileWaiting
{
    public partial class SubModule
    {
        public class VSSWhileWaiting : CampaignBehaviorBase
        {
            public override void RegisterEvents() => CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);

            public override void SyncData(IDataStore dataStore) { }

            private readonly string _messageAllPartysStaminaRecovered = "{=Zqsbdz6MIc9Ex}Party's smithing stamina is replenished";
            private bool _isNotificationReady = false;
            private bool _isTimeStopReady = false;

            private readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

            private void OnHourlyTick()
            {
                if (!IsHeroAbleToRegenerateStaminaAtAll())
                    return;

                bool isAnybodyInPartyHasUsedStamina = IsAnybodyInPartyHasUsedStamina();

                CheckAndPrepareNotification(true, isAnybodyInPartyHasUsedStamina);
                PrepareTimeStopOnConditions(true, isAnybodyInPartyHasUsedStamina);

                if (IsHeroInTown())
                {
                    if (settings.UseSmithingSkillForStaminaRegen && isAnybodyInPartyHasUsedStamina)
                    {
                        RegenerateStaminaForAllParty();
                        isAnybodyInPartyHasUsedStamina = IsAnybodyInPartyHasUsedStamina();
                    }

                    if (settings.ShowCurrentPartysStaminaPercentWhileInTown && isAnybodyInPartyHasUsedStamina)
                        LogCurrentPartysSmithingStaminaPercent();

                    if (!isAnybodyInPartyHasUsedStamina)
                    {
                        if (ShouldDisplayNotifications())
                            DisplayStaminaNotification(isAnybodyInPartyHasUsedStamina);

                        if (settings.StopWaitingWhenStaminaIsFull && _isTimeStopReady)
                            StopWaitingWhenStaminaIsFull(isAnybodyInPartyHasUsedStamina);
                        return;
                    }
                }
                else
                {
                    if (settings.RegenStaminaWhileTravelling && isAnybodyInPartyHasUsedStamina)
                    {
                        if (settings.ShowCurrentStaminaPercentWhileTravelling)
                            LogCurrentPartysSmithingStaminaPercent();

                        RegenerateStaminaForAllParty();
                        isAnybodyInPartyHasUsedStamina = IsAnybodyInPartyHasUsedStamina();

                        if (ShouldDisplayNotifications() && settings.ShowNotificationsWhileTravelling && !isAnybodyInPartyHasUsedStamina)
                            DisplayStaminaNotification(isAnybodyInPartyHasUsedStamina);
                        return;
                    }
                }
            }

            private static bool IsHeroAbleToRegenerateStaminaAtAll()
            {
                var hero = Hero.MainHero;
                return hero != null && !hero.IsDead && !hero.IsDisabled && !hero.IsFugitive && !hero.IsPrisoner && !hero.IsReleased;
            }

            private void CheckAndPrepareNotification(bool flag, bool isAnybodyInPartyHasUsedStamina)
            {
                if (flag)
                {
                    if (!_isNotificationReady && isAnybodyInPartyHasUsedStamina)
                        _isNotificationReady = true;
                }
                else
                {
                    if (_isNotificationReady && !isAnybodyInPartyHasUsedStamina)
                        _isNotificationReady = false;
                }
            }

            private void PrepareTimeStopOnConditions(bool flag, bool isAnybodyInPartyHasUsedStamina)
            {
                if (flag)
                {
                    if (!_isTimeStopReady && IsHeroInTown() && isAnybodyInPartyHasUsedStamina)
                        _isTimeStopReady = true;
                }
                else
                {
                    if (_isTimeStopReady && !isAnybodyInPartyHasUsedStamina)
                        _isTimeStopReady = false;
                }
            }

            private static bool IsHeroInTown()
            {
                var settlement = Settlement.CurrentSettlement;
                if (settlement?.IsTown == true)
                {
                    var town = settlement.Town;
                    if (town != null && !town.IsCastle && !town.InRebelliousState && !town.IsUnderSiege)
                        return true;
                }
                return false;
            }

            private static bool IsHeroHasUsedStamina()
            {
                var craftingBehavior = Campaign.Current?.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (craftingBehavior == null)
                    return false;
                var maxMainHeroStamina = craftingBehavior.GetMaxHeroCraftingStamina(Hero.MainHero);
                var currentMainHeroStamina = craftingBehavior.GetHeroCraftingStamina(Hero.MainHero);
                return currentMainHeroStamina < maxMainHeroStamina;
            }

            private static bool IsAnyCompanionInPartyHasUsedStamina()
            {
                var hero = Hero.MainHero;
                if (hero == null)
                    return false;
                IEnumerable<Hero> companions = hero.CompanionsInParty;
                CraftingCampaignBehavior craftingBehavior = Campaign.Current?.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (craftingBehavior == null)
                    return false;

                foreach (Hero companion in companions)
                {
                    int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(companion);
                    int currentStamina = craftingBehavior.GetHeroCraftingStamina(companion);
                    if (currentStamina < maxStamina)
                        return true;
                }
                return false;
            }

            private static bool IsAnybodyInPartyHasUsedStamina()
            {
                if (IsHeroHasUsedStamina() || IsAnyCompanionInPartyHasUsedStamina())
                    return true;
                return false;
            }

            private void RegenerateStaminaForAllParty()
            {
                var hero = Hero.MainHero;
                if (hero == null)
                    return;
                IEnumerable<Hero> companions = hero.CompanionsInParty;
                AddStamina(hero);
                foreach (Hero companion in companions)
                    AddStamina(companion);
            }

            private void AddStamina(Hero hero)
            {
                if (hero == null)
                    return;
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (craftingBehavior == null)
                    return;
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int smithingSkillLevel = hero.GetSkillValue(DefaultSkills.Crafting);
                int regenStamina = CalculateHowMuchStaminaToRegen(hero, maxStamina, currentStamina, smithingSkillLevel);

                if (currentStamina < maxStamina)
                {
                    craftingBehavior.SetHeroCraftingStamina(hero, currentStamina + regenStamina);

                    if (craftingBehavior.GetHeroCraftingStamina(hero) > maxStamina)
                        craftingBehavior.SetHeroCraftingStamina(hero, maxStamina);
                }
            }

            private int CalculateHowMuchStaminaToRegen(Hero hero, int maxStamina, int currentStamina, int smithingSkillLevel)
            {
                int regenStamina = TaleWorlds.Library.MathF.Round((float)maxStamina / settings.HoursToFullStaminaRegen);
                bool flag = settings.UseSmithingSkillForStaminaRegen;
                if (flag && IsHeroInTown())
                {
                    int regenAmountFromSkill = CalculateStaminaRegenBasedOnSkill(smithingSkillLevel);
                    int hourlyRecoveryRate = GetStaminaHourlyRecoveryRate(hero);
                    if (hourlyRecoveryRate >= regenAmountFromSkill)
                        return 0;
                    else
                        return regenAmountFromSkill - hourlyRecoveryRate;
                }
                if (flag && !IsHeroInTown())
                    return CalculateStaminaRegenBasedOnSkill(smithingSkillLevel);
                return regenStamina;
            }

            private int CalculateStaminaRegenBasedOnSkill(int smithingSkillLevel) => TaleWorlds.Library.MathF.Round((float)smithingSkillLevel / settings.StaminaImmersiveRegenDivisor);

            private int GetStaminaHourlyRecoveryRate(Hero hero)
            {
                if (hero == null)
                    return 0;
                int num = 5 + TaleWorlds.Library.MathF.Round((float)hero.GetSkillValue(DefaultSkills.Crafting) * 0.025f);
                if (hero.GetPerkValue(DefaultPerks.Athletics.Stamina))
                    num += TaleWorlds.Library.MathF.Round((float)num * DefaultPerks.Athletics.Stamina.PrimaryBonus);
                return num;
            }

            private void LogCurrentPartysSmithingStaminaPercent()
            {
                Hero heroWithLongestRecoveryTime = Hero.MainHero;
                float longestRecoveryTime = CalculateRecoveryTime(Hero.MainHero);

                if (IsAnyCompanionInPartyHasUsedStamina())
                {
                    IEnumerable<Hero> companions = Hero.MainHero.CompanionsInParty;
                    foreach (Hero companion in companions)
                    {
                        float recoveryTime = CalculateRecoveryTime(companion);
                        if (recoveryTime > longestRecoveryTime)
                        {
                            longestRecoveryTime = recoveryTime;
                            heroWithLongestRecoveryTime = companion;
                        }
                    }
                }
                var percent = CalculateCurrentHeroSmithingStaminaPercent(heroWithLongestRecoveryTime);
                MBTextManager.SetTextVariable("percent", percent);
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=zdTrD1TxVpbQY}Party's stamina is {percent}%").ToString()));
            }

            private float CalculateRecoveryTime(Hero hero)
            {
                if (hero == null)
                    return 0;
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (craftingBehavior == null)
                    return 0;
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int staminaToRecover = maxStamina - currentStamina;
                int smithingSkillLevel = hero.GetSkillValue(DefaultSkills.Crafting);
                float recoveryRate = CalculateStaminaRegenBasedOnSkill(smithingSkillLevel);
                return (float)staminaToRecover / recoveryRate;
            }

            private static int CalculateCurrentHeroSmithingStaminaPercent(Hero hero)
            {
                if (hero == null)
                    return 0;
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (craftingBehavior == null)
                    return 0;
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int percent = (currentStamina * 100) / maxStamina;
                return percent;
            }

            private bool ShouldDisplayNotifications() => settings.ShowMessageInTheLog || settings.ShowMessageOnTheScreen || settings.ShowMessageAsPopUp;

            private void DisplayStaminaNotification(bool isAnybodyInPartyHasUsedStamina)
            {
                if (_isNotificationReady)
                {
                    CheckAndPrepareNotification(false, isAnybodyInPartyHasUsedStamina);
                    DisplayNotification(_messageAllPartysStaminaRecovered);
                }
            }

            private void DisplayNotification(string message)
            {
                if (settings.ShowMessageInTheLog)
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject(message).ToString()));
                if (settings.ShowMessageOnTheScreen)
                    MBInformationManager.AddQuickInformation(new TextObject(message), 2000, null, "event:/ui/notification/quest_start");
                if (settings.ShowMessageAsPopUp)
                    Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new CustomSmithingStaminaMapNotification(new TextObject(message)));
            }

            private void StopWaitingWhenStaminaIsFull(bool isAnybodyInPartyHasUsedStamina)
            {
                PrepareTimeStopOnConditions(false, isAnybodyInPartyHasUsedStamina);
                GameMenu.SwitchToMenu("town");
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }
    }
}