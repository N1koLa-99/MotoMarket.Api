using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MotoMarket.Api.Services;

public class MyPosSignatureService : IMyPosSignatureService
{
    private readonly MyPosOptions _options;

    public MyPosSignatureService(IOptions<MyPosOptions> options)
    {
        _options = options.Value;
    }

    public string Sign(IDictionary<string, string> fields)
    {
        if (fields == null || fields.Count == 0)
            throw new InvalidOperationException("Няма полета за подпис.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.MerchantPrivateKeyPem);

        var payload = BuildPayload(fields.Select(x => x.Value));
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signatureBytes);
    }

    public bool Verify(IReadOnlyList<KeyValuePair<string, string>> fieldsInOriginalOrder, string signature)
    {
        if (fieldsInOriginalOrder == null || fieldsInOriginalOrder.Count == 0 || string.IsNullOrWhiteSpace(signature))
            return false;

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.MyPosPublicKeyPem);

        var values = fieldsInOriginalOrder
            .Where(x => !string.Equals(x.Key, "Signature", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value ?? string.Empty);

        var payload = BuildPayload(values);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = Convert.FromBase64String(signature);

        return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static string BuildPayload(IEnumerable<string> values)
    {
        var concatenated = string.Join("-", values);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(concatenated));
    }
}