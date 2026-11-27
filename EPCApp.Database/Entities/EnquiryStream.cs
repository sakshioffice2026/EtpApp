namespace EPCApp.Database.Entities;

public class EnquiryStream : BaseEntity
{
    public long EnquiryId { get; set; }

    public string StreamName { get; set; } = string.Empty;

    public decimal FlowM3PerDay { get; set; }

    public decimal CodMgL { get; set; }

    public decimal BodMgL { get; set; }

    public decimal TssMgL { get; set; }

    public decimal TdsMgL { get; set; }

    public decimal OilGreaseMgL { get; set; }

    public decimal Ph { get; set; } = 7.00m;

    public decimal TemperatureC { get; set; } = 30.00m;

    public bool IsDeleted { get; set; }

    public Enquiry Enquiry { get; set; } = null!;
}