using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class Organization : ModelBase<int>
{
    public int PartyId { get; set; }
    public string Name { get; set; } = null!;
    public string? TaxOffice { get; set; }
    public long TaxNumber { get; set; }
    public long IdentityNumber { get; set; }
    public string? TradeName { get; set; }
    public long TradeRegisterNumber { get; set; }
    public long MersisNo { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }

    public virtual Party Party { get; set; } = null!;
    public virtual ICollection<PartyRole> PartyRoles { get; set; } = null!;
    public virtual ICollection<OrganizationLanguageRel> OrganizationLanguageRels { get; set; } = null!;
}
