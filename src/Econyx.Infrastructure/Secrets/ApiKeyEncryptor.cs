using System.Security.Cryptography;
using System.Text;
using Econyx.Application.Ports;

namespace Econyx.Infrastructure.Secrets;

internal sealed class ApiKeyEncryptor : IApiKeyEncryptor
{
    private static readonly byte[] Key = DeriveKey();
    private const int IvSize = 16;

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
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
        var fullBytes = Convert.FromBase64String(cipherText);

        var iv = new byte[IvSize];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, IvSize);

        var cipherBytes = new byte[fullBytes.Length - IvSize];
        Buffer.BlockCopy(fullBytes, IvSize, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = Key;
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

    private static byte[] DeriveKey()
    {
        var machineId = Environment.MachineName + Environment.UserName;
        return SHA256.HashData(Encoding.UTF8.GetBytes(machineId));
    }
}
