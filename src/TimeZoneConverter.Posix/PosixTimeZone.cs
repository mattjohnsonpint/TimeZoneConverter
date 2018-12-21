using System;
using System.Globalization;
using System.Text;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;

namespace TimeZoneConverter.Posix
{
    /// <summary>
    /// Provides methods to generate POSIX time zone strings.
    /// </summary>
    public static class PosixTimeZone
    {
        /// <summary>
        /// Generates a POSIX time zone string from a <see cref="TimeZoneInfo"/> object, for the current year.
        /// Note - only uses only the <see cref="TimeZoneInfo.Id"/> property from the object.
        /// </summary>
        /// <param name="timeZoneInfo">The <see cref="TimeZoneInfo"/> object.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromTimeZoneInfo(TimeZoneInfo timeZoneInfo)
        {
            return TZConvert.KnownIanaTimeZoneNames.Contains(timeZoneInfo.Id)
                ? FromIanaTimeZoneName(timeZoneInfo.Id)
                : FromWindowsTimeZoneId(timeZoneInfo.Id);
        }

        /// <summary>
        /// Generates a POSIX time zone string from a <see cref="TimeZoneInfo"/> object, for the given year.
        /// Note - only uses only the <see cref="TimeZoneInfo.Id"/> property from the object.
        /// </summary>
        /// <param name="timeZoneInfo">The <see cref="TimeZoneInfo"/> object.</param>
        /// <param name="year">The reference year.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromTimeZoneInfo(TimeZoneInfo timeZoneInfo, int year)
        {
            return TZConvert.KnownIanaTimeZoneNames.Contains(timeZoneInfo.Id)
                ? FromIanaTimeZoneName(timeZoneInfo.Id, year)
                : FromWindowsTimeZoneId(timeZoneInfo.Id, year);
        }

        /// <summary>
        /// Generates a POSIX time zone string from a Windows time zone ID, for the current year.
        /// </summary>
        /// <param name="timeZoneId">The Windows time zone ID.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromWindowsTimeZoneId(string timeZoneId)
        {
            string tzName = TZConvert.WindowsToIana(timeZoneId);
            return FromIanaTimeZoneName(tzName);
        }

        /// <summary>
        /// Generates a POSIX time zone string from a Windows time zone ID, for the given year.
        /// </summary>
        /// <param name="timeZoneId">The Windows time zone ID.</param>
        /// <param name="year">The reference year.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromWindowsTimeZoneId(string timeZoneId, int year)
        {
            string tzName = TZConvert.WindowsToIana(timeZoneId);
            return FromIanaTimeZoneName(tzName, year);
        }

        /// <summary>
        /// Generates a POSIX time zone string from an IANA time zone name, for the current year.
        /// </summary>
        /// <param name="timeZoneName">The IANA time zone name.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromIanaTimeZoneName(string timeZoneName)
        {
            var tz = DateTimeZoneProviders.Tzdb[timeZoneName];
            var today = SystemClock.Instance.InZone(tz).GetCurrentDate();
            return FromIanaTimeZoneName(timeZoneName, today.Year);
        }

        /// <summary>
        /// Generates a POSIX time zone string from an IANA time zone name, for the given year.
        /// </summary>
        /// <param name="timeZoneName">The IANA time zone name.</param>
        /// <param name="year">The reference year.</param>
        /// <returns>A POSIX time zone string.</returns>
        public static string FromIanaTimeZoneName(string timeZoneName, int year)
        {
            var tz = DateTimeZoneProviders.Tzdb[timeZoneName];

            var jan = new LocalDate(year, 1, 1).AtStartOfDayInZone(tz);
            var jun = new LocalDate(year, 6, 1).AtStartOfDayInZone(tz);

            var janInterval = tz.GetZoneInterval(jan.ToInstant());
            var junInterval = tz.GetZoneInterval(jun.ToInstant());

            var stdInterval = janInterval.Savings == Offset.Zero
                ? janInterval
                : junInterval;

            var dltInterval = janInterval.Savings != Offset.Zero
                ? janInterval
                : junInterval.Savings != Offset.Zero
                    ? junInterval
                    : null;

            var sb = new StringBuilder();

            var stdAbbreviation = GetPosixAbbreviation(stdInterval.Name);
            sb.Append(stdAbbreviation);

            var stdOffsetString = GetPosixOffsetString(stdInterval.WallOffset);
            sb.Append(stdOffsetString);

            if (dltInterval != null)
            {
                var dltAbbreviation = GetPosixAbbreviation(dltInterval.Name);
                sb.Append(dltAbbreviation);

                if (dltInterval.Savings != Offset.FromHours(1))
                {
                    var dltOffsetString = GetPosixOffsetString(dltInterval.WallOffset);
                    sb.Append(dltOffsetString);
                }

                var stdTransitionString = GetPosixTransitionString(stdInterval);
                sb.Append("," + stdTransitionString);

                var dltTransitionString = GetPosixTransitionString(dltInterval);
                sb.Append("," + dltTransitionString);
            }

            return sb.ToString();
        }

        private static string GetPosixAbbreviation(string abbreviation)
        {
            return abbreviation.IndexOfAny(new[] { '+', '-' }) != -1 ? "<" + abbreviation + ">" : abbreviation;
        }

        private static string GetPosixOffsetString(Offset offset)
        {
            var negated = -offset;
            return negated.ToString("-H:mm", CultureInfo.InvariantCulture).Replace(":00", "");
        }

        private static string GetPosixTransitionString(ZoneInterval interval)
        {
            if (!interval.HasEnd) return "J365/25";

            var transition = interval.IsoLocalEnd;
            var datePart = $"M{transition.Month}.{(transition.Day - 1) / 7 + 1}.{(int)transition.DayOfWeek.ToDayOfWeek()}";

            if (transition.TimeOfDay == new LocalTime(2, 0)) return datePart;
            if (transition.Minute == 0 && transition.Second == 0) return $"{datePart}/{transition.Hour}";
            if (transition.Second == 0) return $"{datePart}/{transition.ToString("H:mm", CultureInfo.InvariantCulture)}";
            return $"{datePart}/{transition.ToString("H:mm:ss", CultureInfo.InvariantCulture)}";
        }
    }
}
