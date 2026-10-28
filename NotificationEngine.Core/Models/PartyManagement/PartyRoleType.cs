using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class PartyRoleType : ModelBase<int>
    {
    public int? OrganizationId { get; set; }  // null = sistem rolü, dolu = kuruma özel
    public string PartyRoleTypeCd { get; set; } = null!;
    public string Name { get; set; } = null!;


    public virtual Organization? Organization { get; set; }      
    public virtual ICollection<PartyRole> PartyRoles { get; set; } = null!;

}
