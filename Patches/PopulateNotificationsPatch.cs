using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;

namespace VisibleSmithingStaminaWhileWaiting.Patches
{
    [HarmonyPatch(typeof(MapNotificationVM), "PopulateTypeDictionary")]
    internal static class PopulateNotificationsPatch
    {
        private static readonly FieldInfo? ItemConstructors = AccessTools.Field(typeof(MapNotificationVM), "_itemConstructors");

        private static void Postfix(MapNotificationVM __instance)
        {
            if (__instance == null || ItemConstructors?.GetValue(__instance) is not Dictionary<Type, Type> dic) return;
            dic[typeof(CustomSmithingStaminaMapNotification)] = typeof(CustomSmithingStaminaMapNotificationVM);
        }
    }
}