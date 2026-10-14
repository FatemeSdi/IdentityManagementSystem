using System.Globalization;
using System.Text;

namespace IdentityManagementSystem.API.Helpers
{
    /// <summary>
    /// تبدیل زمان UTC به متن فارسی به وقت ایران — مثلاً «پنج‌شنبه ۲۳ مهر ۱۴۰۵ ساعت ۱۴:۳۰».
    /// برای پیامک نوبت ارزیابی و نمایش نوبت‌ها.
    /// </summary>
    public static class PersianDateText
    {
        private static readonly string[] MonthNames =
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        private static string DayName(DayOfWeek day) => day switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یک‌شنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنج‌شنبه",
            _ => "جمعه"
        };

        public static TimeZoneInfo IranTimeZone { get; } = ResolveIranTimeZone();

        private static TimeZoneInfo ResolveIranTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        public static DateTime ToIranLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IranTimeZone);

        public static DateTime FromIranLocal(DateTime iranLocal) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(iranLocal, DateTimeKind.Unspecified), IranTimeZone);

        /// <summary>«پنج‌شنبه ۲۳ مهر ۱۴۰۵ ساعت ۱۴:۳۰»</summary>
        public static string Format(DateTime utc)
        {
            var local = ToIranLocal(utc);
            var pc = new PersianCalendar();
            var text = $"{DayName(local.DayOfWeek)} {pc.GetDayOfMonth(local)} {MonthNames[pc.GetMonth(local) - 1]} {pc.GetYear(local)} ساعت {local:HH:mm}";
            return ToPersianDigits(text);
        }

        /// <summary>«۱۴:۳۰»</summary>
        public static string FormatTime(DateTime utc) => ToPersianDigits(ToIranLocal(utc).ToString("HH:mm", CultureInfo.InvariantCulture));

        public static string ToPersianDigits(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                sb.Append(ch >= '0' && ch <= '9' ? (char)('۰' + (ch - '0')) : ch);
            }
            return sb.ToString();
        }
    }
}
