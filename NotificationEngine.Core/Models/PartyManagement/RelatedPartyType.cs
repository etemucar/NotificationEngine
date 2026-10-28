using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

/// <summary>
/// İki Party arasındaki bağlantının türü (RelatedParty.RelatedPartyTypeId).
/// PartyRoleType'tan farklı: o, Party ile PartyRole arasındaki ilişkiyi belirler; bu ise
/// Party ile Party arasındaki ilişkiyi (örn. çalışan → organizasyon, çalışan → departman).
/// </summary>
public class RelatedPartyType : ModelBase<int>
{
    public string Code { get; set; } = null!;   // "OrganizationMember", "DepartmentMember"
    public string Name { get; set; } = null!;

    public virtual ICollection<RelatedParty> RelatedParties { get; set; } = null!;
}