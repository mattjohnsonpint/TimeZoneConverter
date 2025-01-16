## TimeZoneConverter.Posix  [![NuGet Version](https://img.shields.io/nuget/v/TimeZoneConverter.Posix.svg?style=flat)](https://www.nuget.org/packages/TimeZoneConverter.Posix/)

--------------------------------

TimeZoneConverter.Posix generates POSIX time zone strings from standard time zone identifiers.  POSIX time zones are useful in certain scenarios where time zone data is not present, such as when working with IoT devices.

It is a separate helper library that is maintained in the same repository as `TimeZoneConverter`.  You only need it if you require support for POSIX time zones.

Note that `TimeZoneConverter.Posix` is dependendent on both [`TimeZoneConverter`][1] and [Noda Time][2] at run time.

## Installation

- Add the `TimeZoneConverter.Posix` NuGet package to your project.
- Import the `TimeZoneConverter.Posix` namespace where needed.

## Compatibility

As of version 5.0.0, TimeZoneConverter.Posix works with all of the following:

- .NET 6 or greater
- .NET Core 2.0 or greater
- .NET Framework 4.6.2 and greater

.NET Framework versions less than 4.6.2 are no longer supported.

## Example Usage

Generate a POSIX time zone string from a Windows time zone ID.

```csharp
string posix = PosixTimeZone.FromWindowsTimeZoneId("Eastern Standard Time");
// Result: "EST5EDT,M3.2.0,M11.1.0"
```

Generate a POSIX time zone string from an IANA time zone name.

```csharp
string posix = PosixTimeZone.FromIanaTimeZoneName("Australia/Sydney");
// Result: "AEST-10AEDT,M10.1.0,M4.1.0/3"
```

Generate a POSIX time zone string from a `TimeZoneInfo` object.

```csharp
string posix = PosixTimeZone.FromTimeZoneInfo(TimeZoneInfo.Local);
```

## License

This library is provided free of charge, under the terms of the [MIT license][3].

[1]: https://github.com/mattjohnsonpint/TimeZoneConverter
[2]: https://nodatime.org
[3]: https://github.com/mattjohnsonpint/TimeZoneConverter/blob/main/LICENSE.txt
