namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class PaymentMethodModel
{
    public string PaymentMethodId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconKey { get; set; } = "💳";
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
