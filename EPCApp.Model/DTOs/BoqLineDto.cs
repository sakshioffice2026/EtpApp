namespace EPCApp.Model.DTOs;

public class BoqLineDto
{
    public long Id { get; set; }

    public string? TagNo { get; set; }
    public int Category { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }
}