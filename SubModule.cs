using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using System;
using TaleWorlds.Engine;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Attributes;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;


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

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

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
            public override void RegisterEvents()
            {
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            }

            public override void SyncData(IDataStore dataStore)
            {
                
            }

            private bool _canStaminaMessageBeDisplayed = false;
            private string _message = "Main hero stamina reached 100%";

            private void HourlyTick()
            {
                if (!IsHeroInAnySettlement())
                    return;

                if (!DoesHeroHasMaxStamina() && !IsMessageReadyToBeDisplayed())
                {
                    SetMessageToBeDisplayed(true);
                    return;
                }

                if (DoesHeroHasMaxStamina() && IsMessageReadyToBeDisplayed())
                    DisplayStaminaMessage();
            }

            private bool IsHeroInAnySettlement() => Settlement.CurrentSettlement != null;

            private bool DoesHeroHasMaxStamina()
            {
                var maxMainHeroStamina = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>().GetMaxHeroCraftingStamina(Hero.MainHero);
                var currentMainHeroStamina = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>().GetHeroCraftingStamina(Hero.MainHero);
                if (currentMainHeroStamina >= maxMainHeroStamina)
                    return true;
                return false;
            }

            private bool IsMessageReadyToBeDisplayed()
            {
                if (_canStaminaMessageBeDisplayed)
                    return true;
                return false;
            }

            private void SetMessageToBeDisplayed(bool flag) => _canStaminaMessageBeDisplayed = flag;

            private void DisplayStaminaMessage()
            {
                if (AttributeGlobalSettings<Settings>.Instance.ShowMessageInTheLog)
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject(_message).ToString()));

                if (AttributeGlobalSettings<Settings>.Instance.ShowMessageOnTheScreen)
                    MBInformationManager.AddQuickInformation(new TextObject(_message), 2000, null, "event:/ui/notification/quest_start");

                if (AttributeGlobalSettings<Settings>.Instance.ShowMessageAsPopUp)
                    Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new CustomSmithingStaminaMapNotification(new TextObject(_message)));

                SetMessageToBeDisplayed(false);
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
            get
            {
                return new TextObject("Smithing stamina is 100%");
            }
        }

        public override string SoundEventPath
        {
            get
            {
                return "event:/ui/notification/kingdom_decision";
            }
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
            get
            {
                return "VisibleSmithingStaminaWhileWaiting";
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Visible Smithing Stamina While Waiting";
            }
        }

        [SettingPropertyBool("Log message", Order = 0, RequireRestart = false, HintText = "Show smithing stamina message in the log in the bottom left corner of the screen.")]
        [SettingPropertyGroup("Select how you want your smithing stamina message to be shown", GroupOrder = 0)]
        public bool ShowMessageInTheLog { get; set; } = true;

        [SettingPropertyBool("Middle screen message", Order = 0, RequireRestart = false, HintText = "Show smithing stamina message in the middle of the screen.")]
        [SettingPropertyGroup("Select how you want your smithing stamina message to be shown", GroupOrder = 0)]
        public bool ShowMessageOnTheScreen { get; set; } = true;

        [SettingPropertyBool("Popup message", Order = 0, RequireRestart = false, HintText = "Show smithing stamina message as a round pop up on the right side of the screen.")]
        [SettingPropertyGroup("Select how you want your smithing stamina message to be shown", GroupOrder = 0)]
        public bool ShowMessageAsPopUp { get; set; } = true;
    }
    

}