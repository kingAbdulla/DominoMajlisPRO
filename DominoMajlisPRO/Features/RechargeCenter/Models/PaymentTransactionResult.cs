namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed record PaymentTransactionResult(
    bool Approved,
    string Status,
    string Message,
    string ProviderTransactionId,
    bool IsSandbox,
    string PaymentMethodId)
{
    public static PaymentTransactionResult Failed(string paymentMethodId, string message) =>
        new(false, "فشل", message, string.Empty, true, paymentMethodId);
}
