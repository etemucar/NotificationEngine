using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Core.Seeding
{
    public static class InitialSeedData {
        public static readonly List<Language> Languages = new()
        {
            new() { Id = ReferenceData.Language.Turkish, LanguageCd = "tr", Name = "Türkçe" },
            new() { Id = ReferenceData.Language.English, LanguageCd = "en", Name = "English" },
            new() { Id = ReferenceData.Language.Russian, LanguageCd = "ru", Name = "Русский" }
        };

        public static readonly List<LocalizableFields> LocalizableFields = new()
        {
            new() { Id = ReferenceData.LocalizableFields.StatusName, EntityType = "Status", EntityField = "Name" },
            new() { Id = ReferenceData.LocalizableFields.StatusDescription, EntityType = "Status", EntityField = "Description" },
        };

        public static readonly List<PartyRoleType> PartyRoleTypes = new()
        {
            new() { Id = ReferenceData.PartyRoleType.SiteAdmin, PartyRoleTypeCd = "SITE_ADMIN", Name = "Site Yöneticisi" },
            new() { Id = ReferenceData.PartyRoleType.User, PartyRoleTypeCd = "USER", Name = "Uygulama Kullanıcısı" },
            new() { Id = ReferenceData.PartyRoleType.Customer, PartyRoleTypeCd = "CUSTOMER", Name = "Müşteri" },
            new() { Id = ReferenceData.PartyRoleType.BillAccount, PartyRoleTypeCd = "BILL_ACCOUNT", Name = "Fatura Hesabı" },
            new() { Id = ReferenceData.PartyRoleType.ExternalService, PartyRoleTypeCd = ReferenceData.PartyRoleType.ExternalServiceCd, Name = "Dış Servis" }
        };

        public static readonly List<RelatedPartyType> RelatedPartyTypes = new()
        {
            new() { Id = ReferenceData.RelatedPartyType.OrganizationMember, Code = ReferenceData.RelatedPartyType.OrganizationMemberCd, Name = "Organizasyon Üyesi" },
            new() { Id = ReferenceData.RelatedPartyType.DepartmentMember, Code = ReferenceData.RelatedPartyType.DepartmentMemberCd, Name = "Departman Üyesi" }
        };

        // Ortak (OrganizationId = null) şablonlar. Id'ler 1000'den başlar: uygulamanın sonradan ekleyeceği
        // şablonların otomatik Id'leri 1'den artar, seed Id'leriyle çakışmasın.
        // Status açıkça Active: ModelBase varsayılanı Created, şablon araması Active filtresi kullanır.
        public static readonly List<MessageTemplate> MessageTemplates = new()
        {
            new()
            {
                Id = 1001,
                OrganizationId = null,
                Code = "PASSWORD_RESET",
                Channel = Channel.Sms,
                Culture = "tr-TR",
                Subject = null,
                // SMS gövdesi ASCII (Türkçe karakter yok): tek parça gider.
                Body = "Sifre sifirlama kodunuz: {{code}}. {{expiresInMinutes}} dakika gecerlidir. Kodu kimseyle paylasmayin.",
                Status = GeneralStatus.Active
            },
            new()
            {
                Id = 1002,
                OrganizationId = null,
                Code = "PASSWORD_RESET",
                Channel = Channel.Email,
                Culture = "tr-TR",
                Subject = "Şifre sıfırlama talebi",
                Body = "Merhaba,\n\nHesabınız için bir şifre sıfırlama talebi aldık. Şifrenizi yenilemek için aşağıdaki bağlantıyı kullanın:\n\n{{resetLink}}\n\nBu bağlantı {{expiresInMinutes}} dakika boyunca geçerlidir. Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz.\n",
                Status = GeneralStatus.Active
            }
        };

    }

}