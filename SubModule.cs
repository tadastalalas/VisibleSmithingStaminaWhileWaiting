using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
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
            var eventField = typeof(CampaignEvents).GetField("HourlyTickEvent", BindingFlags.Static | BindingFlags.NonPublic);
            var eventDelegate = (MulticastDelegate)eventField?.GetValue(null);
            if (eventDelegate != null && eventDelegate.GetInvocationList().Length > 0)
            {
                CampaignEvents.HourlyTickEvent.ClearListeners(this);
            }
            base.OnGameEnd(game);
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
}