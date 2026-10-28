
using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class Individual : ModelBase<int>
{
    public int PartyId { get; set; }

    public string GivenName      { get; set; } = null!;
    public string FamilyName     { get; set; } = null!;
    public string? MiddleName    { get; set; }
    public string? Title         { get; set; }
    public string? Gender        { get; set; }
    public string? Nationality   { get; set; }
    public DateTime? BirthDate   { get; set; }
    public DateTime? DeathDate   { get; set; }
    public string? PlaceOfBirth  { get; set; }
    public string? CountryOfBirth { get; set; }
    public string? MaritalStatus { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }

    public virtual Party Party   { get; set; } = null!;
}
