using System.Security.Cryptography;
using System.Text;
using Econyx.Application.Ports;
using Microsoft.Extensions.Configuration;

namespace Econyx.Infrastructure.Secrets;

internal sealed class ApiKeyEncryptor : IApiKeyEncryptor
{
    private readonly byte[] _key;
    private const int IvSize = 16;

    public ApiKeyEncryptor(IConfiguration configuration)
    {
        var salt = configuration["Encryption:Salt"] ?? "Econyx-Default-Salt-2024";
        _key = DeriveKey(salt);
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[IvSize + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
        Buffer.BlockCopy(cipherBytes, 0, result, IvSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);

        var fullBytes = Convert.FromBase64String(cipherText);

        if (fullBytes.Length <= IvSize)
            throw new CryptographicException("Invalid cipher text length.");

        var iv = new byte[IvSize];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, IvSize);

        var cipherBytes = new byte[fullBytes.Length - IvSize];
        Buffer.BlockCopy(fullBytes, IvSize, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public string Mask(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return "****";

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }

    private static byte[] DeriveKey(string salt)
    {
        var keyMaterial = $"{Environment.MachineName}-{Environment.UserName}-{salt}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
    }
}
