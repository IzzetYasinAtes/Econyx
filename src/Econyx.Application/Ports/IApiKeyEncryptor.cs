namespace Econyx.Application.Ports;

public interface IApiKeyEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string Mask(string apiKey);
}
