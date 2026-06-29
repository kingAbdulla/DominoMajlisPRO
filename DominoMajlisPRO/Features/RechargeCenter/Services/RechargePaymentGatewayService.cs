using DominoMajlisPRO.Features.RechargeCenter.Models;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargePaymentGatewayService
{
    private static readonly HashSet<string> SupportedSandboxMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "google-play",
        "apple-pay",
        "visa",
        "mastercard",
        "zain-cash",
        "qi-card"
    };

    public static Task<PaymentTransactionResult> AuthorizeAsync(
        string playerId,
        string itemTitle,
        string priceText,
        string paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Task.FromResult(PaymentTransactionResult.Failed(paymentMethodId, "لا يوجد لاعب مرتبط بعملية الدفع."));

        if (string.IsNullOrWhiteSpace(paymentMethodId) || !SupportedSandboxMethods.Contains(paymentMethodId))
            return Task.FromResult(PaymentTransactionResult.Failed(paymentMethodId, "طريقة الدفع غير مدعومة حالياً."));

        var transactionId = $"sandbox-{paymentMethodId}-{Guid.NewGuid():N}";
        var message = ResolveMessage(paymentMethodId, itemTitle, priceText);

        return Task.FromResult(new PaymentTransactionResult(
            true,
            "مكتمل - Sandbox",
            message,
            transactionId,
            true,
            paymentMethodId));
    }

    private static string ResolveMessage(string paymentMethodId, string itemTitle, string priceText) =>
        paymentMethodId.ToLowerInvariant() switch
        {
            "zain-cash" => $"تمت محاكاة دفع Zain Cash لـ {itemTitle} بقيمة {priceText}. الربط الحقيقي يحتاج Merchant ID و API Keys من زين كاش.",
            "mastercard" => $"تمت محاكاة دفع MasterCard لـ {itemTitle} بقيمة {priceText}. الربط الحقيقي يحتاج بوابة دفع ومفاتيح تاجر.",
            "visa" => $"تمت محاكاة دفع VISA لـ {itemTitle} بقيمة {priceText}. الربط الحقيقي يحتاج بوابة دفع ومفاتيح تاجر.",
            "qi-card" => $"تمت محاكاة دفع Qi Card لـ {itemTitle} بقيمة {priceText}. الربط الحقيقي يحتاج مزود دفع رسمي.",
            "google-play" => $"تمت محاكاة Google Play Billing لـ {itemTitle} بقيمة {priceText}. الربط النهائي يحتاج Product IDs من المتجر.",
            "apple-pay" => $"تمت محاكاة Apple Pay لـ {itemTitle} بقيمة {priceText}. الربط النهائي يحتاج إعدادات Apple/Payment Provider.",
            _ => $"تمت محاكاة الدفع لـ {itemTitle} بقيمة {priceText}."
        };
}
