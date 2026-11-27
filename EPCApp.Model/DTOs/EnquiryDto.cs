namespace EPCApp.Model.DTOs;

public class EnquiryDto
{
    public long Id { get; set; }

    public string EnquiryNo { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int PlantType { get; set; }

    public int IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public int Status { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<EnquiryStreamDto> Streams { get; set; } = new();
}