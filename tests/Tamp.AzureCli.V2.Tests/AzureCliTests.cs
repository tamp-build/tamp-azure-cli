using System.IO;
using Bogus;
using Tamp;
using Xunit;

namespace Tamp.AzureCli.V2.Tests;

public sealed class AzureCliTests
{
    private static Tool FakeTool(string name = "az") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++)
            if (args[i] == value) return i;
        return -1;
    }

    // ---- shape ----

    [Fact]
    public void Every_Verb_Uses_Tool_Path()
    {
        var t = FakeTool();
        Assert.Equal(t.Executable.Value, AzureCli.Version(t).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Logout(t).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Login(t, s => { }).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Rest(t, s => s.SetUri("/sub/x")).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Group.Show(t, s => s.SetName("rg")).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Account.Show(t).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Bicep.Build(t, s => s.SetFile("a.bicep")).Executable);
        Assert.Equal(t.Executable.Value, AzureCli.Raw(t, "acr", "login").Executable);
    }

    // ---- common args ----

    [Fact]
    public void Common_Args_Round_Trip_On_Group_Show()
    {
        var plan = AzureCli.Group.Show(FakeTool(), s => s
            .SetName("rg-strata-test")
            .SetOutput("tsv")
            .SetQuery("name")
            .SetSubscription("00000000-0000-0000-0000-000000000000")
            .SetVerbose()
            .SetDebug()
            .SetOnlyShowErrors());
        var args = plan.Arguments;
        Assert.Equal(["group", "show"], args.Take(2));
        Assert.Contains("--output", args); Assert.Contains("tsv", args);
        Assert.Contains("--query", args); Assert.Contains("name", args);
        Assert.Contains("--subscription", args); Assert.Contains("00000000-0000-0000-0000-000000000000", args);
        Assert.Contains("--verbose", args);
        Assert.Contains("--debug", args);
        Assert.Contains("--only-show-errors", args);
        Assert.Contains("--name", args); Assert.Contains("rg-strata-test", args);
    }

    [Fact]
    public void DisableConnectionVerification_Flows_To_Env_Not_Args()
    {
        var plan = AzureCli.Version(FakeTool(), s => s.SetDisableConnectionVerification());
        Assert.Equal("1", plan.Environment["AZURE_CLI_DISABLE_CONNECTION_VERIFICATION"]);
        Assert.DoesNotContain("AZURE_CLI_DISABLE_CONNECTION_VERIFICATION", plan.Arguments);
    }

    [Fact]
    public void Custom_Env_Vars_Survive_Alongside_DisableConnectionVerification()
    {
        var plan = AzureCli.Version(FakeTool(), s => s
            .SetEnv("SSL_CERT_FILE", "/etc/ssl/certs/zscaler.pem")
            .SetDisableConnectionVerification());
        Assert.Equal("/etc/ssl/certs/zscaler.pem", plan.Environment["SSL_CERT_FILE"]);
        Assert.Equal("1", plan.Environment["AZURE_CLI_DISABLE_CONNECTION_VERIFICATION"]);
    }

    // ---- login ----

    [Fact]
    public void Login_Interactive_Default_Is_Just_The_Verb()
    {
        var plan = AzureCli.Login(FakeTool(), s => { });
        Assert.Equal(["login"], plan.Arguments);
        Assert.Empty(plan.Secrets);
    }

    [Fact]
    public void Login_DeviceCode_Adds_Flag()
    {
        var plan = AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.DeviceCode).SetTenant("contoso.onmicrosoft.com"));
        Assert.Contains("--use-device-code", plan.Arguments);
        Assert.Contains("--tenant", plan.Arguments);
        Assert.Contains("contoso.onmicrosoft.com", plan.Arguments);
    }

    [Fact]
    public void Login_ServicePrincipal_Requires_Username_Password_Tenant()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.ServicePrincipal)));
        Assert.Throws<InvalidOperationException>(() =>
            AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.ServicePrincipal).SetUsername("cid")));
        Assert.Throws<InvalidOperationException>(() =>
            AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.ServicePrincipal).SetUsername("cid").SetPassword(new Secret("p", "v"))));
    }

    [Fact]
    public void Login_ServicePrincipal_Reveals_Password_And_Registers_Secret()
    {
        var pwd = new Secret("SPN secret", "super-secret-value");
        var plan = AzureCli.Login(FakeTool(), s => s
            .SetMode(AzureCliLoginMode.ServicePrincipal)
            .SetUsername("00000000-0000-0000-0000-000000000001")
            .SetPassword(pwd)
            .SetTenant("contoso.onmicrosoft.com"));
        Assert.Contains("--service-principal", plan.Arguments);
        Assert.Contains("--username", plan.Arguments);
        Assert.Contains("--password", plan.Arguments);
        Assert.Contains("super-secret-value", plan.Arguments);
        Assert.Single(plan.Secrets);
        Assert.Same(pwd, plan.Secrets[0]);
    }

    [Fact]
    public void Login_ServicePrincipalCertificate_Requires_Cert()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AzureCli.Login(FakeTool(), s => s
                .SetMode(AzureCliLoginMode.ServicePrincipalCertificate)
                .SetUsername("cid")
                .SetTenant("t")));
    }

    [Fact]
    public void Login_ServicePrincipalCertificate_Round_Trips()
    {
        var plan = AzureCli.Login(FakeTool(), s => s
            .SetMode(AzureCliLoginMode.ServicePrincipalCertificate)
            .SetUsername("cid")
            .SetCertificate("/path/to/cert.pem")
            .SetUseCertSnIssuer()
            .SetTenant("t"));
        Assert.Contains("--service-principal", plan.Arguments);
        Assert.Contains("--certificate", plan.Arguments);
        Assert.Contains("/path/to/cert.pem", plan.Arguments);
        Assert.Contains("--use-cert-sn-issuer", plan.Arguments);
        Assert.Empty(plan.Secrets); // cert path is not a Secret
    }

    [Fact]
    public void Login_ManagedIdentity_Default_Just_Identity()
    {
        var plan = AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.ManagedIdentity));
        Assert.Contains("--identity", plan.Arguments);
        Assert.DoesNotContain("--client-id", plan.Arguments);
    }

    [Fact]
    public void Login_ManagedIdentity_With_UserAssigned_ClientId()
    {
        var plan = AzureCli.Login(FakeTool(), s => s
            .SetMode(AzureCliLoginMode.ManagedIdentity)
            .SetUserAssignedClientId("00000000-0000-0000-0000-000000000002"));
        Assert.Contains("--identity", plan.Arguments);
        Assert.Contains("--client-id", plan.Arguments);
        Assert.Contains("00000000-0000-0000-0000-000000000002", plan.Arguments);
    }

    [Fact]
    public void Login_FederatedToken_Requires_All_Three()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AzureCli.Login(FakeTool(), s => s.SetMode(AzureCliLoginMode.FederatedToken)));
    }

    [Fact]
    public void Login_FederatedToken_Reveals_Token_And_Registers_Secret()
    {
        var token = new Secret("WIF OIDC token", "ey.fake.federated.token");
        var plan = AzureCli.Login(FakeTool(), s => s
            .SetMode(AzureCliLoginMode.FederatedToken)
            .SetUsername("00000000-0000-0000-0000-000000000003")
            .SetFederatedToken(token)
            .SetTenant("contoso.onmicrosoft.com"));
        Assert.Contains("--service-principal", plan.Arguments);
        Assert.Contains("--federated-token", plan.Arguments);
        Assert.Contains("ey.fake.federated.token", plan.Arguments);
        Assert.Single(plan.Secrets);
        Assert.Same(token, plan.Secrets[0]);
    }

    [Fact]
    public void Login_SkipSubscriptionDiscovery_And_AllowNoSubscriptions_Round_Trip()
    {
        var plan = AzureCli.Login(FakeTool(), s => s
            .SetAllowNoSubscriptions()
            .SetSkipSubscriptionDiscovery());
        Assert.Contains("--allow-no-subscriptions", plan.Arguments);
        Assert.Contains("--skip-subscription-discovery", plan.Arguments);
    }

    [Fact]
    public void Login_DefaultSubscription_Uses_Short_Form()
    {
        var plan = AzureCli.Login(FakeTool(), s => s.SetDefaultSubscription("MySub"));
        Assert.Contains("-s", plan.Arguments);
        var sIdx = IndexOf(plan.Arguments, "-s");
        Assert.Equal("MySub", plan.Arguments[sIdx + 1]);
    }

    // ---- logout ----

    [Fact]
    public void Logout_Without_Username_Is_Just_The_Verb()
    {
        Assert.Equal(["logout"], AzureCli.Logout(FakeTool()).Arguments);
    }

    [Fact]
    public void Logout_With_Username()
    {
        var plan = AzureCli.Logout(FakeTool(), s => s.SetUsername("cid"));
        Assert.Equal(["logout", "--username", "cid"], plan.Arguments);
    }

    // ---- rest ----

    [Fact]
    public void Rest_Requires_Uri()
    {
        Assert.Throws<InvalidOperationException>(() => AzureCli.Rest(FakeTool(), s => { }));
    }

    [Fact]
    public void Rest_Default_Method_Implicit_GET()
    {
        var plan = AzureCli.Rest(FakeTool(), s => s.SetUri("https://management.azure.com/subscriptions"));
        Assert.Equal(["rest", "--uri", "https://management.azure.com/subscriptions"], plan.Arguments);
        Assert.DoesNotContain("--method", plan.Arguments);
    }

    [Fact]
    public void Rest_FC1_ListKeys_Pattern()
    {
        // This is the Strata pain-point #4 pattern: az functionapp keys
        // list returns empty on Flex Consumption; must use the listKeys
        // ARM call directly via az rest.
        var plan = AzureCli.Rest(FakeTool(), s => s
            .SetMethod("post")
            .SetUri("https://management.azure.com/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/host/default/listKeys?api-version=2023-12-01")
            .SetQuery("functionKeys.default")
            .SetOutput("tsv"));
        var args = plan.Arguments;
        Assert.Contains("--method", args);
        Assert.Contains("post", args);
        Assert.Contains("listKeys", args[IndexOf(args, "--uri") + 1]);
        Assert.Contains("--query", args);
        Assert.Contains("functionKeys.default", args);
        Assert.Contains("--output", args);
        Assert.Contains("tsv", args);
    }

    [Fact]
    public void Rest_Body_From_File_Uses_At_Prefix()
    {
        var plan = AzureCli.Rest(FakeTool(), s => s.SetUri("/x").SetBodyFromFile("payload.json"));
        Assert.Contains("--body", plan.Arguments);
        Assert.Contains("@payload.json", plan.Arguments);
    }

    [Fact]
    public void Rest_Headers_Emit_As_KEY_EQ_VALUE_After_Single_Flag()
    {
        var plan = AzureCli.Rest(FakeTool(), s => s
            .SetUri("/x")
            .SetHeader("Content-Type", "application/json")
            .SetHeader("X-Request-Id", "abc-123"));
        var args = plan.Arguments;
        var hIdx = IndexOf(args, "--headers");
        Assert.True(hIdx >= 0);
        // The next two args should be KEY=VALUE pairs.
        Assert.Contains("Content-Type=application/json", args);
        Assert.Contains("X-Request-Id=abc-123", args);
    }

    [Fact]
    public void Rest_Resource_And_SkipAuth_Round_Trip()
    {
        var plan = AzureCli.Rest(FakeTool(), s => s
            .SetUri("https://graph.microsoft.com/v1.0/me")
            .SetResource("https://graph.microsoft.com")
            .SetSkipAuthorizationHeader());
        Assert.Contains("--resource", plan.Arguments);
        Assert.Contains("https://graph.microsoft.com", plan.Arguments);
        Assert.Contains("--skip-authorization-header", plan.Arguments);
    }

    [Fact]
    public void Rest_OutputFile_Saves_Response_Payload()
    {
        var plan = AzureCli.Rest(FakeTool(), s => s.SetUri("/x").SetOutputFile("resp.json"));
        Assert.Contains("--output-file", plan.Arguments);
        Assert.Contains("resp.json", plan.Arguments);
    }

    // ---- group ----

    [Theory]
    [InlineData("show")]
    [InlineData("exists")]
    public void Group_ShowOrExists_Require_Name(string verb)
    {
        Assert.Throws<InvalidOperationException>(() => verb switch
        {
            "show" => AzureCli.Group.Show(FakeTool(), s => { }),
            "exists" => AzureCli.Group.Exists(FakeTool(), s => { }),
            _ => throw new InvalidOperationException()
        });
    }

    [Fact]
    public void Group_Show_Round_Trip()
    {
        var plan = AzureCli.Group.Show(FakeTool(), s => s.SetName("rg-strata-prod"));
        Assert.Equal(["group", "show", "--name", "rg-strata-prod"], plan.Arguments);
    }

    [Fact]
    public void Group_Create_Requires_Name_And_Location()
    {
        Assert.Throws<InvalidOperationException>(() => AzureCli.Group.Create(FakeTool(), s => { }));
        Assert.Throws<InvalidOperationException>(() => AzureCli.Group.Create(FakeTool(), s => s.SetName("rg")));
        Assert.Throws<InvalidOperationException>(() => AzureCli.Group.Create(FakeTool(), s => s.SetLocation("eastus")));
    }

    [Fact]
    public void Group_Create_With_Tags_And_ManagedBy()
    {
        var plan = AzureCli.Group.Create(FakeTool(), s => s
            .SetName("rg-strata-test")
            .SetLocation("eastus")
            .SetTag("environment", "test")
            .SetTag("owner", "strata-team")
            .SetManagedBy("/subscriptions/x/resourceGroups/parent"));
        var args = plan.Arguments;
        Assert.Equal(["group", "create"], args.Take(2));
        Assert.Contains("--name", args);
        Assert.Contains("rg-strata-test", args);
        Assert.Contains("--location", args);
        Assert.Contains("eastus", args);
        Assert.Contains("--tags", args);
        Assert.Contains("environment=test", args);
        Assert.Contains("owner=strata-team", args);
        Assert.Contains("--managed-by", args);
    }

    [Fact]
    public void Group_Delete_Requires_Name()
    {
        Assert.Throws<InvalidOperationException>(() => AzureCli.Group.Delete(FakeTool(), s => s.SetYes()));
    }

    [Fact]
    public void Group_Delete_With_Yes_NoWait_Force()
    {
        var plan = AzureCli.Group.Delete(FakeTool(), s => s
            .SetName("rg-strata-test")
            .SetYes()
            .SetNoWait()
            .SetForceDeletionTypes("Microsoft.Compute/virtualMachines"));
        Assert.Contains("--yes", plan.Arguments);
        Assert.Contains("--no-wait", plan.Arguments);
        Assert.Contains("--force-deletion-types", plan.Arguments);
        Assert.Contains("Microsoft.Compute/virtualMachines", plan.Arguments);
    }

    [Fact]
    public void Group_List_With_Tag_Filter()
    {
        var plan = AzureCli.Group.List(FakeTool(), s => s.SetTagFilter("environment=production"));
        Assert.Contains("--tag", plan.Arguments);
        Assert.Contains("environment=production", plan.Arguments);
    }

    // ---- account ----

    [Fact]
    public void Account_Show_Is_Just_The_Verb()
    {
        Assert.Equal(["account", "show"], AzureCli.Account.Show(FakeTool()).Arguments);
    }

    [Fact]
    public void Account_List_All_And_Refresh()
    {
        var plan = AzureCli.Account.List(FakeTool(), s => s.SetAll().SetRefresh());
        Assert.Contains("--all", plan.Arguments);
        Assert.Contains("--refresh", plan.Arguments);
    }

    [Fact]
    public void Account_Set_Requires_SubscriptionId()
    {
        Assert.Throws<InvalidOperationException>(() => AzureCli.Account.Set(FakeTool(), s => { }));
    }

    [Fact]
    public void Account_Set_Round_Trip()
    {
        var plan = AzureCli.Account.Set(FakeTool(), s => s.SetSubscriptionId("my-sub-id"));
        Assert.Equal(["account", "set", "--subscription", "my-sub-id"], plan.Arguments);
    }

    // ---- bicep ----

    [Fact]
    public void Bicep_Build_Requires_File()
    {
        Assert.Throws<InvalidOperationException>(() => AzureCli.Bicep.Build(FakeTool(), s => { }));
    }

    [Fact]
    public void Bicep_Build_Round_Trip()
    {
        var plan = AzureCli.Bicep.Build(FakeTool(), s => s
            .SetFile("infra/main.bicep")
            .SetOutFile("infra/main.json")
            .SetNoRestore());
        var args = plan.Arguments;
        Assert.Equal(["bicep", "build"], args.Take(2));
        Assert.Contains("--file", args);
        Assert.Contains("infra/main.bicep", args);
        Assert.Contains("--outfile", args);
        Assert.Contains("infra/main.json", args);
        Assert.Contains("--no-restore", args);
    }

    [Fact]
    public void Bicep_Build_Stdout_Mode()
    {
        var plan = AzureCli.Bicep.Build(FakeTool(), s => s.SetFile("a.bicep").SetStdout());
        Assert.Contains("--stdout", plan.Arguments);
    }

    [Fact]
    public void Bicep_Install_Version_And_Platform()
    {
        var plan = AzureCli.Bicep.Install(FakeTool(), s => s.SetVersion("v0.40.31").SetTargetPlatform("osx-arm64"));
        Assert.Contains("--version", plan.Arguments);
        Assert.Contains("v0.40.31", plan.Arguments);
        Assert.Contains("--target-platform", plan.Arguments);
        Assert.Contains("osx-arm64", plan.Arguments);
    }

    [Fact]
    public void Bicep_Version_Is_Just_The_Verb()
    {
        Assert.Equal(["bicep", "version"], AzureCli.Bicep.Version(FakeTool()).Arguments);
    }

    // ---- version ----

    [Fact]
    public void Version_Default_Is_Just_The_Verb()
    {
        Assert.Equal(["version"], AzureCli.Version(FakeTool()).Arguments);
    }

    [Fact]
    public void Version_With_Output_Json()
    {
        var plan = AzureCli.Version(FakeTool(), s => s.SetOutput("json"));
        Assert.Equal(["version", "--output", "json"], plan.Arguments);
    }

    // ---- raw ----

    [Fact]
    public void Raw_Requires_Args()
    {
        Assert.Throws<ArgumentException>(() => AzureCli.Raw(FakeTool()));
    }

    [Fact]
    public void Raw_Forwards_Verbatim()
    {
        var plan = AzureCli.Raw(FakeTool(), "staticwebapp", "secrets", "list", "--name", "swa-strata");
        Assert.Equal(["staticwebapp", "secrets", "list", "--name", "swa-strata"], plan.Arguments);
    }

    // ---- nulls ----

    [Fact]
    public void Null_Tool_Throws_For_All_Verbs()
    {
        Assert.Throws<ArgumentNullException>(() => AzureCli.Login(null!, s => { }));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Logout(null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Rest(null!, s => s.SetUri("/x")));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Group.Show(null!, s => s.SetName("rg")));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Account.Show(null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Bicep.Build(null!, s => s.SetFile("a")));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Version(null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Raw(null!, "version"));
    }

    [Fact]
    public void Null_Configurer_Throws_For_Required_Verbs()
    {
        Assert.Throws<ArgumentNullException>(() => AzureCli.Login(FakeTool(), (Action<AzureCliLoginSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Rest(FakeTool(), (Action<AzureCliRestSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Group.Show(FakeTool(), (Action<AzureCliGroupShowSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Group.Create(FakeTool(), (Action<AzureCliGroupCreateSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Group.Delete(FakeTool(), (Action<AzureCliGroupDeleteSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Account.Set(FakeTool(), (Action<AzureCliAccountSetSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => AzureCli.Bicep.Build(FakeTool(), (Action<AzureCliBicepBuildSettings>)null!));
    }

    [Fact]
    public void Many_Headers_Preserve_KV_Pairs_Under_Random_Names()
    {
        // Rest's --headers KEY=VALUE list should round-trip every entry.
        var faker = new Faker();
        var headers = Enumerable.Range(0, 6)
            .ToDictionary(_ => faker.Random.AlphaNumeric(8), _ => faker.Random.AlphaNumeric(12));
        var plan = AzureCli.Rest(FakeTool(), s =>
        {
            s.SetUri("/x");
            foreach (var (k, v) in headers) s.SetHeader(k, v);
        });
        foreach (var (k, v) in headers)
            Assert.Contains($"{k}={v}", plan.Arguments);
    }

    [Fact]
    public void Working_Directory_Flows_To_Plan()
    {
        var cwd = Path.GetTempPath();
        var plan = AzureCli.Group.Show(FakeTool(), s => s.SetName("rg").SetWorkingDirectory(cwd));
        Assert.Equal(cwd, plan.WorkingDirectory);
    }
}
