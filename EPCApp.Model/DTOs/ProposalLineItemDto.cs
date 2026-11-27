namespace EPCApp.Model.DTOs;

public class ProposalLineItemDto
{
    public long Id { get; set; }

    public long ProposalId { get; set; }

    public int Category { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }
}