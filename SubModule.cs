using HarmonyLib;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace VisibleSmithingStaminaWhileWaiting
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("visible_smithing_stamina_while_waiting");
            harmony.PatchAll();
        }

        protected override void OnSubModuleUnloaded() => base.OnSubModuleUnloaded();

        protected override void OnBeforeInitialModuleScreenSetAsRoot() => base.OnBeforeInitialModuleScreenSetAsRoot();

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (Campaign.Current is Campaign campaign && campaign.GameMode == CampaignGameMode.Campaign)
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                campaignGameStarter.AddBehavior(new VSSWhileWaiting());
            }
        }

        public override void OnGameEnd(Game game)
        {
            CampaignEvents.HourlyTickEvent.ClearListeners(this);
            base.OnGameEnd(game);
        }

        public class VSSWhileWaiting : CampaignBehaviorBase
        {
            public override void RegisterEvents() => CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);

            public override void SyncData(IDataStore dataStore) { }

            private readonly string _message = "{=Zqsbdz6MIc9Ex}Main hero stamina reached 100%";
            private bool _IsNotificationReadyToBeDisplayed = false;
            private bool _IsStopWaitingReadyToBeExecuted = false;

            private readonly Settings settings = AttributeGlobalSettings<Settings>.Instance ?? new Settings();

            private void HourlyTick()
            {
                if (!IsHeroAbleToRegenerateStaminaAtAll())
                    return;

                CheckIfNotificationMeetsConditions();
                CheckIfStopWaitingMeetsConditions();

                if ((!IsHeroInTown()) && !DoesHeroHasMaxStamina() && settings.RegenStaminaWhileTravelling)
                {
                    if (settings.ShowCurrentStaminaPercentWhileTravelling)
                        ShowCurrentHeroSmithingStaminaPercentInLog();

                    AddStaminaToHeroParty();

                    if (DoesHeroHasMaxStamina() && settings.ShowNotificationsWhileTravelling)
                        DisplayStaminaNotification();

                    return;
                }
                
                if (IsHeroInTown() && !DoesHeroHasMaxStamina() && settings.RegenerateStaminaBasedOnSmithingSkill)
                    AddStaminaToHeroParty();
                
                if (IsHeroInTown() && settings.ShowCurrentStaminaPercentInTown)
                    ShowCurrentHeroSmithingStaminaPercentInLog();

                if (IsHeroInTown() && DoesHeroHasMaxStamina() && IsAnySettlementNotificationIsAllowedToBeDisplayed())
                {
                    DisplayStaminaNotification();
                    if (settings.StopWaitingWhenStaminaIsFull)
                        StopWaitingWhenStaminaIsFull();

                    return;
                }
            }

            private void CheckIfNotificationMeetsConditions()
            {
                if (!_IsNotificationReadyToBeDisplayed && !DoesHeroHasMaxStamina())
                    _IsNotificationReadyToBeDisplayed = true;
            }

            private void CheckIfStopWaitingMeetsConditions()
            {
                if (!_IsStopWaitingReadyToBeExecuted && !DoesHeroHasMaxStamina())
                    _IsStopWaitingReadyToBeExecuted = true;
            }

            private static bool IsHeroInTown()
            {
                if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown)
                {
                    var town = Settlement.CurrentSettlement.Town;
                    if (town != null)
                    {
                        if (town.IsCastle || town.InRebelliousState || town.IsUnderSiege)
                        {
                            return false;
                        }
                        return true;
                    }
                }
                return false;
            }

            private static bool IsHeroAbleToRegenerateStaminaAtAll()
            {
                var hero = Hero.MainHero;
                return !hero.IsDead && !hero.IsDisabled && !hero.IsFugitive && !hero.IsPrisoner && !hero.IsReleased;
            }

            private static bool DoesHeroHasMaxStamina()
            {
                var craftingBehavior = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>();
                var maxMainHeroStamina = craftingBehavior.GetMaxHeroCraftingStamina(Hero.MainHero);
                var currentMainHeroStamina = craftingBehavior.GetHeroCraftingStamina(Hero.MainHero);
                return currentMainHeroStamina >= maxMainHeroStamina;
            }

            private bool IsAnySettlementNotificationIsAllowedToBeDisplayed()
            {
                if (settings.ShowMessageInTheLog || settings.ShowMessageOnTheScreen || settings.ShowMessageAsPopUp)
                    return true;
                return false;
            }

            private void DisplayStaminaNotification()
            {
                if (_IsNotificationReadyToBeDisplayed)
                {
                    if (settings.ShowMessageInTheLog)
                        InformationManager.DisplayMessage(new InformationMessage(new TextObject(_message).ToString()));

                    if (settings.ShowMessageOnTheScreen)
                        MBInformationManager.AddQuickInformation(new TextObject(_message), 2000, null, "event:/ui/notification/quest_start");

                    if (settings.ShowMessageAsPopUp)
                        Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new CustomSmithingStaminaMapNotification(new TextObject(_message)));

                    _IsNotificationReadyToBeDisplayed = false;
                }
            }

            private void StopWaitingWhenStaminaIsFull()
            {
                if (_IsStopWaitingReadyToBeExecuted)
                {
                    GameMenu.SwitchToMenu("town");
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    _IsStopWaitingReadyToBeExecuted = false;
                }
            }

            private void AddStaminaToHeroParty()
            {
                var hero = Hero.MainHero;
                IEnumerable<Hero> companions = hero.CompanionsInParty;
                AddStamina(hero);
                foreach (Hero companion in companions)
                {
                    AddStamina(companion);
                }
            }

            private void AddStamina(Hero hero)
            {
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
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
                int regenStamina = TaleWorlds.Library.MathF.Round((float)maxStamina / 72);
                bool flag = settings.RegenerateStaminaBasedOnSmithingSkill;
                if (flag && IsHeroInTown())
                {
                    int regenAmountFromSkill = TaleWorlds.Library.MathF.Round((float)smithingSkillLevel / 10);
                    int hourlyRecoveryRate = GetStaminaHourlyRecoveryRate(hero);
                    if (hourlyRecoveryRate >= regenAmountFromSkill)
                    {
                        return 0;
                    }
                    else
                    {
                        return regenAmountFromSkill - hourlyRecoveryRate;
                    }
                }
                if (flag && !IsHeroInTown())
                {
                    return TaleWorlds.Library.MathF.Round((float)smithingSkillLevel / 10);
                }
                return regenStamina;
            }

            private int GetStaminaHourlyRecoveryRate(Hero hero)
            {
                int num = 5 + TaleWorlds.Library.MathF.Round((float)hero.GetSkillValue(DefaultSkills.Crafting) * 0.025f);
                if (hero.GetPerkValue(DefaultPerks.Athletics.Stamina))
                {
                    num += TaleWorlds.Library.MathF.Round((float)num * DefaultPerks.Athletics.Stamina.PrimaryBonus);
                }
                return num;
            }

            private static void ShowCurrentHeroSmithingStaminaPercentInLog()
            {
                if (DoesHeroHasMaxStamina())
                    return;
                var percent = CalculateCurrentHeroSmithingStaminaPercent(Hero.MainHero);
                MBTextManager.SetTextVariable("percent", percent);
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=zdTrD1TxVpbQY}Main hero stamina is {percent}%").ToString()));
            }

            private static int CalculateCurrentHeroSmithingStaminaPercent(Hero hero)
            {
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int percent = (currentStamina * 100) / maxStamina;
                return percent;
            }
        }
    }

    public class CustomSmithingStaminaMapNotificationVM : MapNotificationItemBaseVM
    {
        public CustomSmithingStaminaMapNotificationVM(CustomSmithingStaminaMapNotification data) : base(data)
        {
            base.NotificationIdentifier = "custom_smithing_stamina_map_notification";

            this._onInspect = delegate ()
            {
                SoundEvent.PlaySound2D("event:/ui/notification/quest_start");
            };
        }
    }


    public class CustomSmithingStaminaMapNotification : InformationData
    {
        public CustomSmithingStaminaMapNotification(TextObject description) : base(description) { }

        public override TextObject TitleText
        {
            get { return new TextObject("Smithing stamina is 100%"); }
        }

        public override string SoundEventPath
        {
            get { return "event:/ui/notification/kingdom_decision"; }
        }
    }


    [HarmonyPatch(typeof(MapNotificationVM), "PopulateTypeDictionary")]
    internal class PopulateNotificationsPatch
    {
        private static void Postfix(MapNotificationVM __instance)
        {
            var fieldInfo = __instance.GetType().GetField("_itemConstructors", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo?.GetValue(__instance) is Dictionary<Type, Type> dic)
            {
                dic.Add(typeof(CustomSmithingStaminaMapNotification), typeof(CustomSmithingStaminaMapNotificationVM));
            }
        }
    }

    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(130572221) { }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(CustomSmithingStaminaMapNotification), 1);
        }

    }

    internal class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id
        {
            get { return "VisibleSmithingStaminaWhileWaiting"; }
        }

        public override string DisplayName
        {
            get { return new TextObject("{=EuYdW1aH89JPd}Visible Smithing Stamina While Waiting").ToString(); }
        }

        public override string FolderName
        {
            get { return "VisibleSmithingStaminaWhileWaiting"; }
        }

        public override string FormatType
        {
            get { return "json2"; }
        }

        [SettingPropertyBool("{=PDnUz0EnsVJyH}Event log notification", Order = 0, RequireRestart = false, HintText = "{=3UWpkfOEaAIzL}Show smithing stamina notification in the log in the bottom-left corner of the screen.")]
        [SettingPropertyGroup("{=OcXCbctwrryDV}Select how you want your smithing stamina notifications to be shown", GroupOrder = 0)]
        public bool ShowMessageInTheLog { get; set; } = true;

        [SettingPropertyBool("{=4gwYs6wqtoMx9}Middle screen notification", Order = 1, RequireRestart = false, HintText = "{=F4b8RhAxgtqAy}Show smithing stamina notification in the top-middle part of the screen.")]
        [SettingPropertyGroup("{=OcXCbctwrryDV}Select how you want your smithing stamina notifications to be shown", GroupOrder = 0)]
        public bool ShowMessageOnTheScreen { get; set; } = true;

        [SettingPropertyBool("{=W9KhHjSq0EHDc}Round popup notification", Order = 2, RequireRestart = false, HintText = "{=wY5QrbROdk7M5}Show smithing stamina notification as a round pop up on the right side of the screen.")]
        [SettingPropertyGroup("{=OcXCbctwrryDV}Select how you want your smithing stamina notifications to be shown", GroupOrder = 0)]
        public bool ShowMessageAsPopUp { get; set; } = true;

        [SettingPropertyBool("{=GK78CZFIe9Dti}Stop waiting when stamina is full", Order = 0, RequireRestart = false, HintText = "{=yOQyJJBraA767}Hero will stop waiting when main hero stamina reaches 100%.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool StopWaitingWhenStaminaIsFull { get; set; } = false;

        [SettingPropertyBool("{=sO6sGMmSdvZQW}Regenerate stamina while travelling", Order = 1, RequireRestart = false, HintText = "{=Z1K0iP4UAKq6c}All hero party members will regenerate smithing stamina while travelling. 3 days = 100% stamina.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool RegenStaminaWhileTravelling { get; set; } = true;

        [SettingPropertyBool("{=6qMBxn8agWxK2}Show notifications while travelling", Order = 2, RequireRestart = false, HintText = "{=zCzem6NK44Yde}Show smithing stamina recovery notifications while hero is travelling. [Regen stamina while travelling] option must be enabled for this to work.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowNotificationsWhileTravelling { get; set; } = true;

        [SettingPropertyBool("{=6oTamV4M8XdZO}[Town] Show current stamina percent", Order = 3, RequireRestart = false, HintText = "{=WtDruY7sQcxsx}Show current smithing stamina percent every hour while waiting in town.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentStaminaPercentInTown { get; set; } = false;

        [SettingPropertyBool("{=ErodgkpkVhxKk}[Travelling] Show current stamina percent", Order = 4, RequireRestart = false, HintText = "{=jLu2I19JjbY84}Show current smithing stamina percent every hour while travelling in the world.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentStaminaPercentWhileTravelling { get; set; } = false;

        [SettingPropertyBool("{=s2viSnCYOMSCW}Immersive Stamina Regen", Order = 5, RequireRestart = false, HintText = "{=01qKs9tTnkpZk}Regenerate smithing stamina based on hero's smithing skill. Formula:  Regen amount(per hour) = (Smithing Skill / 10).")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool RegenerateStaminaBasedOnSmithingSkill { get; set; } = true;
    }
}