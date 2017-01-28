TimeZoneConverter
=================

Lightweight library to convert quickly between Windows time zone IDs and IANA Time Zone names.

## Installation

```powershell
PM> Install-Package TimeZoneConverter
```

This library is targeting .NET Standard 1.1.  
See the [.NET Standard Platform Support Matrix][1] for further details.

## Example Usage

Convert an IANA time zone name to the best fitting Windows time zone ID.
```csharp
string tz = TZConvert.IanaToWindows("America/Los_Angeles");
// Result:  "Eastern Standard Time"
```

Convert a Windows time zone name to the best fitting IANA time zone ID.
```csharp
string tz = TZConvert.WindowsToIana("Eastern Standard Time");
// result:  "America/New_York"
```

Convert a Windows time zone name to the best fitting IANA time zone ID, with regard to a specific country.
```csharp
string tz = TZConvert.WindowsToIana("Eastern Standard Time", "CA");
// result:  "America/Toronto"
```

## Notes

This library uses a combination of data sources to achieve its goals:

- The [Unicode CLDR][2] project
- The [IANA time zone][3] data
- Microsoft Windows [time zone updates][4]
- The author's best-informed knowledge and opinions

Usually, the latter is reserved for edge cases, and for newly-introduced zones that may
or may not have been published to official sources yet.

**Important:** Since this data can change whenver new time zones are introduced from any of these sources,
it is recommended that you always use the most current revision, and check for updates regularly.

## License

This library is provided free of charge, under the terms of the [MIT license][5].


[1]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[2]: http://cldr.unicode.org
[3]: http://iana.org/time-zones
[4]: https://blogs.technet.microsoft.com/dst2007
[5]: https://raw.githubusercontent.com/mj1856/TimeZoneConverter/master/LICENSE.txt