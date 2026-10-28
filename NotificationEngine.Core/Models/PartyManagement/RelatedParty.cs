using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class RelatedParty : ModelBase<int>
{
    // Bağlantıyı kuran taraf (örn. çalışanın Party'si) — aşağıdan yukarıya yön: Individual → Organization
    public int PartyId { get; set; }

    // İlişkili taraf (örn. organizasyonun Party'si) — doğrudan Party FK
    public int RelatedPartyId { get; set; }

    // Bağlantının türü: OrganizationMember, DepartmentMember...
    // (Eski string "Role" alanı yerine; API'deki TMF "role" metni RelatedPartyType.Code'a çözülür.)
    public int RelatedPartyTypeId { get; set; }

    // Geçerlilik süresi (TM Forum'da TimePeriod). ValidTo boş = açık/aktif bağlantı.
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public virtual Party Party { get; set; } = null!;
    public virtual Party Related { get; set; } = null!;
    public virtual RelatedPartyType RelatedPartyType { get; set; } = null!;
}