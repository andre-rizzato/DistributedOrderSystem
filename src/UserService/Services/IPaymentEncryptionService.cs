using System.Security.Cryptography;
using System.Text;

namespace UserService.Services;

/// <summary>
/// Servizio per criptare/decriptare informazioni sensibili dei metodi di pagamento
/// </summary>
public interface IPaymentEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string GetLast4Digits(string cardNumber);
}

public class PaymentEncryptionService : IPaymentEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public PaymentEncryptionService(IConfiguration configuration)
    {
        // In produzione, usa Azure Key Vault o AWS KMS
        var encryptionKey = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption key not configured");
        var encryptionIv = configuration["Encryption:IV"] ?? throw new InvalidOperationException("Encryption IV not configured");
        
        _key = Encoding.UTF8.GetBytes(encryptionKey);
        _iv = Encoding.UTF8.GetBytes(encryptionIv);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }

    public string GetLast4Digits(string cardNumber)
    {
        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");
        return cleanNumber.Length >= 4 ? cleanNumber[^4..] : cleanNumber;
    }
}
