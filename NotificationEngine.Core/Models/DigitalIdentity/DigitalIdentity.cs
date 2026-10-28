using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class DigitalIdentity : ModelBase<Guid>
{
    public string? Nickname { get; set; }
    public DateTime DigitalIdentityDate { get; set; }
    public int PartyRoleId { get; set; }
    
    public PartyRole PartyRole { get; set; } = null!;
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();
}
