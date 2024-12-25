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


namespace VisibleSmithingStaminaWhileWaiting
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("visible_smithing_stamina_while_waiting");
            harmony.PatchAll();
        }

        protected override void OnSubModuleUnloaded() => base.OnSubModuleUnloaded();

        protected override void OnBeforeInitialModuleScreenSetAsRoot() => base.OnBeforeInitialModuleScreenSetAsRoot();

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            Campaign campaign = Campaign.Current;
            bool flag = campaign != null && campaign.GameMode == CampaignGameMode.Campaign;
            if (flag)
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                campaignGameStarter.AddBehavior(new VSSWhileWaiting());
            }
        }

        public class VSSWhileWaiting : CampaignBehaviorBase
        {
            public override void RegisterEvents() => CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);

            public override void SyncData(IDataStore dataStore) { }

            private string _message = "{=Zqsbdz6MIc9Ex}Main hero stamina reached 100%";
            private bool _IsNotificationReadyToBeDisplayed = false;
            private bool _IsStopWaitingReadyToBeExecuted = false;

            private void HourlyTick()
            {
                CheckIfNotificationMeetsConditions();
                CheckIfStopWaitingMeetsConditions();



                if ((!IsHeroInTown()) && !DoesHeroHasMaxStamina() && AttributeGlobalSettings<Settings>.Instance.RegenStaminaWhileTravelling)
                {
                    if (AttributeGlobalSettings<Settings>.Instance.ShowCurrentStaminaPercentWhileTravelling)
                        ShowCurrentHeroSmithingStaminaPercentInLog();

                    AddStaminaToHeroParty();

                    if (DoesHeroHasMaxStamina() && AttributeGlobalSettings<Settings>.Instance.ShowNotificationsWhileTravelling)
                        DisplayStaminaNotification();

                    return;
                }

                if (IsHeroInTown() && AttributeGlobalSettings<Settings>.Instance.ShowCurrentStaminaPercentInTown)
                    ShowCurrentHeroSmithingStaminaPercentInLog();

                if (IsHeroInTown() && DoesHeroHasMaxStamina() && IsAnySettlementNotificationIsAllowedToBeDisplayed())
                {
                    DisplayStaminaNotification();
                    if (AttributeGlobalSettings<Settings>.Instance.StopWaitingWhenStaminaIsFull)
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

            private bool IsHeroInTown() => Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown;

            private bool DoesHeroHasMaxStamina()
            {
                var maxMainHeroStamina = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>().GetMaxHeroCraftingStamina(Hero.MainHero);
                var currentMainHeroStamina = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>().GetHeroCraftingStamina(Hero.MainHero);
                if (currentMainHeroStamina >= maxMainHeroStamina)
                    return true;
                return false;
            }

            private bool IsAnySettlementNotificationIsAllowedToBeDisplayed()
            {
                if (AttributeGlobalSettings<Settings>.Instance.ShowMessageInTheLog || AttributeGlobalSettings<Settings>.Instance.ShowMessageOnTheScreen || AttributeGlobalSettings<Settings>.Instance.ShowMessageAsPopUp)
                    return true;
                return false;
            }

            private void DisplayStaminaNotification()
            {
                if (_IsNotificationReadyToBeDisplayed)
                {
                    if (AttributeGlobalSettings<Settings>.Instance.ShowMessageInTheLog) { }
                        InformationManager.DisplayMessage(new InformationMessage(new TextObject(_message).ToString()));

                    if (AttributeGlobalSettings<Settings>.Instance.ShowMessageOnTheScreen)
                        MBInformationManager.AddQuickInformation(new TextObject(_message), 2000, null, "event:/ui/notification/quest_start");

                    if (AttributeGlobalSettings<Settings>.Instance.ShowMessageAsPopUp)
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
                IEnumerable<Hero> companions = Hero.MainHero.CompanionsInParty;
                IEnumerable<Hero> siblings = Hero.MainHero.Siblings;
                List<Hero> kids = Hero.MainHero.Children;
                AddStamina(Hero.MainHero);
                foreach (Hero companion in companions)
                {
                    AddStamina(companion);
                }
                foreach (Hero sibling in siblings)
                {
                    AddStamina(sibling);
                }
                foreach (Hero kid in kids)
                {
                    AddStamina(kid);
                }
            }

            private void AddStamina(Hero hero)
            {
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int regenStamina = maxStamina / 72;
                if (currentStamina < maxStamina)
                {
                    craftingBehavior.SetHeroCraftingStamina(hero, currentStamina + regenStamina);

                    if (craftingBehavior.GetHeroCraftingStamina(hero) > maxStamina)
                        craftingBehavior.SetHeroCraftingStamina(hero, maxStamina);
                }
            }

            private int CalculateCurrentHeroSmithingStaminaPercent(Hero hero)
            {
                CraftingCampaignBehavior craftingBehavior = CampaignBehaviorBase.GetCampaignBehavior<CraftingCampaignBehavior>();
                int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);
                int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
                int percent = (currentStamina * 100) / maxStamina;
                return percent;
            }

            private void ShowCurrentHeroSmithingStaminaPercentInLog()
            {
                if (DoesHeroHasMaxStamina())
                    return;
                var percent = CalculateCurrentHeroSmithingStaminaPercent(Hero.MainHero);
                MBTextManager.SetTextVariable("percent", percent);
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=zdTrD1TxVpbQY}Main hero stamina is {percent}%").ToString()));
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
            Dictionary<Type, Type> dic = (Dictionary<Type, Type>)__instance.GetType().GetField("_itemConstructors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            dic.Add(typeof(CustomSmithingStaminaMapNotification), typeof(CustomSmithingStaminaMapNotificationVM));
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
        public bool RegenStaminaWhileTravelling { get; set; } = false;

        [SettingPropertyBool("{=6qMBxn8agWxK2}Show notifications while travelling", Order = 2, RequireRestart = false, HintText = "{=zCzem6NK44Yde}Show smithing stamina recovery notifications while hero is travelling. [Regen stamina while travelling] option must be enabled for this to work.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowNotificationsWhileTravelling { get; set; } = false;

        [SettingPropertyBool("{=6oTamV4M8XdZO}[Town] Show current stamina percent", Order = 3, RequireRestart = false, HintText = "{=WtDruY7sQcxsx}Show current smithing stamina percent every hour while waiting in town.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentStaminaPercentInTown { get; set; } = false;

        [SettingPropertyBool("{=ErodgkpkVhxKk}[Travelling] Show current stamina percent", Order = 4, RequireRestart = false, HintText = "{=jLu2I19JjbY84}Show current smithing stamina percent every hour while travelling in the world.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentStaminaPercentWhileTravelling { get; set; } = false;
    }
}