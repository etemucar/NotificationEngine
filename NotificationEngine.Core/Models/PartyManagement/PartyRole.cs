using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class PartyRole : ModelBase<int>
{
    public int PartyId { get; set; }
    public int PartyRoleTypeId { get; set; } 
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }

    public virtual Party Party { get; set; } = null!;
    public virtual PartyRoleType PartyRoleType { get; set; } = null!;
    public virtual Customer? Customer { get; set; }         
    public virtual DigitalIdentity? DigitalIdentity { get; set; }
    public virtual PartyRoleAccount? PartyRoleAccount { get; set; }
}
