using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;

public class Credential : ModelBase<Guid>
{
    public CredentialType CredentialType { get; set; }
    public int? TrustLevel { get; set; }
    public Guid DigitalIdentityId { get; set; }

    public DigitalIdentity DigitalIdentity { get; set; } = null!;
    public ICollection<CredentialCharacteristic> Characteristics { get; set; } = new List<CredentialCharacteristic>();
    public ICollection<ContactMedium> ContactMedia { get; set; } = new List<ContactMedium>();

}
