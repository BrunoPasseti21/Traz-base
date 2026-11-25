using SupraKit.App;

namespace SupraKit.Tests;

[TestClass]
public sealed class VpnConnectionRulesTests
{
    [TestMethod]
    public void NormalizeNames_RemovesEmptyEntries()
    {
        var names = VpnConnection.NormalizeNames(["  ", "", "VPN Empresa", null!]);

        CollectionAssert.AreEqual(new[] { "VPN Empresa" }, names.ToArray());
    }

    [TestMethod]
    public void NormalizeNames_DeduplicatesIgnoringCase()
    {
        var names = VpnConnection.NormalizeNames(["VPN Empresa", "vpn empresa", "VPN EMPRESA", "Outra VPN"]);

        CollectionAssert.AreEqual(new[] { "Outra VPN", "VPN Empresa" }, names.ToArray());
    }

    [TestMethod]
    public void NormalizeNames_SortsByOrdinalIgnoreCase()
    {
        var names = VpnConnection.NormalizeNames(["Zulu", "alfa", "Beta"]);

        CollectionAssert.AreEqual(new[] { "alfa", "Beta", "Zulu" }, names.ToArray());
    }

    [TestMethod]
    public void ConnectionNameMatches_IsCaseInsensitive()
    {
        var active = new[] { "Minha VPN", "Outra VPN" };

        Assert.IsTrue(VpnConnection.ConnectionNameMatches(active, "minha vpn"));
        Assert.IsTrue(VpnConnection.ConnectionNameMatches(active, "OUTRA VPN"));
        Assert.IsFalse(VpnConnection.ConnectionNameMatches(active, "Terceira VPN"));
    }

    [TestMethod]
    public void ConnectionNameMatches_HandlesNullAndBlankInput()
    {
        var active = new[] { "Minha VPN" };

        Assert.IsFalse(VpnConnection.ConnectionNameMatches(null, "Minha VPN"));
        Assert.IsFalse(VpnConnection.ConnectionNameMatches(active, null));
        Assert.IsFalse(VpnConnection.ConnectionNameMatches(active, "   "));
    }

    [TestMethod]
    public void ConnectionNameMatches_TrimsEntriesFromBothSides()
    {
        var active = new[] { "  Minha VPN  " };

        Assert.IsTrue(VpnConnection.ConnectionNameMatches(active, " Minha VPN "));
    }
}
