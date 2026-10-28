namespace NotificationEngine.Core.Common.References;

/// <summary>
/// Seed verilerinde kullanılan sabit Id'ler. InitialSeedData.cs'teki tüm
/// listeler buradan besleniyor — aynı Id iki yerde farklı yazılıp
/// tutarsızlık çıkmasın diye tek kaynak burası.
///
/// NOT: Currency Id 1/2/3 (Try/Usd/Eur) InitialSeedData.cs'te zaten mevcuttu
/// (eski Code="TRL" -> "TRY" olarak düzeltildi, Id değişmedi). Yeni eklenen
/// 22 para birimi Id=4'ten devam ediyor.
/// </summary>
public static class ReferenceData
{

    public static class Language    
    {
        public const int Turkish = 1;
        public const int English = 2;
        public const int Russian = 3;
    }

    public static class LocalizableFields
    {
        public const int StatusName = 1;
        public const int StatusDescription = 2;
    }

    // --- Currency Id'leri ---
    public static class Currencies
    {
        public const int Try = 1;
        public const int Usd = 2;
        public const int Eur = 3;
        public const int Gbp = 4;
        public const int Jpy = 5;
        public const int Chf = 6;
        public const int Cny = 7;
        public const int Rub = 8;
        public const int Aud = 9;
        public const int Cad = 10;
        public const int Aed = 11;
        public const int Sar = 12;
        public const int Kwd = 13;
        public const int Qar = 14;
        public const int Jod = 15;
        public const int Egp = 16;
        public const int Inr = 17;
        public const int Sek = 18;
        public const int Nok = 19;
        public const int Dkk = 20;
        public const int Pln = 21;
        public const int Czk = 22;
        public const int Huf = 23;
        public const int Zar = 24;
        public const int Krw = 25;        
    }
    
    // --- PartyRoleType Id'leri ---
    public static class PartyRoleType
    {
        public const int SiteAdmin   = 1;
        public const int User        = 2;
        public const int Customer    = 3;
        public const int BillAccount = 4;
        public const int ExternalService = 5; // sadece ApiKey ile çalışan servisler (FinYo, Censitor); ApplicationUser oluşmaz

        public const string SiteAdminCd   = "SITE_ADMIN";
        public const string UserCd        = "USER";
        public const string CustomerCd    = "CUSTOMER";
        public const string BillAccountCd = "BILL_ACCOUNT";
        public const string ExternalServiceCd = "EXTERNAL_SERVICE";

        // Token'a rol claim'i gömerken PartyRoleTypeId'den Cd üretmek için —
        // ekstra bir DB sorgusuna gerek kalmasın diye (InitialSeedData ile senkron tutulmalı).
        public static string ToCode(int partyRoleTypeId) => partyRoleTypeId switch
        {
            SiteAdmin   => SiteAdminCd,
            User        => UserCd,
            Customer    => CustomerCd,
            BillAccount => BillAccountCd,
            ExternalService => ExternalServiceCd,
            _ => throw new ArgumentOutOfRangeException(
                nameof(partyRoleTypeId), $"Bilinmeyen PartyRoleType Id: {partyRoleTypeId}")
        };
    }

    // --- RelatedPartyType Id'leri (Party ↔ Party bağlantı türleri; PartyRoleType'tan bağımsız) ---
    public static class RelatedPartyType
    {
        public const int OrganizationMember = 1; // çalışan → organizasyon (kullanıcı başına tek açık kayıt)
        public const int DepartmentMember   = 2; // çalışan → departman (birden çok olabilir)

        public const string OrganizationMemberCd = "ORGANIZATION_MEMBER";
        public const string DepartmentMemberCd   = "DEPARTMENT_MEMBER";
    }

    // --- MarketDataProvider Id'leri (2026-08 eklendi — eski enum'ın yerine) ---
    public static class MarketDataProviders
    {
        public const int Yahoo     = 1;
        public const int CoinGecko = 2; // 2026-08: kripto icin artik kullanilmiyor
                                         // (API key gerektiriyor, Binance'e gecildi) -
                                         // tablo/kayit duruyor, ileride tekrar
                                         // kullanilabilir diye silinmedi.
        public const int Binance   = 3; // 2026-08 eklendi - kripto icin aktif saglayici
        public const int Fixed     = 4; // 2026-08 eklendi - USDT gibi peg'li
                                         // stablecoin'ler icin sabit deger donen saglayici

        public const string YahooCd     = "YAHOO";
        public const string CoinGeckoCd = "COINGECKO";
        public const string BinanceCd   = "BINANCE";
        public const string FixedCd     = "FIXED";
    }

    // --- Sentetik Exchange ---
    public const int OtcExchange = 9000;

    // --- Kripto Exchange ---
    public const int CryptoExchange = 9420;

    // --- Country Id'leri ---
    public const int Turkey = 9800;
    public const int InternationalCountry = 9801; // OTC/Crypto gibi ülkesiz borsalar için sentetik

    // --- FX Instrument / CurrencyDetail Id aralığı (24 satır, TRY hariç) ---
    public const int FxInstrumentRangeStart = 9101;
    public const int FxCurrencyDetailRangeStart = 9201;

    // --- Commodity Instrument / CommodityDetail Id aralığı ---
    public const int CommodityInstrumentRangeStart = 9301;
    public const int CommodityDetailRangeStart = 9351;

    // --- Crypto Instrument / CryptoDetail Id aralığı ---
    // NOT: Sector aralığı (9500-9510) ile Stock aralığı (10000+) arasında
    // boşta kalan bloktan seçildi, büyüme payı bırakıldı.
    public const int CryptoInstrumentRangeStart = 9600;
    public const int CryptoDetailRangeStart = 9650;

    // --- InstrumentProviderMapping Id aralığı (2026-08 eklendi) ---
    // Coin ve döviz enstrümanları için Yahoo sembol eşlemeleri (36 satır:
    // 24 FX + 2 emtia + 10 kripto). CryptoDetail (9650+) ile Turkey (9800)
    // arasındaki boş bloktan seçildi.
    public const int InstrumentProviderMappingRangeStart = 9700; // 9700-9735 arası kullanılıyor

    // ============================================================
    // BIST / Stock domain — YENİ EKLENDİ
    // ============================================================

    // --- BIST Exchange + Pay Piyasası Market ---
    public const int BistExchange = 9400;
    public const int BistPayPiyasasiMarket = 9410;

    // --- Sector Id aralığı (11 sektör: 9500-9510) ---
    public const int SectorRangeStart = 9500;

    // --- BIST Stock Instrument / StockDetail Id aralığı (619 hisse) ---
    // NOT: Id çakışmasın diye FX/Commodity aralıklarından (9xxx) uzak,
    // geniş bir blok seçildi. 619 kayıt var, ~1000'lik pay bırakıldı
    // (ileride KAP'tan yeni sembol eklenirse aralık taşmasın diye).
    public const int StockInstrumentRangeStart = 10000; // 10000-10618 arası kullanılıyor
    public const int StockDetailRangeStart = 20000;      // 20000-20618 arası kullanılıyor

}