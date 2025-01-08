using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace VisibleSmithingStaminaWhileWaiting
{
    internal class MCMSettings : AttributeGlobalSettings<MCMSettings>
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

        [SettingPropertyInteger("{=ey3zAJ91Tj1Fk}Hours to replenish stamina fully", 6, 168, "0", Order = 2, RequireRestart = false, HintText = "{=hkyjCW0dowCis}In how many hours any hero in party will replenish stamina from 0% to 100%. [Default: 72 hours]")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public int HoursToFullStaminaRegen { get; set; } = 72;

        [SettingPropertyBool("{=6qMBxn8agWxK2}Show notifications while travelling", Order = 3, RequireRestart = false, HintText = "{=zCzem6NK44Yde}Show smithing stamina recovery notifications while hero is travelling. [Regen stamina while travelling] option must be enabled for this to work.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowNotificationsWhileTravelling { get; set; } = true;

        [SettingPropertyBool("{=6oTamV4M8XdZO}[Town] Show current stamina percent", Order = 4, RequireRestart = false, HintText = "{=WtDruY7sQcxsx}Show current smithing stamina percent every hour while waiting in town.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentPartysStaminaPercentWhileInTown { get; set; } = false;

        [SettingPropertyBool("{=ErodgkpkVhxKk}[Travelling] Show current stamina percent", Order = 5, RequireRestart = false, HintText = "{=jLu2I19JjbY84}Show current smithing stamina percent every hour while travelling in the world.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool ShowCurrentStaminaPercentWhileTravelling { get; set; } = false;

        [SettingPropertyBool("{=s2viSnCYOMSCW}Immersive stamina regeneration", Order = 6, RequireRestart = false, HintText = "{=01qKs9tTnkpZk}Regenerate smithing stamina based on hero's smithing skill.\nSimple formula: (Smithing skill / 20) = Regeneration amount per hour.")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public bool UseSmithingSkillForStaminaRegen { get; set; } = true;

        [SettingPropertyInteger("{=eyXXNJ2r9YjOv}Immersive regeneration divisor", 2, 80, "0", Order = 7, RequireRestart = false, HintText = "{=QnNdfiTqAmiD4}A helper number to calculate how much stamina will be replenished every hour if immersive mode is selected. Higher number = longer replenishment. [Default: 20]")]
        [SettingPropertyGroup("{=2wemouR6uduxG}Additional options", GroupOrder = 1)]
        public int StaminaImmersiveRegenDivisor { get; set; } = 20;
    }
}