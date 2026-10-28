using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;
public class ContactMedium : ModelBase<int>
{
    public int PartyId { get; set; }
    public Guid? CredentialId { get; set; } 
    public ContactMediumType MediumType { get; set; }
    public bool  IsPreferred { get; set; } 
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int? AddressId { get; set; }


    public virtual Party Party { get; set; } = null!;
    public virtual Credential? Credential { get; set; }

}
