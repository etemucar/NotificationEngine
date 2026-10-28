using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Seeding;            // ApiKeyCacheConstants burada değilse bu satırı sil
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public class RegisterOrganizationCommandHandler
    : IRequestHandler<RegisterOrganizationCommand, RegisterOrganizationResult>
{
    private readonly IRepository<Party, int> _partyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEncryptionService _encryptionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RegisterOrganizationCommandHandler> _logger;

    public RegisterOrganizationCommandHandler(
        IRepository<Party, int> partyRepository,
        IPasswordHasher passwordHasher,
        IEncryptionService encryptionService,
        IMemoryCache cache,
        ILogger<RegisterOrganizationCommandHandler> logger)
    {
        _partyRepository = partyRepository;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RegisterOrganizationResult> Handle(
        RegisterOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        // ApplicationUser sadece PartyRoleType ExternalService DEĞİLSE oluşur -
        // yani "bir insan bununla login edebilir" anlamına gelen her rol için.
        var createApplicationUser = request.PartyRoleTypeId != ReferenceData.PartyRoleType.ExternalService;

        if (createApplicationUser && request.LanguageId is null)
            throw new InvalidInputException(
                "ApplicationUser oluşturulacağı için LanguageId zorunludur (PartyRoleTypeId ExternalService değil).");

        // ContactMedium'lar sadece Credential.ContactMedia zincirinden
        // ulaşılabiliyor, Party.ContactMedium koleksiyonundan değil - EF'in
        // PartyId FK'ini doğru çözebilmesi için Party navigasyonunu SaveChanges
        // öncesi elle set etmemiz lazım.
        var contactMediaToFixUp = new List<ContactMedium>();
        var issuedCredentials = new List<IssuedCredentialInfo>();
        var credentials = new List<Credential>();

        foreach (var cr in request.Credentials)
        {
            var characteristics = new List<CredentialCharacteristic>();

            foreach (var ch in cr.Characteristics)
            {
                characteristics.Add(ch.Name switch
                {
                    // Hash'lenenler - bir daha GERİ OKUNMASI gerekmiyor, sadece doğrulama.
                    "password" => new CredentialCharacteristic { Name = "passwordHash", Value = _passwordHasher.Hash(ch.Value) },
                    "apiKey"   => new CredentialCharacteristic { Name = "apiKeyHash", Value = _passwordHasher.Hash(ch.Value) },
                    // Şifrelenen (hash değil): geri okunup dış sisteme gönderilmesi gereken anahtarlar.
                    "outboundApiKey" => new CredentialCharacteristic { Name = "outboundApiKeyEncrypted", Value = _encryptionService.Encrypt(ch.Value) },
                    _          => new CredentialCharacteristic { Name = ch.Name, Value = ch.Value }
                });
            }

            string? generatedRawValue = null;

            // ApiKey tipi ama caller kendi anahtarını vermediyse: sunucu güvenli bir tane üretir.
            if (cr.CredentialType == CredentialType.ApiKey && !cr.Characteristics.Any(c => c.Name == "apiKey"))
            {
                generatedRawValue = GenerateRawApiKey();
                characteristics.Add(new CredentialCharacteristic
                {
                    Name = "apiKeyHash",
                    Value = _passwordHasher.Hash(generatedRawValue)
                });
            }

            // OutboundApiKey tipi ama caller kendi anahtarını vermediyse: sunucu üretir ve
            // şifreleyip saklar; ham değer sadece bu response'ta görünür.
            if (cr.CredentialType == CredentialType.OutboundApiKey && !cr.Characteristics.Any(c => c.Name == "outboundApiKey"))
            {
                generatedRawValue = GenerateRawApiKey();
                characteristics.Add(new CredentialCharacteristic
                {
                    Name = "outboundApiKeyEncrypted",
                    Value = _encryptionService.Encrypt(generatedRawValue)
                });
            }

            var contactMedia = cr.ContactMedia.Select(MapContactMedium).ToList();
            contactMediaToFixUp.AddRange(contactMedia);

            var credential = new Credential
            {
                CredentialType  = cr.CredentialType,
                TrustLevel      = cr.TrustLevel,
                Characteristics = characteristics,
                ContactMedia    = contactMedia
            };

            credentials.Add(credential);
            issuedCredentials.Add(new IssuedCredentialInfo
            {
                CredentialType    = cr.CredentialType,
                GeneratedRawValue = generatedRawValue
                // CredentialId, Handle sonunda (Id atandıktan sonra) dolduruluyor.
            });
        }

        var digitalIdentity = new DigitalIdentity
        {
            Nickname            = request.Nickname ?? request.Name,
            DigitalIdentityDate = DateTime.UtcNow,
            Credentials         = credentials,
            ApplicationUser     = createApplicationUser
                ? new ApplicationUser { LanguageId = request.LanguageId!.Value }
                : null
        };

        var party = new Party
        {
            PartyType = PartyType.Organization,
            Organization = new Organization
            {
                Name                = request.Name,
                TaxOffice           = request.TaxOffice,
                TaxNumber           = request.TaxNumber,
                IdentityNumber      = request.IdentityNumber,
                TradeName           = request.TradeName,
                TradeRegisterNumber = request.TradeRegisterNumber,
                MersisNo            = request.MersisNo,
                ValidForStart       = request.ValidForStart ?? DateTime.MinValue,
                ValidForEnd         = request.ValidForEnd   ?? DateTime.MaxValue
            },
            PartyRoles = new List<PartyRole>
            {
                new PartyRole
                {
                    PartyRoleTypeId = request.PartyRoleTypeId,
                    ValidForStart   = DateTime.UtcNow,
                    ValidForEnd     = DateTime.MaxValue,
                    DigitalIdentity = digitalIdentity
                }
            }
        };

        foreach (var cm in contactMediaToFixUp)
        {
            cm.Party = party;
        }

        await _partyRepository.AddAsync(party, cancellationToken);

        var organization = party.Organization!;
        var partyRole = party.PartyRoles!.First();

        for (var i = 0; i < credentials.Count; i++)
        {
            issuedCredentials[i].CredentialId = credentials[i].Id;
        }

        _logger.LogInformation(
            "Organization kaydedildi. Name: {Name}, PartyId: {PartyId}, PartyRoleTypeId: {PartyRoleTypeId}, ApplicationUser: {HasAppUser}",
            request.Name, party.Id, request.PartyRoleTypeId, createApplicationUser);

        // Yeni bir ApiKey credential'ı eklendiyse ApiKeyAuthenticationHandler'ın cache'ini
        // hemen geçersiz kıl; yoksa yeni anahtar cache süresi (5 dk) boyunca çalışmayabilir.
        if (credentials.Any(c => c.CredentialType == CredentialType.ApiKey))
        {
            _cache.Remove(ApiKeyCacheConstants.CacheKey);
        }

        return new RegisterOrganizationResult
        {
            PartyId           = party.Id,
            OrganizationId    = organization.Id,
            PartyRoleId       = partyRole.Id,
            DigitalIdentityId = digitalIdentity.Id,
            ApplicationUserId = digitalIdentity.ApplicationUser?.Id,
            IssuedCredentials = issuedCredentials
        };
    }

    private static ContactMedium MapContactMedium(ContactMediumRequest cm)
    {
        if (!Enum.TryParse<ContactMediumType>(cm.MediumType, ignoreCase: true, out var mediumType))
            throw new InvalidInputException($"Geçersiz iletişim türü: '{cm.MediumType}'.");

        string? email       = null;
        string? phoneNumber = null;

        switch (mediumType)
        {
            case ContactMediumType.EmailAddress:
                email = cm.Characteristic.GetValueOrDefault("emailAddress")?.ToString();
                break;
            case ContactMediumType.PhoneNumber:
                phoneNumber = cm.Characteristic.GetValueOrDefault("phoneNumber")?.ToString();
                break;
        }

        return new ContactMedium
        {
            MediumType  = mediumType,
            IsPreferred = cm.Preferred,
            Email       = email,
            PhoneNumber = phoneNumber
            // PartyId burada YOK - çağıran kod Party navigasyonunu elle set ediyor.
        };
    }

    private static string GenerateRawApiKey()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32); // 256-bit
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}