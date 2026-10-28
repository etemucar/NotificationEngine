namespace NotificationEngine.Service.Dtos.Responses;

public class TimePeriodResponse
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime   { get; set; }
}

public class ContactMediumResponse
{
    public string MediumType { get; set; } = null!;
    public bool   Preferred  { get; set; }
    public TimePeriodResponse ValidFor { get; set; } = new();
    public ContactMediumCharacteristicResponse Characteristic { get; set; } = new();
}

public class ContactMediumCharacteristicResponse
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

public class RelatedPartyResponse
{
    public string Role          { get; set; } = null!;
    public string Type          { get; set; } = null!;
    public PartyOrPartyRoleResponse PartyOrPartyRole { get; set; } = new();
}

public class PartyOrPartyRoleResponse
{
    public string Id           { get; set; } = null!;
    public string Name         { get; set; } = null!;
    public string Type         { get; set; } = null!;
    public string ReferredType { get; set; } = null!;
}
