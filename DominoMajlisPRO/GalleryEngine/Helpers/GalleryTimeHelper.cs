namespace DominoMajlisPRO.GalleryEngine.Helpers;

public static class GalleryTimeHelper
{
    public static TimeSpan GetRemainingTime(DateTime endDate)
    {
        var remaining = endDate - DateTime.Now;

        return remaining.TotalSeconds <= 0
            ? TimeSpan.Zero
            : remaining;
    }

    public static bool IsExpired(DateTime endDate)
    {
        return DateTime.Now > endDate;
    }

    public static bool IsActive(DateTime startDate, DateTime endDate)
    {
        var now = DateTime.Now;

        return now >= startDate && now <= endDate;
    }

    public static string GetDaysText(DateTime endDate)
    {
        var remaining = GetRemainingTime(endDate);
        return remaining.Days.ToString("00");
    }

    public static string GetHoursText(DateTime endDate)
    {
        var remaining = GetRemainingTime(endDate);
        return remaining.Hours.ToString("00");
    }

    public static string GetMinutesText(DateTime endDate)
    {
        var remaining = GetRemainingTime(endDate);
        return remaining.Minutes.ToString("00");
    }

    public static string GetCountdownShort(DateTime endDate)
    {
        var remaining = GetRemainingTime(endDate);

        if (remaining == TimeSpan.Zero)
            return "انتهى";

        if (remaining.Days > 0)
            return $"{remaining.Days} يوم";

        if (remaining.Hours > 0)
            return $"{remaining.Hours} ساعة";

        return $"{remaining.Minutes} دقيقة";
    }

    public static string GetCountdownFull(DateTime endDate)
    {
        var remaining = GetRemainingTime(endDate);

        if (remaining == TimeSpan.Zero)
            return "انتهى";

        return $"{remaining.Days:00} يوم {remaining.Hours:00} ساعة {remaining.Minutes:00} دقيقة";
    }
}