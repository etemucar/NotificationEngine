
using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;

public class Party : ModelBase<int>
{
    public PartyType PartyType  { get; set; }
    
    public virtual Individual? Individual { get; set; } = null!;
    public virtual Organization? Organization { get; set; } = null!;
    public virtual ICollection<PartyRole>? PartyRoles { get; set; } = null!;
    public virtual ICollection<ContactMedium>? ContactMedium { get; set; } = null!;
    public virtual ICollection<RelatedParty>? RelatedParties { get; set; } = null!;
    
}
