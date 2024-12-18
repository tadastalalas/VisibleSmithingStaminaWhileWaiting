using HarmonyLib;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;


namespace VisibleSmithingStaminaWhileWaiting
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

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
                InformationManager.DisplayMessage(new InformationMessage(new TextObject(_message).ToString()));
                
                MBInformationManager.AddQuickInformation(new TextObject(_message), 1000, null, "");

                // Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ConspiracyQuestMapNotification(this, this.SideNotificationText));

                SetMessageToBeDisplayed(false);
            }
        }
    }
}