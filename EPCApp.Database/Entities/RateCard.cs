

namespace EPCApp.Database.Entities;

public class RateCard : BaseEntity
{
    public string ItemCode { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public string CurrencyCode { get; set; } = "INR";

    public DateTime EffectiveFrom { get; set; }

    public bool IsActive { get; set; } = true;
}