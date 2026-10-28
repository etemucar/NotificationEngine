using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Requests;

public class TimePeriodRequest
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime   { get; set; }
}

public class ContactMediumRequest
{
    public string MediumType { get; set; } = null!;
    public bool   Preferred  { get; set; }
    public TimePeriodRequest ValidFor { get; set; } = new();
    public Dictionary<string, object> Characteristic { get; set; } = new();
}

public class ContactMediumCharacteristicRequest
{
    public string? EmailAddress    { get; set; }
    public string? PhoneNumber     { get; set; }
    public string? FaxNumber       { get; set; }
    public string? Street1         { get; set; }
    public string? Street2         { get; set; }
    public string? City            { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostCode        { get; set; }
    public string? Country         { get; set; }
}

public class RelatedPartyRequest
{
    public string Role          { get; set; } = null!;
    public string Type          { get; set; } = null!;
    public PartyOrPartyRoleRequest PartyOrPartyRole { get; set; } = new();
}

public class PartyOrPartyRoleRequest
{
    public string Id           { get; set; } = null!;
    public string Type         { get; set; } = null!;
    public string ReferredType { get; set; } = null!;
}

public class CredentialRequest
{
    public CredentialType CredentialType { get; set; }
    public int? TrustLevel { get; set; }
    public List<CredentialCharacteristicRequest> Characteristics { get; set; } = new();
    public List<ContactMediumRequest> ContactMedia { get; set; } = new();
}

public class CredentialCharacteristicRequest
{
    public string Name { get; set; } = null!;   // "password" — hash'i biz üretiriz
    public string Value { get; set; } = null!;
}
