using System.Reflection;
using System.Text.RegularExpressions;

namespace SentinelCore.Identity;

public sealed partial record SentinelIdentity
{
    public SentinelIdentity(string internalName, string displayName, string author, SentinelVersion version)
    {
        if (string.IsNullOrWhiteSpace(internalName) || !InternalNamePattern().IsMatch(internalName))
            throw new ArgumentException("Internal name must start with a letter and contain only letters or digits.", nameof(internalName));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        InternalName = internalName;
        DisplayName = displayName.Trim();
        Author = author.Trim();
        Version = version;
    }

    public string InternalName { get; }
    public string DisplayName { get; }
    public string Author { get; }
    public SentinelVersion Version { get; }

    public string DiagnosticPrefix => $"[{DisplayName} {Version}]";

    public static SentinelIdentity FromAssembly(
        string internalName,
        string displayName,
        string author,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var version = assembly.GetName().Version ?? new Version(0, 0, 0, 0);
        return new SentinelIdentity(
            internalName,
            displayName,
            author,
            new SentinelVersion(
                Math.Max(0, version.Major),
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build),
                Math.Max(0, version.Revision)));
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex InternalNamePattern();
}

