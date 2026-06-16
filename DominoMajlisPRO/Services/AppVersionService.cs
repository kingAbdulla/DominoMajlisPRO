using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class AppVersionService
{
    public static AppVersionInfoModel GetVersionInfo()
    {
        return new AppVersionInfoModel
        {
            AppName = "Domino Majlis PRO",
            Version = "1.2.0",
            Build = AppInfo.Current.BuildString,
            ReleaseType = "نسخة تجريبية داخلية",
            Developer = "King Esmat",
            UpdateStatus = "لا يوجد تحديث جديد حالياً",

            LatestUpdates =
            {
                "تحسين اختيار الفرق",
                "تحسين الأداء",
                "إضافة Hall Of Fame",
                "إضافة النسخ الاحتياطي",
                "إضافة الاستعادة",
                "إضافة إدارة البيانات",
                "إضافة حالة البيانات",
                "إضافة التشخيص"
            }
        };
    }

    public static string BuildCopyText(
        AppVersionInfoModel info)
    {
        return
            $"{info.AppName}\n" +
            $"الإصدار: {info.Version}\n" +
            $"البناء: {info.Build}\n" +
            $"نوع النسخة: {info.ReleaseType}\n" +
            $"المطور: {info.Developer}\n" +
            $"حالة التحديث: {info.UpdateStatus}\n\n" +
            $"آخر التحديثات:\n" +
            string.Join(
                "\n",
                info.LatestUpdates.Select(x => $"✓ {x}"));
    }
}