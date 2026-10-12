using System.IO.Compression;
using System.Resources;

namespace TimeZoneConverter;

internal static class DataBundle
{
    internal const string ResourceName = "TimeZoneConverter.Data.TimeZoneData.zip";
    internal const int MaximumSize = 4 * 1024 * 1024;
    private static readonly string[] Names = ["Mapping.csv", "Aliases.csv", "RailsMapping.csv", "Territories.csv"];

    internal static Dictionary<string, string[]> ReadEmbedded()
    {
        using var stream = typeof(DataBundle).Assembly.GetManifestResourceStream(ResourceName) ??
                           throw new MissingManifestResourceException(ResourceName);
        return Read(stream);
    }

    internal static Dictionary<string, string[]> Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            if (entries.ContainsKey(entry.FullName))
                throw new InvalidDataException($"Duplicate bundle entry: {entry.FullName}.");
            entries.Add(entry.FullName, entry);
        }
        if (!entries.TryGetValue("manifest.txt", out var manifest) || manifest.Length > 128)
        {
            throw new InvalidDataException("The bundle requires a manifest.txt entry.");
        }

        using (var input = manifest.Open())
        using (var buffer = Buffer(input, 128))
        using (var reader = new StreamReader(buffer))
        {
            if (reader.ReadToEnd().Trim() != "TimeZoneConverter/1")
            {
                throw new InvalidDataException("Unsupported time zone data bundle format.");
            }
        }

        var result = new Dictionary<string, string[]>();
        foreach (var name in Names)
        {
            if (!entries.TryGetValue(name, out var entry) || entry.Length > MaximumSize)
            {
                throw new InvalidDataException($"The bundle requires {name}, no larger than {MaximumSize} bytes.");
            }

            using var input = entry.Open();
            using var buffer = Buffer(input);
            using var reader = new StreamReader(buffer, new System.Text.UTF8Encoding(false, true));
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                var fields = line.Split(',');
                var count = name == "Mapping.csv" ? 3 : 2;
                if (fields.Length != count || fields.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidDataException($"Invalid {name} data at line {lines.Count + 1}.");
                }
                lines.Add(line);
            }

            if (lines.Count == 0)
            {
                throw new InvalidDataException($"{name} must not be empty.");
            }
            result.Add(name, lines.ToArray());
        }
        return result;
    }

    internal static MemoryStream Buffer(Stream source, int maximumSize = MaximumSize)
    {
        ValidateStream(source);
        var destination = new MemoryStream();
        try
        {
            var buffer = new byte[81920];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                Append(destination, buffer, count, maximumSize);
            }
            destination.Position = 0;
            return destination;
        }
        catch
        {
            destination.Dispose();
            throw;
        }
    }

    private static void ValidateStream(Stream source)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source == null) throw new ArgumentNullException(nameof(source));
#endif
    }

    private static void Append(MemoryStream destination, byte[] buffer, int count, int maximumSize)
    {
        if (destination.Length + count > maximumSize)
            throw new InvalidDataException($"Data exceeds the {maximumSize} byte size limit.");
        destination.Write(buffer, 0, count);
    }

    internal static async Task<MemoryStream> BufferAsync(Stream source, CancellationToken cancellationToken)
    {
        ValidateStream(source);
        var destination = new MemoryStream();
        try
        {
            var buffer = new byte[81920];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count =
#if NET8_0_OR_GREATER
                    await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
                    await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#endif
                if (count == 0) break;
                Append(destination, buffer, count, MaximumSize);
            }
            destination.Position = 0;
            return destination;
        }
        catch
        {
            destination.Dispose();
            throw;
        }
    }
}
