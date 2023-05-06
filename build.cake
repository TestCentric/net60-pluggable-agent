#tool NuGet.CommandLine&version=6.0.0

// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.0.0-dev00066
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

var target = Argument("target", Argument("t", "Default"));

BuildSettings.Initialize
(
	context: Context,
	title: "Net60PluggableAgent",
	solutionFile: "net60-pluggable-agent.sln",
	unitTests: "net60-agent-launcher.tests.exe",
	githubOwner: "TestCentric",
	githubRepository: "net60-pluggable-agent"
);

// Define Package Tests
//   Level 1 tests are run each time we build the packages
//   Level 2 tests are run for PRs and when packages will be published
//   Level 3 tests are run only when publishing a release

// Ensure that this agent is not used except for .NET 6.0 tests. Since
// this is the only extension installed, all tests built for .NET 5.0
// or lower will run using the .NET 6.0 agent.

var NetCore11PackageTest = new PackageTest(
	1, "NetCore11PackageTest", "Run mock-assembly.dll targeting .NET Core 1.1",
	"tests/netcoreapp1.1/mock-assembly.dll", MockAssemblyResult("Net60AgentLauncher"));

var NetCore21PackageTest = new PackageTest(
	1, "NetCore21PackageTest", "Run mock-assembly.dll targeting .NET Core 2.1",
	"tests/netcoreapp2.1/mock-assembly.dll", MockAssemblyResult("Net60AgentLauncher"));

var NetCore31PackageTest = new PackageTest(
	1, "NetCore31PackageTest", "Run mock-assembly.dll targeting .NET Core 3.1",
	"tests/netcoreapp3.1/mock-assembly.dll", MockAssemblyResult("Net60AgentLauncher"));

var Net50PackageTest = new PackageTest(
	1, "Net50PackageTest", "Run mock-assembly.dll targeting .NET 5.0",
	"tests/net5.0/mock-assembly.dll", MockAssemblyResult("Net60AgentLauncher"));

// Tests actually using this agent

var Net60PackageTest = new PackageTest(
	1, "Net60PackageTest", "Run mock-assembly.dll targeting .NET 6.0",
	"tests/net6.0/mock-assembly.dll", MockAssemblyResult("Net60AgentLauncher"));

var AspNetCore60Test = new PackageTest(
	1, "AspNetCore60Test", "Run test using AspNetCore targeting .NET 6.0",
	"tests/net6.0/aspnetcore-test.dll", AspNetCoreResult("Net60AgentLauncher"));

var Net60WindowsFormsTest = new PackageTest(
	1, "Net60WindowsFormsTest", "Run test using windows forms under .NET 6.0",
	"tests/net6.0-windows/windows-forms-test.dll", WindowsFormsResult("Net60AgentLauncher"));

var packageTests = new PackageTest[] {
	NetCore11PackageTest, NetCore21PackageTest, NetCore31PackageTest,
	Net50PackageTest, Net60PackageTest, Net60PackageTest,
	AspNetCore60Test, Net60WindowsFormsTest };

var NuGetAgentPackage = new NuGetPackage(
	id: "NUnit.Extension.Net60PluggableAgent",
	source: "nuget/Net60PluggableAgent.nuspec",
	basePath: BuildSettings.OutputDirectory,
	testRunner: new GuiRunner("TestCentric.GuiRunner", "2.0.0-dev00272"),
	checks: new PackageCheck[] {
		HasFiles("LICENSE.txt"),
		HasDirectory("tools").WithFiles("net60-agent-launcher.dll", "nunit.engine.api.dll"),
		HasDirectory("tools/agent").WithFiles(
			"net60-pluggable-agent.dll", "net60-pluggable-agent.dll.config",
			"nunit.engine.api.dll", "testcentric.engine.core.dll",
			"testcentric.engine.metadata.dll", "testcentric.extensibility.dll") },
	tests: packageTests);

var ChocolateyAgentPackage = new ChocolateyPackage(
	id: "nunit-extension-net60-pluggable-agent",
	source: "choco/net60-pluggable-agent.nuspec",
	basePath: BuildSettings.OutputDirectory,
	testRunner: new GuiRunner("testcentric-gui", "2.0.0-dev00272"),
	checks: new PackageCheck[] {
		HasDirectory("tools").WithFiles("net60-agent-launcher.dll", "nunit.engine.api.dll")
			.WithFiles("LICENSE.txt", "VERIFICATION.txt"),
		HasDirectory("tools/agent").WithFiles(
			"net60-pluggable-agent.dll", "net60-pluggable-agent.dll.config",
			"nunit.engine.api.dll", "testcentric.engine.core.dll",
			"testcentric.engine.metadata.dll", "testcentric.extensibility.dll") },
	tests: packageTests);

BuildSettings.Packages.AddRange(new PackageDefinition[] { NuGetAgentPackage, ChocolateyAgentPackage });

ExpectedResult MockAssemblyResult(string expectedAgent) => new ExpectedResult("Failed")
{
	Total = 36,
	Passed = 23,
	Failed = 5,
	Warnings = 1,
	Inconclusive = 1,
	Skipped = 7,
	Assemblies = new ExpectedAssemblyResult[]
	{
		new ExpectedAssemblyResult("mock-assembly.dll", expectedAgent)
	}
};

ExpectedResult AspNetCoreResult(string expectedAgent) => new ExpectedResult("Passed")
{
	Assemblies = new ExpectedAssemblyResult[]
	{
		new ExpectedAssemblyResult("aspnetcore-test.dll", expectedAgent)
	}
};

ExpectedResult WindowsFormsResult(string expectedAgent) => new ExpectedResult("Passed")
{
    Assemblies = new ExpectedAssemblyResult[]
	{
		new ExpectedAssemblyResult("windows-forms-test.dll", expectedAgent)
	}
};

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Appveyor")
	.IsDependentOn("DumpSettings")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("Publish")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
