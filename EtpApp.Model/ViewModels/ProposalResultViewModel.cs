using EtpApp.Model.DTOs;

namespace EtpApp.Model.ViewModel;

public class ProposalResultViewModel
{
    public long ProposalId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string PlantTypeName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public List<string> TrainOperations { get; set; } = new();

    public List<EquipmentDto> Equipment { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();
}