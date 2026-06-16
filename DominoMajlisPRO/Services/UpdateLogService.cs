using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class UpdateLogService
{
    public static List<UpdateLogModel> GetUpdateLogs()
    {
        return new List<UpdateLogModel>
        {
            new UpdateLogModel
            {
                Version = "1.2.0",
                ReleaseDate = "2026/06/13",
                Updates =
                {
                    "تحسين اختيار الفرق",
                    "تحسين الأداء",
                    "إضافة Hall Of Fame",
                    "إضافة النسخ الاحتياطي",
                    "إضافة الاستعادة",
                    "إضافة إدارة البيانات",
                    "إضافة حالة البيانات",
                    "إضافة التشخيص",
                    "إضافة معلومات الإصدار"
                }
            },

            new UpdateLogModel
            {
                Version = "1.1.0",
                ReleaseDate = "نسخة تطوير",
                Updates =
                {
                    "تحسين صفحة التصنيفات",
                    "تحسين صفحة السجل",
                    "تحسين نظام الفرق",
                    "تحسين المزامنة بين الصفحات"
                }
            },

            new UpdateLogModel
            {
                Version = "1.0.0",
                ReleaseDate = "الإصدار الأول",
                Updates =
                {
                    "إطلاق نظام حساب النقاط",
                    "إضافة إنشاء الفرق",
                    "إضافة سجل المباريات",
                    "إضافة التصنيفات الأساسية"
                }
            }
        };
    }
}