namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public sealed record StoreCmsValidationError(string Field, string Message);
public sealed class StoreCmsValidationResult
{
    private readonly List<StoreCmsValidationError> _errors = new();
    public IReadOnlyList<StoreCmsValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;
    public string FirstMessage => _errors.FirstOrDefault()?.Message ?? string.Empty;
    public StoreCmsValidationResult Add(string field, string message) { _errors.Add(new(field, message)); return this; }
}

public static class StoreCmsValidationEngine
{
    public static StoreCmsValidationResult ValidateRequired(StoreCmsValidationResult result, string field, string? value, string message) => string.IsNullOrWhiteSpace(value) ? result.Add(field, message) : result;
    public static StoreCmsValidationResult ValidateTitle(StoreCmsValidationResult result, string? value, string message = "العنوان مطلوب") => ValidateRequired(result, "Title", value, message);
    public static StoreCmsValidationResult ValidateImage(StoreCmsValidationResult result, string? value, string message = "الصورة مطلوبة") => ValidateRequired(result, "ImagePath", value, message);
    public static StoreCmsValidationResult ValidatePrice(StoreCmsValidationResult result, decimal value, bool allowZero, string message) => value < 0 || (!allowZero && value == 0) ? result.Add("Price", message) : result;
    public static StoreCmsValidationResult ValidateDates(StoreCmsValidationResult result, DateTime startsAt, DateTime endsAt, string message) => startsAt >= endsAt ? result.Add("Dates", message) : result;
}
