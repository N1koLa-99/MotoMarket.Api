namespace MotoMarket.Api.Services.Interfaces;

public interface IMyPosSignatureService
{
    string Sign(IDictionary<string, string> fields);
    bool Verify(IReadOnlyList<KeyValuePair<string, string>> fieldsInOriginalOrder, string signature);
}