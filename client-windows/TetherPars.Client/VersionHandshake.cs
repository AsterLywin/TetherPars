namespace TetherPars.Client;

/// <summary>
/// Task 7: version handshake over the shared text protocol.
/// Both sides exchange `HELLO version=X`; equal versions connect,
/// anything else is a clear "Version Mismatched" error (never a crash).
/// Mirrors Android `VersionCheck` (same rules, same messages).
/// </summary>
public static class VersionHandshake
{
    public const string CurrentVersion = "0.1.0";
    public const string MismatchMessage = "Version Mismatched";

    public static bool IsCompatible(string? phone, string? pc) =>
        !string.IsNullOrWhiteSpace(phone)
        && !string.IsNullOrWhiteSpace(pc)
        && string.Equals(phone.Trim(), pc.Trim(), StringComparison.Ordinal);

    public static string BuildHello(string version) => $"HELLO version={version?.Trim()}";

    public static bool TryParseHello(string? line, out string version)
    {
        version = "";
        if (string.IsNullOrWhiteSpace(line)) return false;
        const string prefix = "HELLO version=";
        var t = line.Trim();
        if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        version = t.Substring(prefix.Length).Trim();
        return version.Length > 0;
    }

    /// <summary>
    /// Validates a phone HELLO line against this client's version.
    /// Returns (ok, message): ok=false carries the user-facing mismatch text.
    /// </summary>
    public static (bool Ok, string Message) CheckPhoneHello(string? helloLine, string pcVersion = CurrentVersion)
    {
        if (!TryParseHello(helloLine, out var phoneVersion))
            return (false, MismatchMessage);
        return IsCompatible(phoneVersion, pcVersion)
            ? (true, "OK")
            : (false, $"{MismatchMessage}: phone={phoneVersion} pc={pcVersion}");
    }
}
