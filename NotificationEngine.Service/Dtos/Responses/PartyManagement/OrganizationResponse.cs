namespace NotificationEngine.Service.Dtos.Responses;

public class OrganizationResponse
{
    public int     Id                   { get; set; }
    public string  Name                 { get; set; } = null!;
    public string? TaxOffice            { get; set; }
    public long    TaxNumber            { get; set; }
    public long    IdentityNumber       { get; set; }
    public string? TradeName            { get; set; }
    public long    TradeRegisterNumber  { get; set; }
    public long    MersisNo             { get; set; }
    public TimePeriodResponse           ValidFor      { get; set; } = new();
    public List<ContactMediumResponse>  ContactMedium { get; set; } = new();
    public List<RelatedPartyResponse>   RelatedParty  { get; set; } = new();
}
