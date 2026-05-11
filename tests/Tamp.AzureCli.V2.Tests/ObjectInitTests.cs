using System.IO;
using Tamp;
using Xunit;

namespace Tamp.AzureCli.V2.Tests;

public sealed class ObjectInitTests
{
    private static Tool FakeTool(string name = "az") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    // ---- Object-init overloads (TAM-161, 0.2.0+) ----

    [Fact]
    public void Group_Show_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var tool = FakeTool();

        var fluent = AzureCli.Group.Show(tool, s => s
            .SetName("rg-strata-prod")
            .SetOutput("tsv")
            .SetSubscription("00000000-0000-0000-0000-000000000000")
            .SetVerbose());

        var objectInit = AzureCli.Group.Show(tool, new AzureCliGroupShowSettings
        {
            Name = "rg-strata-prod",
            Output = "tsv",
            Subscription = "00000000-0000-0000-0000-000000000000",
            Verbose = true,
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Every_ObjectInit_Overload_Returns_NonNull_CommandPlan()
    {
        var tool = FakeTool();

        // Top-level verbs
        Assert.NotNull(AzureCli.Login(tool, new AzureCliLoginSettings()));
        Assert.NotNull(AzureCli.Logout(tool, new AzureCliLogoutSettings()));
        Assert.NotNull(AzureCli.Rest(tool, new AzureCliRestSettings { Uri = "/x" }));
        Assert.NotNull(AzureCli.Version(tool, new AzureCliVersionSettings()));

        // Group sub-facade
        Assert.NotNull(AzureCli.Group.Show(tool, new AzureCliGroupShowSettings { Name = "rg" }));
        Assert.NotNull(AzureCli.Group.Exists(tool, new AzureCliGroupExistsSettings { Name = "rg" }));
        Assert.NotNull(AzureCli.Group.Create(tool, new AzureCliGroupCreateSettings { Name = "rg", Location = "eastus" }));
        Assert.NotNull(AzureCli.Group.Delete(tool, new AzureCliGroupDeleteSettings { Name = "rg", Yes = true }));
        Assert.NotNull(AzureCli.Group.List(tool, new AzureCliGroupListSettings()));

        // Account sub-facade
        Assert.NotNull(AzureCli.Account.Show(tool, new AzureCliAccountShowSettings()));
        Assert.NotNull(AzureCli.Account.List(tool, new AzureCliAccountListSettings()));
        Assert.NotNull(AzureCli.Account.Set(tool, new AzureCliAccountSetSettings { SubscriptionId = "my-sub-id" }));

        // Bicep sub-facade
        Assert.NotNull(AzureCli.Bicep.Build(tool, new AzureCliBicepBuildSettings { File = "a.bicep" }));
        Assert.NotNull(AzureCli.Bicep.Install(tool, new AzureCliBicepInstallSettings()));
        Assert.NotNull(AzureCli.Bicep.Version(tool, new AzureCliBicepVersionSettings()));
    }
}
