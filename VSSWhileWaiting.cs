using System.Collections.Generic;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace VisibleSmithingStaminaWhileWaiting
{
    public partial class SubModule
    {
        public class VSSWhileWaiting : CampaignBehaviorBase
        {
            private const string MessageReplenished = "{=Zqsbdz6MIc9Ex}Party's smithing stamina is replenished";
            private static readonly HashSet<string> StopMenuIds = ["town", "town_wait_menus"];
            private static readonly MCMSettings FallbackSettings = new();
            private static MCMSettings Settings => AttributeGlobalSettings<MCMSettings>.Instance ?? FallbackSettings;

            private readonly List<Hero> _partyHeroes = [];
            private readonly Dictionary<Hero, float> _regenRemainder = [];
            private bool _staminaWasUsed;
            private CraftingCampaignBehavior? _crafting;

            public override void RegisterEvents() => CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);

            public override void SyncData(IDataStore dataStore) => dataStore.SyncData("_vsswStaminaWasUsed", ref _staminaWasUsed);

            private void OnHourlyTick()
            {
                Hero? hero = Hero.MainHero;
                if (hero == null || !IsAbleToRegenerate(hero)) return;
                _crafting ??= Campaign.Current?.GetCampaignBehavior<CraftingCampaignBehavior>();
                if (_crafting == null) return;

                Utilities.FillHeroesInParty(hero, _partyHeroes);
                MCMSettings settings = Settings;
                bool inTown = IsHeroInTown();
                bool inSettlement = hero.CurrentSettlement != null;

                bool usedBefore = AnyStaminaUsed();
                if (usedBefore) _staminaWasUsed = true;

                if (usedBefore && (inSettlement || settings.RegenStaminaWhileTravelling))
                    RegenerateParty(settings, inSettlement);

                bool usedAfter = usedBefore && AnyStaminaUsed();

                if (usedAfter && (inTown ? settings.ShowCurrentPartysStaminaPercentWhileInTown : settings.ShowCurrentStaminaPercentWhileTravelling))
                    LogLowestStaminaPercent();

                if (usedAfter || !_staminaWasUsed) return;

                // Full now: consume the flag regardless of display settings so it can never go stale.
                _staminaWasUsed = false;
                _regenRemainder.Clear();

                if (ShouldDisplayNotifications(settings) && (inTown || settings.ShowNotificationsWhileTravelling))
                    DisplayNotification(settings, MessageReplenished);

                if (inTown && settings.StopWaitingWhenStaminaIsFull)
                    StopWaiting();
            }

            private static bool IsAbleToRegenerate(Hero hero) =>
                hero is { PartyBelongedTo: not null, IsDead: false, IsDisabled: false, IsFugitive: false, IsPrisoner: false, IsReleased: false };

            private static bool IsHeroInTown() =>
                Settlement.CurrentSettlement is { IsTown: true, Town: { InRebelliousState: false, IsUnderSiege: false } };

            private bool AnyStaminaUsed()
            {
                for (int i = 0; i < _partyHeroes.Count; i++)
                {
                    Hero hero = _partyHeroes[i];
                    if (_crafting!.GetHeroCraftingStamina(hero) < _crafting.GetMaxHeroCraftingStamina(hero)) return true;
                }
                return false;
            }

            private void RegenerateParty(MCMSettings settings, bool inSettlement)
            {
                for (int i = 0; i < _partyHeroes.Count; i++)
                {
                    Hero hero = _partyHeroes[i];
                    int max = _crafting!.GetMaxHeroCraftingStamina(hero);
                    int current = _crafting.GetHeroCraftingStamina(hero);
                    if (current >= max) { _regenRemainder.Remove(hero); continue; }

                    int regen = settings.UseSmithingSkillForStaminaRegen
                        ? SkillRegen(hero, settings, inSettlement)
                        : HoursRegen(hero, max, settings);

                    if (regen > 0) _crafting.SetHeroCraftingStamina(hero, MathF.Min(max, current + regen));
                }
            }

            private static int SkillRegen(Hero hero, MCMSettings settings, bool inSettlement)
            {
                int target = MathF.Round((float)hero.GetSkillValue(DefaultSkills.Crafting) / settings.StaminaImmersiveRegenDivisor);
                if (!inSettlement) return MathF.Max(1, target);
                // Vanilla adds its own hourly rate while in a settlement; only top up to the immersive target.
                return MathF.Max(0, target - VanillaHourlyRate(hero));
            }

            // Carries the fractional remainder so HoursToFullStaminaRegen is accurate instead of rounded per hour.
            private int HoursRegen(Hero hero, int max, MCMSettings settings)
            {
                _regenRemainder.TryGetValue(hero, out float carry);
                float exact = (float)max / settings.HoursToFullStaminaRegen + carry;
                int whole = (int)exact;
                _regenRemainder[hero] = exact - whole;
                return whole;
            }

            // Mirrors CraftingCampaignBehavior.GetStaminaHourlyRecoveryRate (private in vanilla).
            private static int VanillaHourlyRate(Hero hero)
            {
                int num = 5 + MathF.Round(hero.GetSkillValue(DefaultSkills.Crafting) * 0.025f);
                if (hero.GetPerkValue(DefaultPerks.Athletics.Stamina))
                    num += MathF.Round(num * DefaultPerks.Athletics.Stamina.PrimaryBonus);
                return num;
            }

            private void LogLowestStaminaPercent()
            {
                int lowest = 100;
                for (int i = 0; i < _partyHeroes.Count; i++)
                {
                    Hero hero = _partyHeroes[i];
                    int percent = _crafting!.GetHeroCraftingStamina(hero) * 100 / _crafting.GetMaxHeroCraftingStamina(hero);
                    if (percent < lowest) lowest = percent;
                }
                TextObject text = new("{=zdTrD1TxVpbQY}Party's stamina is {percent}%");
                text.SetTextVariable("percent", lowest);
                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
            }

            private static bool ShouldDisplayNotifications(MCMSettings settings) =>
                settings.ShowMessageInTheLog || settings.ShowMessageOnTheScreen || settings.ShowMessageAsPopUp;

            private static void DisplayNotification(MCMSettings settings, string message)
            {
                TextObject text = new(message);
                if (settings.ShowMessageInTheLog)
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                if (settings.ShowMessageOnTheScreen)
                    MBInformationManager.AddQuickInformation(text, 2000, null, null, "event:/ui/notification/quest_start");
                if (settings.ShowMessageAsPopUp)
                    Campaign.Current?.CampaignInformationManager?.NewMapNoticeAdded(new CustomSmithingStaminaMapNotification(text));
            }

            private static void StopWaiting()
            {
                Campaign? campaign = Campaign.Current;
                string? menuId = campaign?.CurrentMenuContext?.GameMenu?.StringId;
                if (menuId == null || !StopMenuIds.Contains(menuId)) return;
                if (menuId == "town_wait_menus")
                {
                    // Same as vanilla's "Stop waiting" option.
                    if (PlayerEncounter.Current != null) PlayerEncounter.Current.IsPlayerWaiting = false;
                    GameMenu.SwitchToMenu("town");
                }
                campaign!.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }
    }
}