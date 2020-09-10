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
            DateTimeZone tz = DateTimeZoneProviders.Tzdb[timeZoneName];
            LocalDate today = SystemClock.Instance.InZone(tz).GetCurrentDate();
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
            DateTimeZone tz = DateTimeZoneProviders.Tzdb[timeZoneName];

            ZonedDateTime jan = new LocalDate(year, 1, 1).AtStartOfDayInZone(tz);
            ZonedDateTime jul = new LocalDate(year, 7, 1).AtStartOfDayInZone(tz);

            ZoneInterval janInterval = tz.GetZoneInterval(jan.ToInstant());
            ZoneInterval julInterval = tz.GetZoneInterval(jul.ToInstant());

            ZoneInterval stdInterval = janInterval.Savings == Offset.Zero
                ? janInterval
                : julInterval;

            ZoneInterval dltInterval = janInterval.Savings != Offset.Zero
                ? janInterval
                : julInterval.Savings != Offset.Zero
                    ? julInterval
                    : null;

            var sb = new StringBuilder();

            string stdAbbreviation = GetPosixAbbreviation(stdInterval.Name);
            sb.Append(stdAbbreviation);

            string stdOffsetString = GetPosixOffsetString(stdInterval.WallOffset);
            sb.Append(stdOffsetString);

            if (dltInterval != null)
            {
                string dltAbbreviation = GetPosixAbbreviation(dltInterval.Name);
                sb.Append(dltAbbreviation);

                if (dltInterval.Savings != Offset.FromHours(1))
                {
                    string dltOffsetString = GetPosixOffsetString(dltInterval.WallOffset);
                    sb.Append(dltOffsetString);
                }

                string stdTransitionString = GetPosixTransitionString(stdInterval, tz);
                sb.Append("," + stdTransitionString);

                string dltTransitionString = GetPosixTransitionString(dltInterval, tz);
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
            Offset negated = -offset;
            return negated.ToString("-H:mm", CultureInfo.InvariantCulture).Replace(":00", "");
        }

        private static string GetPosixTransitionString(ZoneInterval interval, DateTimeZone tz)
        {
            if (!interval.HasEnd) return "J365/25";

            LocalDateTime transition = interval.IsoLocalEnd;
            int transitionOccurrence = (transition.Day - 1) / 7 + 1;

            // return "last occurrence" (5) when appropriate
            if (transitionOccurrence == 4)
            {
                for (var i = 1; i <= 7; i++)
                {
                    var futureInstant = interval.IsoLocalEnd.PlusYears(i).InZoneLeniently(tz).ToInstant();
                    ZoneInterval futureInterval = tz.GetZoneInterval(futureInstant);
                    int occurrence = (futureInterval.IsoLocalEnd.Day - 1) / 7 + 1;
                    if (occurrence < 4)
                    {
                        transitionOccurrence = 4;
                        break;
                    }

                    if (occurrence == 5)
                    {
                        transitionOccurrence = 5;
                    }
                }
            }

            var datePart = $"M{transition.Month}.{transitionOccurrence}.{(int)transition.DayOfWeek.ToDayOfWeek()}";

            if (transition.TimeOfDay == new LocalTime(2, 0)) return datePart;
            if (transition.Minute == 0 && transition.Second == 0) return $"{datePart}/{transition.Hour}";
            if (transition.Second == 0) return $"{datePart}/{transition.ToString("H:mm", CultureInfo.InvariantCulture)}";
            return $"{datePart}/{transition.ToString("H:mm:ss", CultureInfo.InvariantCulture)}";
        }
    }
}
