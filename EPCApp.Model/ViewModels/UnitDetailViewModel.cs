using EPCApp.Model.DTOs;

namespace EPCApp.Model.ViewModel;

public class UnitValueItem
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;
}

public class UnitCalcStep
{
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public string Substitution { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string? Note { get; set; }
}

public class UnitExplanation
{
    public List<UnitValueItem> Inputs { get; set; } = new();

    public List<UnitCalcStep> Steps { get; set; } = new();
}

public class UnitEquipmentSection
{
    public string Tag { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string MaterialName { get; set; } = string.Empty;

    public Dictionary<string, decimal> Parameters { get; set; } = new();

    public List<UnitValueItem> Inputs { get; set; } = new();

    public List<UnitCalcStep> Steps { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();
}

public class UnitDetailViewModel
{
    public long ProposalId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string PlantTypeName { get; set; } = string.Empty;

    public string PlantController { get; set; } = "Etp";

    public string UnitTypeName { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public List<UnitEquipmentSection> Sections { get; set; } = new();
}
