namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Kind of data collected for a teacher registration requirement.
/// </summary>
public enum RegistrationRequirementType
{
    File = 1,
    Text = 2,
    Boolean = 3,
    /// <summary>Admin-defined picklist (single or multi-select). Options live in OptionsJson.</summary>
    Selection = 4
}
