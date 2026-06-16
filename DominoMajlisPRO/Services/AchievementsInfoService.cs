using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class AchievementsInfoService
{
    public static InfoSectionModel GetBadgesInfo()
    {
        return new InfoSectionModel
        {
            Title = "شرح الشارات",

            Items = new List<string>
            {
                "Activity: نشاط 7 أيام متتالية",
                "Verified: فريق موثق",
                "Trust: ثقة مرتفعة",
                "Rivalry: منافسة قوية",
                "Season Reward: مكافأة الموسم",
                "MVP: أفضل فريق أداءً",
                "Champion: بطل الموسم",
                "Hall Of Fame: عضو قاعة المشاهير",
                "Founder: مؤسس التطبيق",
                "Developer: مطور التطبيق",
                "Early Adopter: من أوائل المستخدمين",
                "Season Veteran: مخضرم المواسم"
            }
        };
    }

    public static InfoSectionModel GetAchievementRules()
    {
        return new InfoSectionModel
        {
            Title = "شروط الإنجازات",

            Items = new List<string>
            {
                "Activity: 7 أيام نشاط متواصل",
                "Verified: 5 مباريات + ثقة مرتفعة",
                "Trust: TrustScore مرتفع",
                "MVP: نسبة فوز عالية وأداء مميز",
                "Champion: المركز الأول بالموسم",
                "Season Reward: تحقيق متطلبات الموسم",
                "Hall Of Fame: تحقيق شروط القاعة"
            }
        };
    }

    public static InfoSectionModel GetHallOfFameRules()
    {
        return new InfoSectionModel
        {
            Title = "شروط الانضمام إلى قاعة المشاهير",

            Items = new List<string>
            {
                "3000 XP أو أكثر",
                "20 انتصار أو أكثر",
                "نسبة فوز 60% أو أكثر",
                "عدم وجود Suspicious Flag",
                "الالتزام بقواعد اللعب النظيف"
            }
        };
    }
}