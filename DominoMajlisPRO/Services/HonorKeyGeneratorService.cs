using System.Security.Cryptography;
using System.Text;

namespace DominoMajlisPRO.Services;

public static class HonorKeyGeneratorService
{
    public static List<string> GenerateFounderKeys(
        int startFounderNumber,
        int count = 5)
    {
        List<string> keys =
            new();

        for (int i = 0; i < count; i++)
        {
            int founderNumber =
                startFounderNumber + i;

            keys.Add(
                $"DMP-FND-{founderNumber:0000}-{RandomBlock()}-{RandomBlock()}");
        }

        return keys;
    }

    public static List<string> GenerateHonorKeys(
        int count = 5)
    {
        List<string> keys =
            new();

        for (int i = 0; i < count; i++)
        {
            keys.Add(
                $"DMP-HNR-{RandomBlock()}-{RandomBlock()}-{RandomBlock()}");
        }

        return keys;
    }

    public static List<string> GenerateDeveloperKeys(
        int count = 5)
    {
        List<string> keys =
            new();

        for (int i = 0; i < count; i++)
        {
            keys.Add(
                $"DMP-DEV-{RandomBlock()}-{RandomBlock()}-{RandomBlock()}");
        }

        return keys;
    }

    public static string BuildKeysText(
        string title,
        List<string> keys)
    {
        StringBuilder builder =
            new();

        builder.AppendLine(
            "Domino Majlis PRO");

        builder.AppendLine(
            title);

        builder.AppendLine(
            $"Created At: {DateTime.Now:yyyy/MM/dd HH:mm}");

        builder.AppendLine(
            "--------------------------------");

        for (int i = 0; i < keys.Count; i++)
        {
            builder.AppendLine(
                $"{i + 1}. {keys[i]}");
        }

        builder.AppendLine();
        builder.AppendLine(
            "ملاحظة مهمة:");

        builder.AppendLine(
            "هذه المفاتيح خاصة ولا يجب مشاركتها إلا مع الأشخاص المعتمدين.");

        builder.AppendLine(
            "كل مفتاح يجب أن يستخدم من الشخص المخصص له فقط.");

        return builder.ToString();
    }

    public static async Task<string> CreateKeysFileAsync(
        string title,
        List<string> keys)
    {
        string fileName =
            $"DominoMajlisPRO_Keys_{DateTime.Now:yyyy_MM_dd_HH_mm}.txt";

        string filePath =
            Path.Combine(
                FileSystem.CacheDirectory,
                fileName);

        string text =
            BuildKeysText(
                title,
                keys);

        await File.WriteAllTextAsync(
            filePath,
            text);

        return filePath;
    }

    static string RandomBlock()
    {
        const string chars =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        char[] result =
            new char[4];

        byte[] bytes =
            RandomNumberGenerator.GetBytes(4);

        for (int i = 0; i < result.Length; i++)
        {
            result[i] =
                chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }
}