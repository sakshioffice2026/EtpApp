namespace EPCApp.Model.DTOs;

public class EnquiryDto
{
    public long Id { get; set; }

    public string EnquiryNo { get; set; } = string.Empty;

    public DateTime EnquiryDate { get; set; } = DateTime.UtcNow.Date;

    public string Title { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public int PlantType { get; set; }

    public int IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public string? Remarks { get; set; }

    public int Status { get; set; }

    public long? ProjectId { get; set; }
}