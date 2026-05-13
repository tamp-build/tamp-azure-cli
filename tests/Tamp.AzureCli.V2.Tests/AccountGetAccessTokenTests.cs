using System;
using System.Linq;
using Tamp;
using Tamp.AzureCli.V2;
using Xunit;

namespace Tamp.AzureCli.V2.Tests;

/// <summary>
/// Plan-shape tests for the new Account.GetAccessToken surface (TAM-178/strata-scott
/// 2026-05-13). The GetAccessTokenAsSecret helper actually spawns az, so it's covered
/// by integration tests rather than unit tests; here we verify the plan a Target would
/// dispatch and the settings validation rules.
/// </summary>
public sealed class AccountGetAccessTokenTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/az"));

    private static int IndexOf(System.Collections.Generic.IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    [Fact]
    public void GetAccessToken_Bare_Has_Verb_Tokens()
    {
        var plan = AzureCli.Account.GetAccessToken(FakeTool());
        Assert.Equal(new[] { "account", "get-access-token" }, plan.Arguments.Take(2));
    }

    [Fact]
    public void GetAccessToken_With_Resource()
    {
        var plan = AzureCli.Account.GetAccessToken(FakeTool(), s => s
            .SetResource("https://vault.azure.net"));
        Assert.Equal("https://vault.azure.net",
            plan.Arguments[IndexOf(plan.Arguments, "--resource") + 1]);
    }

    [Fact]
    public void GetAccessToken_With_Tenant_And_Subscription()
    {
        var plan = AzureCli.Account.GetAccessToken(FakeTool(), s => s
            .SetResource("https://management.azure.com")
            .SetTenant("contoso.onmicrosoft.com")
            .SetSubscription("00000000-0000-0000-0000-000000000000"));
        Assert.Equal("contoso.onmicrosoft.com",
            plan.Arguments[IndexOf(plan.Arguments, "--tenant") + 1]);
        Assert.Equal("00000000-0000-0000-0000-000000000000",
            plan.Arguments[IndexOf(plan.Arguments, "--subscription") + 1]);
    }

    [Fact]
    public void GetAccessToken_With_Scope()
    {
        var plan = AzureCli.Account.GetAccessToken(FakeTool(), s => s
            .SetResource("https://graph.microsoft.com")
            .SetScope("https://graph.microsoft.com/.default"));
        Assert.Equal("https://graph.microsoft.com/.default",
            plan.Arguments[IndexOf(plan.Arguments, "--scope") + 1]);
    }

    [Fact]
    public void GetAccessToken_ObjectInit_Round_Trips()
    {
        var plan = AzureCli.Account.GetAccessToken(FakeTool(), new AzureCliAccountGetAccessTokenSettings
        {
            Resource = "https://vault.azure.net",
            Tenant = "tenant",
            Output = "json",
        });
        Assert.Equal("https://vault.azure.net",
            plan.Arguments[IndexOf(plan.Arguments, "--resource") + 1]);
        Assert.Contains("--output", plan.Arguments);
        Assert.Equal("json", plan.Arguments[IndexOf(plan.Arguments, "--output") + 1]);
    }

    [Fact]
    public void GetAccessTokenAsSecret_Rejects_Null_Tool()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AzureCli.Account.GetAccessTokenAsSecret(null!));
    }

    [Fact]
    public void GetAccessTokenAsSecret_Rejects_Empty_Resource()
    {
        Assert.Throws<ArgumentException>(() =>
            AzureCli.Account.GetAccessTokenAsSecret(FakeTool(), resource: ""));
    }
}
