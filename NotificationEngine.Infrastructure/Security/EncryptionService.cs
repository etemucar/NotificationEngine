using NotificationEngine.Core.Security;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace NotificationEngine.Infrastructure.Security
{
    // Sinif static olmaktan çikartildi ve interface implemente edildi
    public class EncryptionService : IEncryptionService
    {
        // NOT: Bu anahtar 32 bayt (256-bit) uzunlugunda olmalidir.
        // Üretim ortaminda konfigürasyondan (Inject edilerek) alinmasi önerilir.
        private readonly byte[] _encryptionKey;


        public EncryptionService(IConfiguration configuration)
        {
            var keyString = configuration["AppSettings:EncryptionKey"];
            if (string.IsNullOrEmpty(keyString) || Encoding.UTF8.GetByteCount(keyString) != 32)
            {
                throw new ArgumentException("Sifreleme anahtari (EncryptionKey) appsettings.json içinde 32 karakter (256-bit) olarak tanimlanmalidir.");
            }
            
            _encryptionKey = Encoding.UTF8.GetBytes(keyString);
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            
            var nonce = new byte[12];
            var tag = new byte[16];
            var ciphertextBytes = new byte[plaintextBytes.Length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            using (var aesGcm = new AesGcm(_encryptionKey, 16))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);
            }

            var resultBytes = new byte[nonce.Length + tag.Length + ciphertextBytes.Length];
            Buffer.BlockCopy(nonce, 0, resultBytes, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, resultBytes, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertextBytes, 0, resultBytes, nonce.Length + tag.Length, ciphertextBytes.Length);

            return Convert.ToBase64String(resultBytes);
        }

        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            var fullCipherBytes = Convert.FromBase64String(ciphertext);

            if (fullCipherBytes.Length < 28) throw new ArgumentException("Geçersiz sifreli veri yapisi.");

            var nonce = new byte[12];
            var tag = new byte[16];
            var ciphertextBytes = new byte[fullCipherBytes.Length - 12 - 16];

            Buffer.BlockCopy(fullCipherBytes, 0, nonce, 0, 12);
            Buffer.BlockCopy(fullCipherBytes, 12, tag, 0, 16);
            Buffer.BlockCopy(fullCipherBytes, 12 + 16, ciphertextBytes, 0, ciphertextBytes.Length);

            var plaintextBytes = new byte[ciphertextBytes.Length];

            using (var aesGcm = new AesGcm(_encryptionKey, 16))
            {
                aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintextBytes);
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        // Extension metot olmaktan çikartildi (this kaldirildi) 
        // ancak mevcut kod yapisi ve islevselligi birebir korundu.
        public string EncodeBase64(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public string DecodeBase64(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
