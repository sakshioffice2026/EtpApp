using EtpApp.Database.Entities.Common;
using EtpApp.Database.Enums;

namespace EtpApp.Database.Entities;

public class DesignRule : BaseEntity
{
    public PlantType PlantType { get; set; }

    public string Category { get; set; } = string.Empty;

    public string RuleCode { get; set; } = string.Empty;

    public string Parameter { get; set; } = string.Empty;

    public string? AppliesTo { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public string? Unit { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string? SourceReference { get; set; }

    public bool IsVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remarks { get; set; }
}