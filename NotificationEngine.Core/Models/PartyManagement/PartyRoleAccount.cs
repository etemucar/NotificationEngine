using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class PartyRoleAccount : ModelBase<int>
{
    public int PartyRoleId { get; set; }
    public string CurrencyCode { get; set; } = null!;

    public virtual PartyRole PartyRole { get; set; } = null!;

}
