using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

public class SystemSetting : AuditableEntity
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Key { get; set; } = default!;
    
    [Required]
    public string Value { get; set; } = default!;
    
    [MaxLength(200)]
    public string? DescriptionAr { get; set; }
    
    [MaxLength(200)]
    public string? DescriptionEn { get; set; }
    
    public SettingType Type { get; set; } // String/Number/Boolean/JSON
    
    public bool IsPublic { get; set; } = false;
}

public enum SettingType
{
    String = 1,
    Number = 2,
    Boolean = 3,
    JSON = 4
}

