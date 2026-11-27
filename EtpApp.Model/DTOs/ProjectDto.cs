namespace EtpApp.Model.DTOs;

public class ProjectDto
{
    public long Id { get; set; }

    public string ProjectCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public int PlantType { get; set; }

    public int IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public string? Remarks { get; set; }
}