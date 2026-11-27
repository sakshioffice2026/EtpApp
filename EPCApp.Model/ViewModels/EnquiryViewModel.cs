using System.ComponentModel.DataAnnotations;
using EPCApp.Model.DTOs;

namespace EPCApp.Model.ViewModel;

public class EnquiryViewModel
{
    public long Id { get; set; }

    [Display(Name = "Enquiry No")]
    public string? EnquiryNo { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Enquiry Date")]
    public DateTime EnquiryDate { get; set; } = DateTime.Today;

    [Required, StringLength(200)]
    [Display(Name = "Enquiry Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [EmailAddress, StringLength(200)]
    [Display(Name = "Contact Email")]
    public string? ContactEmail { get; set; }

    [Phone, StringLength(30)]
    [Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [Range(1, 3, ErrorMessage = "Select a plant type.")]
    [Display(Name = "Plant Type")]
    public int PlantType { get; set; }

    [Range(1, 7, ErrorMessage = "Select an industry.")]
    [Display(Name = "Industry")]
    public int IndustryType { get; set; }

    [Display(Name = "ZLD Required")]
    public bool ZldRequired { get; set; }

    [StringLength(250)]
    [Display(Name = "Site Location")]
    public string? SiteLocation { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public int Status { get; set; }

    public long? ProjectId { get; set; }

    public string PlantTypeName { get; set; } = string.Empty;

    public string IndustryTypeName { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public List<StreamInputDto> Streams { get; set; } = new();

    public List<ProposalDto> Proposals { get; set; } = new();
}