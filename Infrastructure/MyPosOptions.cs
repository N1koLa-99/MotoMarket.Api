namespace MotoMarket.Api.Infrastructure;

public class MyPosOptions
{
    public const string SectionName = "MyPos";

    public bool UseSandbox { get; set; }
    public string ApiUrl { get; set; } = default!;
    public string Sid { get; set; } = default!;
    public string WalletNumber { get; set; } = default!;
    public int KeyIndex { get; set; }
    public string MerchantPrivateKeyPem { get; set; } = default!;
    public string MyPosPublicKeyPem { get; set; } = default!;
    public string Language { get; set; } = "EN";
    public string BaseCallbackUrl { get; set; } = default!;
    public string FrontendSuccessUrl { get; set; } = default!;
    public string FrontendCancelUrl { get; set; } = default!;
    public bool SimulateSuccessfulPayments { get; set; }
}