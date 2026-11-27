using System.ComponentModel.DataAnnotations;

namespace EtpApp.Model.ViewModel;

public class ProjectViewModel
{
    public long Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Project Code")]
    public string ProjectCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Plant Type")]
    public int PlantType { get; set; }

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

    public string PlantTypeName { get; set; } = string.Empty;

    public string IndustryTypeName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}