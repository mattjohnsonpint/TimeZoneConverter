TimeZoneConverter  [![NuGet Version](https://img.shields.io/nuget/v/TimeZoneConverter.svg?style=flat)](https://www.nuget.org/packages/TimeZoneConverter/) 
=================

Lightweight library to convert quickly between IANA, Windows, and Rails time zone names.

## Installation

```powershell
PM> Install-Package TimeZoneConverter
```

This library is targeting .NET Standard 2.0, 1.3, 1.1, and .NET 4.5.
See the [.NET Standard Platform Support Matrix][1] for further details.

## Notes

This library uses a combination of data sources to achieve its goals:

- The [Unicode CLDR][2] project
- The [IANA time zone][3] data
- Microsoft Windows [time zone updates][4]
- The `MAPPING` data from `ActiveSupport::TimeZone` in [the Rails source code][5].
- The author's best-informed knowledge and opinions

Usually, the latter is reserved for edge cases, and for newly-introduced zones that may
or may not have been published to official sources yet.

**Important:** Since this data can change whenever new time zones are introduced from any of these sources,
it is recommended that you always use the most current revision, and check for updates regularly.

## Example Usage

Convert an IANA time zone name to the best fitting Windows time zone ID.

```csharp
string tz = TZConvert.IanaToWindows("America/New_York");
// Result:  "Eastern Standard Time"
```

Convert a Windows time zone name to the best fitting IANA time zone name.

```csharp
string tz = TZConvert.WindowsToIana("Eastern Standard Time");
// result:  "America/New_York"
```

Convert a Windows time zone name to the best fitting IANA time zone name, with regard to a specific country.

```csharp
string tz = TZConvert.WindowsToIana("Eastern Standard Time", "CA");
// result:  "America/Toronto"
```

Get a `TimeZoneInfo` object from .NET Core, regardless of what OS you are running on:  
*Helps with .NET CoreFX issue [#11897][6]*  
***This function is only available for .NET Standard 1.3+ or full .NET Framework targets***

```csharp
// Either of these will work on any platform:
TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo("America/New_York");
```

Convert a Rails time zone name to the best fitting IANA time zone name.

```csharp
string tz = TZConvert.RailsToIana("Mexico City");
// result:  "America/Mexico_City"
```

Convert a Rails time zone name to the best fitting Windows time zone ID.

```csharp
string tz = TZConvert.RailsToWindows("Mexico City");
// result:  "Central Standard Time (Mexico)"
```

Convert an IANA time zone name to one or more Rails time zone names.

```csharp
IList<string> tz = TZConvert.IanaToRails("America/Mexico_City");
// Result:  { "Guadalajara", "Mexico City" }
```

Convert a Windows time zone ID to one or more Rails time zone names.

```csharp
IList<string> tz = TZConvert.WindowsToRails("Central Standard Time (Mexico)");
// Result:  { "Guadalajara", "Mexico City" }
```

## License

This library is provided free of charge, under the terms of the [MIT license][7].


[1]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[2]: http://cldr.unicode.org
[3]: http://iana.org/time-zones
[4]: https://blogs.technet.microsoft.com/dst2007
[5]: https://github.com/rails/rails/blob/master/activesupport/lib/active_support/values/time_zone.rb
[6]: https://github.com/dotnet/corefx/issues/11897
[7]: https://raw.githubusercontent.com/mj1856/TimeZoneConverter/master/LICENSE.txt
