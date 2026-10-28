using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// TM Forum TMF632 Party Management API - ContactMedium (array elemanı)
/// </summary>
public class ContactMediumModel
{
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; } = false;

    [Required]
    [JsonPropertyName("mediumType")]
    public string MediumType { get; set; } = null!; // EmailAddress, TelephoneNumber, PostalAddress

    // Polymorphic deserialize için @type zorunlu
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "ContactMedium";

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }    

    // Characteristic farklı tiplere göre değişir → polymorphic
    [JsonPropertyName("characteristic")]
    [JsonConverter(typeof(ContactCharacteristicConverter))]
    public ContactCharacteristicBase? Characteristic { get; set; }
}

// --------------------- BASE ---------------------
public abstract class ContactCharacteristicBase
{
    [JsonPropertyName("@type")]
    public abstract string Type { get; }
}

// --------------------- EMAIL ---------------------
public class EmailCharacteristic : ContactCharacteristicBase
{
    public override string Type => "EmailAddress";

    [JsonPropertyName("emailAddress")]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;
}

// --------------------- TELEPHONE ---------------------
public class TelephoneCharacteristic : ContactCharacteristicBase
{
    public override string Type => "TelephoneNumber";

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("areaCode")]
    public string? AreaCode { get; set; }

    [JsonPropertyName("localNumber")]
    public string? LocalNumber { get; set; }
}

// --------------------- POSTAL ADDRESS ---------------------
public class PostalAddressCharacteristic : ContactCharacteristicBase
{
    public override string Type => "PostalAddress";

    // TM Forum zorunlu/önerilen – frontend bunları doldurur (gösterim için)
    [JsonPropertyName("street1")]
    public string? Street1 { get; set; }           // "Örnek Sokak No:5 D:3"

    [JsonPropertyName("street2")]
    public string? Street2 { get; set; }           // "Yavuztürk Mah."

    [JsonPropertyName("city")]
    public string City { get; set; } = "İstanbul";

    [JsonPropertyName("stateOrProvince")]
    public string? StateOrProvince { get; set; }   // "Marmara" veya boş

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = "Türkiye";

    // ---------- TÜRKİYE'YE ÖZEL – JsonIgnore ile TM Forum JSON’unda ÇIKMAZ ----------
    [JsonIgnore] public int CityId { get; set; } = 34;         // İstanbul
    [JsonIgnore] public int DistrictId { get; set; }           // Üsküdar ID
    [JsonIgnore] public int? NeighborhoodId { get; set; }      // Yavuztürk Mah. ID
    [JsonIgnore] public int? ExistingAddressId { get; set; }   // var olan adresi seçtiyse
}
