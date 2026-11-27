using System.ComponentModel.DataAnnotations;
using EPCApp.Model.DTOs;

namespace EPCApp.Model.ViewModel;

public class EnquiryFormViewModel
{
    public long Id { get; set; }

    [StringLength(30)]
    [Display(Name = "Enquiry No")]
    public string EnquiryNo { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Project Name")]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Plant Type")]
    public int PlantType { get; set; } = 1;

    [Required]
    [Display(Name = "Industry")]
    public int IndustryType { get; set; }

    [Display(Name = "ZLD Required")]
    public bool ZldRequired { get; set; }

    [StringLength(250)]
    [Display(Name = "Site Location")]
    public string? SiteLocation { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public int Status { get; set; } = 1;

    public List<EnquiryStreamDto> Streams { get; set; } = new();
}