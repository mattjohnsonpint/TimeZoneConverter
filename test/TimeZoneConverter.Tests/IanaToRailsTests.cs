using Xunit.Abstractions;

namespace TimeZoneConverter.Tests;

public class IanaToRailsTests
{
    private readonly ITestOutputHelper _output;

    public IanaToRailsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static IEnumerable<string> UnmappableZones => new[]
    {
        "Africa/Abidjan",
        "Africa/Accra",
        "Africa/Bamako",
        "Africa/Bangui",
        "Africa/Banjul",
        "Africa/Bissau",
        "Africa/Brazzaville",
        "Africa/Conakry",
        "Africa/Dakar",
        "Africa/Douala",
        "Africa/Freetown",
        "Africa/Juba",
        "Africa/Khartoum",
        "Africa/Kinshasa",
        "Africa/Lagos",
        "Africa/Libreville",
        "Africa/Lome",
        "Africa/Luanda",
        "Africa/Malabo",
        "Africa/Ndjamena",
        "Africa/Niamey",
        "Africa/Nouakchott",
        "Africa/Ouagadougou",
        "Africa/Porto-Novo",
        "Africa/Sao_Tome",
        "Africa/Timbuktu",
        "Africa/Tripoli",
        "Africa/Tunis",
        "Africa/Windhoek",
        "America/Adak",
        "America/Araguaina",
        "America/Asuncion",
        "America/Atka",
        "America/Bahia",
        "America/Belem",
        "America/Campo_Grande",
        "America/Cancun",
        "America/Cayenne",
        "America/Coyhaique",
        "America/Cuiaba",
        "America/Danmarkshavn",
        "America/Fortaleza",
        "America/Grand_Turk",
        "America/Havana",
        "America/Maceio",
        "America/Miquelon",
        "America/Noronha",
        "America/Paramaribo",
        "America/Port-au-Prince",
        "America/Punta_Arenas",
        "America/Recife",
        "America/Santarem",
        "Antarctica/Palmer",
        "Antarctica/Rothera",
        "Antarctica/Troll",
        "Antarctica/Vostok",
        "Asia/Amman",
        "Asia/Barnaul",
        "Asia/Beirut",
        "Asia/Bishkek",
        "Asia/Chita",
        "Asia/Damascus",
        "Asia/Gaza",
        "Asia/Hebron",
        "Asia/Hovd",
        "Asia/Omsk",
        "Asia/Pyongyang",
        "Asia/Qyzylorda",
        "Asia/Sakhalin",
        "Asia/Tomsk",
        "Atlantic/Reykjavik",
        "Atlantic/St_Helena",
        "Atlantic/Stanley",
        "Australia/Eucla",
        "Australia/LHI",
        "Australia/Lord_Howe",
        "Brazil/DeNoronha",
        "Chile/EasterIsland",
        "Cuba",
        "Etc/GMT-1",
        "Etc/GMT-12",
        "Etc/GMT-13",
        "Etc/GMT-14",
        "Etc/GMT-6",
        "Etc/GMT+11",
        "Etc/GMT+2",
        "Etc/GMT+3",
        "Etc/GMT+8",
        "Etc/GMT+9",
        "Europe/Astrakhan",
        "Europe/Chisinau",
        "Europe/Saratov",
        "Europe/Tiraspol",
        "Europe/Ulyanovsk",
        "Iceland",
        "Indian/Chagos",
        "Indian/Mauritius",
        "Libya",
        "Pacific/Bougainville",
        "Pacific/Easter",
        "Pacific/Enderbury",
        "Pacific/Funafuti",
        "Pacific/Gambier",
        "Pacific/Kanton",
        "Pacific/Kiritimati",
        "Pacific/Marquesas",
        "Pacific/Nauru",
        "Pacific/Niue",
        "Pacific/Norfolk",
        "Pacific/Pitcairn",
        "US/Aleutian"
    };

    [Fact]
    public void Can_Convert_Iana_Zones_To_Rails_Zones()
    {
        var errors = new List<string>();
        var ianaZones = TZConvert.KnownIanaTimeZoneNames.OrderBy(x => x);

        foreach (var ianaZone in ianaZones.Except(UnmappableZones))
        {
            if (TZConvert.TryIanaToRails(ianaZone, out var railsZones))
            {
                Assert.NotNull(railsZones);
                Assert.NotEmpty(railsZones);
            }
            else
            {
                errors.Add(ianaZone);
            }
        }

        var errorsCount = errors.Count;
        if (errorsCount > 0)
        {
            _output.WriteLine("Failed to convert:\n");
            _output.WriteLine(string.Join(",\n", errors.Select(x => $"\"{x}\"")));
        }

        Assert.Equal(0, errorsCount);
    }
}
