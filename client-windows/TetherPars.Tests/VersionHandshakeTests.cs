using TetherPars.Client;
using Xunit;

namespace TetherPars.Tests;

/// <summary>
/// Task 7 TDD RED: version handshake must accept equal versions
/// and reject mismatches with a clear "Version Mismatched" path (no crash).
/// </summary>
public class VersionHandshakeTests
{
    [Fact]
    public void MatchingVersions_Pass()
    {
        Assert.True(VersionHandshake.IsCompatible("0.1.0", "0.1.0"));
        Assert.False(VersionHandshake.IsCompatible("0.1.0", "0.2.0"));
    }

    [Fact]
    public void HelloMessage_Roundtrips()
    {
        var hello = VersionHandshake.BuildHello("0.1.0");
        Assert.Equal("HELLO version=0.1.0", hello);
        Assert.True(VersionHandshake.TryParseHello(hello, out var version));
        Assert.Equal("0.1.0", version);
    }

    [Fact]
    public void MismatchedVersions_AreIncompatible()
    {
        Assert.False(VersionHandshake.IsCompatible("0.1.0", "0.1.1"));
        Assert.False(VersionHandshake.IsCompatible("", "0.1.0"));
        Assert.False(VersionHandshake.IsCompatible("0.1.0", ""));
    }
}
