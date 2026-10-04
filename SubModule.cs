using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace VisibleSmithingStaminaWhileWaiting
{
    public partial class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("visible_smithing_stamina_while_waiting");
            harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (Campaign.Current is Campaign campaign && campaign.GameMode == CampaignGameMode.Campaign)
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                campaignGameStarter.AddBehavior(new VSSWhileWaiting());
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

        public override TextObject TitleText => new("{=kq2Rz8Fb1LmwP}Smithing stamina is 100%");
        public override string SoundEventPath => "event:/ui/notification/kingdom_decision";
    }

    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(130572221) { }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(CustomSmithingStaminaMapNotification), 1);
        }

    }
}