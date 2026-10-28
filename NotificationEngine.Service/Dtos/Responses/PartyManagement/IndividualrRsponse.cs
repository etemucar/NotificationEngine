namespace NotificationEngine.Service.Dtos.Responses;

public class IndividualResponse
{
    public int    Id            { get; set; }
    public string GivenName     { get; set; } = null!;
    public string FamilyName    { get; set; } = null!;
    public string? MiddleName    { get; set; }
    public string? Title         { get; set; }
    public string? Gender        { get; set; }
    public string? Nationality   { get; set; }
    public DateTime? BirthDate  { get; set; }
    public string? PlaceOfBirth  { get; set; }
    public string? CountryOfBirth { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Status        { get; set; }
    public TimePeriodResponse ValidFor       { get; set; } = new();
    public List<ContactMediumResponse> ContactMedium { get; set; } = new();
    public List<RelatedPartyResponse>  RelatedParty  { get; set; } = new();
}
