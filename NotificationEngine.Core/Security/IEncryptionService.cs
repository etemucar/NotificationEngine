namespace NotificationEngine.Core.Security;

/// <summary>
/// AES-256-GCM ile şifreleme/şifre çözme sözleşmesi.
/// CanvasDataSource.EncryptedConnectionData gibi opak blob'ların
/// (backend içeriğini parse etmeden) şifrelenip saklanması için kullanılır.
///
/// NOT: Bu dosya bir varsayımdır — Encryption.cs zaten böyle bir interface
/// implemente ediyorsa (ör. farklı metod isimleriyle), bu dosyayı eklemeyip
/// mevcut olanı kullan; yalnızca handler'lardaki metod adlarını ona göre uyarla.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string EncodeBase64(string plainText);
    string DecodeBase64(string base64EncodedData);
}
