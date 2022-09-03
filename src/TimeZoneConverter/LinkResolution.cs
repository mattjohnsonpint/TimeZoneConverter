namespace TimeZoneConverter;

/// <summary>
/// Provides options for how links are resolved when returning results from
/// converting from a Windows zone to an IANA zone.
/// </summary>
public enum LinkResolution
{
    /// <summary>
    /// Return an IANA canonical value for the conversion, except for the case
    /// when an alias is more regionally appropriate for the conversion.
    /// </summary>
    /// <remarks>
    /// Ex: "India Standard Time" => "Asia/Kolkata"
    /// "SA Western Standard Time" (AG) => "America/Antigua"
    /// </remarks>
    Default = 0,

    /// <summary>
    /// Always return an IANA canonical value.
    /// </summary>
    /// <remarks>
    /// This was the default behavior prior to version 6.0.
    /// Ex: "India Standard Time" => "Asia/Kolkata"
    /// "SA Western Standard Time" (AG) => "America/Puerto_Rico"
    /// </remarks>
    Canonical,

    /// <summary>
    /// Always return the original value from the CLDR mapping, without attempting
    /// to resolve it to the IANA canonical form.
    /// </summary>
    /// <remarks>
    /// Ex: "India Standard Time" => "Asia/Calcutta"
    /// "SA Western Standard Time" (AG) => "America/Antigua"
    /// </remarks>
    Original
}
