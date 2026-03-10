namespace Econyx.Application.Configuration;

public sealed class PlatformOptions
{
    public const string SectionName = "Platform";

    public PolymarketOptions Polymarket { get; set; } = new();
}

public sealed class PolymarketOptions
{
    public string BaseUrl { get; set; } = "https://clob.polymarket.com";
    public string GammaBaseUrl { get; set; } = "https://gamma-api.polymarket.com";
    public int ChainId { get; set; } = 137;
    public string PrivateKeySecretName { get; set; } = "polymarket-private-key";
    public int SignatureType { get; set; } = 2;
}
