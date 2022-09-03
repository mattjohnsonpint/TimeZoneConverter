## TimeZoneConverter  [![NuGet Version](https://img.shields.io/nuget/v/TimeZoneConverter.svg?style=flat)](https://www.nuget.org/packages/TimeZoneConverter/)

## TimeZoneConverter.Posix  [![NuGet Version](https://img.shields.io/nuget/v/TimeZoneConverter.Posix.svg?style=flat)](https://www.nuget.org/packages/TimeZoneConverter.Posix/)

--------------------------------

- TimeZoneConverter is a lightweight library to convert quickly between IANA, Windows, and Rails time zone names.
- TimeZoneConverter.Posix adds support for generating POSIX time zone strings, which are useful in certain scenarios such as IoT.

## TimeZoneConverter Installation

```powershell
PM> Install-Package TimeZoneConverter
```

As of version 6.0.0, *TimeZoneConverter* works with all of the following:

- .NET 5 or greater
- .NET Core 2.0 or greater
- .NET Framework 4.6.2 and greater

Note that .NET Framework versions less than 4.6.2 are no longer supported.

#### Important note on .NET 6+

.NET 6 has built-in support for IANA and Windows time zones in a cross-platform manner, somewhat reducing the need for this library.
It relies on [.NET's ICU integration](https://docs.microsoft.com/dotnet/core/extensions/globalization-icu) to perform this functionality.
See [the .NET blog](https://devblogs.microsoft.com/dotnet/date-time-and-time-zone-enhancements-in-net-6/#time-zone-conversion-apis) for details.

Restated, if you are targeting only .NET 6 (or higher), and you have either platform-provided or App-local ICU enabled, you *might* not need to use this library.
However, it is indeed still supported, and many have found it continues to be useful in certain environments or on specific platforms.

TimeZoneConverter has no external data dependencies at runtime.  All of the data it needs is embedded in the library itself.

#### Note on OS Data Dependencies

Some functions in TimeZoneConverter, such as `TZConvert.GetTimeZoneInfo` rely on the underlying `TimeZoneInfo` object having access to
time zone data of the operating system.  On Windows, this data comes from the registry and is maintained via Windows Updates.

On OSX and Linux, this data comes from a distribution of the [IANA time zone database](https://www.iana.org/time-zones),  usually via the `tzdata` package.  If your environment does not have the `tzdata` package installed, you will need to install it for `TZConvert.GetTimeZoneInfo` to work correctly.

For example, the Alpine Linux Docker images for .NET Core no longer ship with `tzdata`.  See [dotnet/dotnet-docker#1366](https://github.com/dotnet/dotnet-docker/issues/1366) for instructions on how to add it to your Docker images.

## TimeZoneConverter.Posix Installation

```powershell
PM> Install-Package TimeZoneConverter.Posix
```

This is a separate helper library that is maintained in the same repository.  You only need it if you require support for POSIX time zones.

As of version 4.0.0, *TimeZoneConverter.Posix* works with all of the following:

- .NET 5 or greater
- .NET Core 2.0 or greater
- .NET Framework 4.6.2 and greater

Note that .NET Framework versions less than 4.6.2 are no longer supported.

Also note that `TimeZoneConverter.Posix` takes a dependency on both `TimeZoneConverter` and [Noda Time][2].

## Notes

This library uses a combination of data sources to achieve its goals:

- The [Unicode CLDR][3] project
- The [IANA time zone][4] data
- Microsoft Windows [time zone updates][5]
- The `MAPPING` data from `ActiveSupport::TimeZone` in [the Rails source code][6].
- The author's best-informed knowledge and opinions

Usually, the latter is reserved for edge cases, and for newly-introduced zones that may
or may not have been published to official sources yet.

**Important:** Since this data can change whenever new time zones are introduced from any of these sources,
it is recommended that you always use the most current revision, and check for updates regularly.

Additionally, this library does not attempt to determine if the time zone IDs provided are actually present on the computer where the code is running.  It is assumed that the computer is kept current with time zone updates.

For example, if one attempts to convert `Africa/Khartoum` to a Windows time zone ID, they will get `Sudan Standard Time`.  If it is then used on a Windows computer that does not yet have [`KB4051956`][7] installed (which created this time zone), they will likely get a `TimeZoneNotFoundException`.

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

Generate a POSIX time zone string from a Windows time zone ID.

*Requires `TimeZoneConverter.Posix`*

```csharp
string posix = PosixTimeZone.FromWindowsTimeZoneId("Eastern Standard Time");
// Result: "EST5EDT,M3.2.0,M11.1.0"
```

Generate a POSIX time zone string from an IANA time zone name.

*Requires `TimeZoneConverter.Posix`*

```csharp
string posix = PosixTimeZone.FromIanaTimeZoneName("Australia/Sydney");
// Result: "AEST-10AEDT,M10.1.0,M4.1.0/3"
```

Generate a POSIX time zone string from a `TimeZoneInfo` object.

*Requires `TimeZoneConverter.Posix`*

```csharp
string posix = PosixTimeZone.FromTimeZoneInfo(TimeZoneInfo.Local);
```

## Extras

There are a few additional helpers you may find useful.

- These properties provide lists of the various types of time zones known to this library:
  - `TZConvert.KnownIanaTimeZoneNames`
  - `TZConvert.KnownWindowsTimeZoneIds`
  - `TZConvert.KnownRailsTimeZoneNames`

- If you need a list of time zones that are applicable in a given region, you can use:
  - `TZConvert.GetIanaTimeZoneNamesByTerritory()`

## License

This library is provided free of charge, under the terms of the [MIT license][8].


[1]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
[2]: https://nodatime.org
[3]: https://cldr.unicode.org
[4]: https://iana.org/time-zones
[5]: https://aka.ms/dstblog
[6]: https://github.com/rails/rails/blob/master/activesupport/lib/active_support/values/time_zone.rb
[7]: https://support.microsoft.com/en-us/help/4051956/time-zone-and-dst-changes-in-windows-for-northern-cyprus-sudan-and-ton
[8]: https://github.com/mattjohnsonpint/TimeZoneConverter/blob/main/LICENSE.txt
