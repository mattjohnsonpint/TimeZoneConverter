using System.Diagnostics.CodeAnalysis;

namespace TimeZoneConverter;

internal static class CustomTimeZoneFactory
{
    private const string TrollTimeZoneId = "Antarctica/Troll";
    private static readonly Lazy<TimeZoneInfo> TrollTimeZone = new(CreateTrollTimeZone);

    public static bool TryGetTimeZoneInfo(string timeZoneId, [MaybeNullWhen(false)] out TimeZoneInfo timeZoneInfo)
    {
        if (timeZoneId.Equals(TrollTimeZoneId, StringComparison.OrdinalIgnoreCase))
        {
            timeZoneInfo = TrollTimeZone.Value;
            return true;
        }

        timeZoneInfo = null;
        return false;
    }

    private static TimeZoneInfo CreateTrollTimeZone() =>
        TimeZoneInfo.CreateCustomTimeZone(
            id: TrollTimeZoneId,
            baseUtcOffset: TimeSpan.Zero,
            displayName: "(UTC+00:00) Troll Station, Antarctica",
            standardDisplayName: "Greenwich Mean Time",
            daylightDisplayName: "Central European Summer Time",
            adjustmentRules: new[]
            {
                // Like IANA, we will approximate with only UTC and CEST (UTC+2).
                // Handling the CET (UTC+1) period would require generating adjustment rules for each individual year.
                TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                    dateStart: DateTime.MinValue.Date,
                    dateEnd: DateTime.MaxValue.Date,
                    daylightDelta: TimeSpan.FromHours(2), // Two hours DST gap
                    daylightTransitionStart: TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                        timeOfDay: new DateTime(1, 1, 1, 1, 0, 0), // 01:00 local, 01:00 UTC
                        month: 3, // March
                        week: 5, // the last week of the month
                        DayOfWeek.Sunday),
                    daylightTransitionEnd: TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                        timeOfDay: new DateTime(1, 1, 1, 3, 0, 0), // 03:00 local, 01:00 UTC
                        month: 10, // October
                        week: 5, // the last week of the month
                        DayOfWeek.Sunday)
                )
            }
        );
}
