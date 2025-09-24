namespace Pm.Helper
{
    public static class TimeHelper
    {
        public static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

            if (timeSpan <= TimeSpan.FromSeconds(60))
                return "Baru saja";

            if (timeSpan <= TimeSpan.FromMinutes(60))
                return $"{timeSpan.Minutes} menit yang lalu";

            if (timeSpan <= TimeSpan.FromHours(24))
                return $"{timeSpan.Hours} jam yang lalu";

            if (timeSpan <= TimeSpan.FromDays(30))
                return $"{timeSpan.Days} hari yang lalu";

            if (timeSpan <= TimeSpan.FromDays(365))
                return $"{timeSpan.Days / 30} bulan yang lalu";

            return $"{timeSpan.Days / 365} tahun yang lalu";
        }
    }
}