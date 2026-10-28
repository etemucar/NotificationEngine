using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class Customer : ModelBase<int>
{
    public int PartyRoleId { get; set; } 
    public string? CustomerNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public virtual PartyRole PartyRole { get; set; } = null!;
    public virtual ICollection<PartyRoleAccount>? PartyRoleAccounts { get; set; }
}
