namespace DominoMajlisPRO.Services;

public static class HallOfLegendsConstitutionService
{
    public static List<string> GetArticles()
    {
        return new()
        {
            "Article 1: Hall Of Legends يعتمد على الإنجاز والنزاهة معاً.",
            "Article 2: لا يدخل الفريق القاعة بالانتصارات فقط دون سجل موثوق.",
            "Article 3: لا يتم استبعاد أي فريق بناءً على الشك وحده.",
            "Article 4: افتراض النزاهة والبراءة هو الأصل حتى تثبت المخالفة.",
            "Article 5: أي إزالة دائمة تتطلب دليلاً موثقاً.",
            "Article 6: كل قرار قبول أو إزالة يجب أن يسجل في Audit Log.",
            "Article 7: تكرار الخصوم وحده ليس دليلاً على الغش.",
            "Article 8: تكرار الفوز وحده ليس دليلاً على الغش.",
            "Article 9: سرعة المباراة وحدها ليست دليلاً على الغش.",
            "Article 10: يتم تقييم السلوك عبر بيانات طويلة المدى.",
            "Article 11: المراجعة تمر بمراحل Watch ثم Investigation ثم Confirmed Fraud.",
            "Article 12: الفريق في Watch يبقى مؤهلاً ما لم تظهر أدلة قوية.",
            "Article 13: الفريق في Investigation لا يزال تحت افتراض البراءة.",
            "Article 14: Confirmed Fraud فقط يسمح بالحظر أو الإزالة.",
            "Article 15: للمطور صلاحية المراجعة النهائية عند التعارض.",
            "Article 16: Founder و Honor لا يملكون صلاحية إزالة نهائية دون سياسة موثقة.",
            "Article 17: سجل Hall Of Legends تاريخي ولا يتم تغييره إلا بسبب موثق."
        };
    }
}