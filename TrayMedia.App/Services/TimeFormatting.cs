namespace TrayMedia.Services;

public static class TimeFormatting
{
    public static string Format(double totalSec)
    {
        var sec = Math.Max(0, (int)Math.Floor(totalSec));
        return $"{sec / 60}:{sec % 60:00}";
    }
}
