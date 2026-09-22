using System.Globalization;

namespace SentinelCore.Identity;

public readonly record struct SentinelVersion : IComparable<SentinelVersion>
{
    public SentinelVersion(int major, int minor, int patch, int revision)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major);
        ArgumentOutOfRangeException.ThrowIfNegative(minor);
        ArgumentOutOfRangeException.ThrowIfNegative(patch);
        ArgumentOutOfRangeException.ThrowIfNegative(revision);
        Major = major;
        Minor = minor;
        Patch = patch;
        Revision = revision;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public int Revision { get; }

    public static SentinelVersion Parse(string value)
        => TryParse(value, out var parsed)
            ? parsed
            : throw new FormatException($"'{value}' is not a four-component Sentinel version.");

    public static bool TryParse(string? value, out SentinelVersion parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split('.');
        if (parts.Length != 4)
            return false;

        Span<int> numbers = stackalloc int[4];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index])
                || numbers[index] < 0)
            {
                return false;
            }
        }

        parsed = new SentinelVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
        return true;
    }

    public int CompareTo(SentinelVersion other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0) return major;
        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0) return minor;
        var patch = Patch.CompareTo(other.Patch);
        return patch != 0 ? patch : Revision.CompareTo(other.Revision);
    }

    public Version ToSystemVersion() => new(Major, Minor, Patch, Revision);

    public static bool operator <(SentinelVersion left, SentinelVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(SentinelVersion left, SentinelVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(SentinelVersion left, SentinelVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SentinelVersion left, SentinelVersion right) => left.CompareTo(right) >= 0;

    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}.{Revision}");
}
