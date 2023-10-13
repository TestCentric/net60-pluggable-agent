#tool NuGet.CommandLine&version=6.0.0

// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-dev00048
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

var target = Argument("target", Argument("t", "Default"));

BuildSettings.Initialize
(
	context: Context,
	title: "Net60PluggableAgent",
	solutionFile: "net60-pluggable-agent.sln",
	unitTests: "**/*.tests.exe",
	githubOwner: "TestCentric",
	githubRepository: "net60-pluggable-agent"
);

var MockAssemblyResult = new ExpectedResult("Failed")
{
	Total = 36, Passed = 23, Failed = 5, Warnings = 1, Inconclusive = 1, Skipped = 7,
	Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly.dll") }
};


var AspNetCoreResult = new ExpectedResult("Passed")
{
	Total = 2, Passed = 2, Failed = 0, Warnings = 0, Inconclusive = 0, Skipped = 0,
	Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll") }
};

var WindowsFormsResult = new ExpectedResult("Passed")
{
	Total = 2, Passed = 2, Failed = 0, Warnings = 0, Inconclusive = 0, Skipped = 0,
	Assemblies = new ExpectedAssemblyResult[] {	new ExpectedAssemblyResult("windows-forms-test.dll") }
};

var PackageTests = new PackageTest[] {
	new PackageTest(
		1, "NetCore11PackageTest", "Run mock-assembly.dll targeting .NET Core 1.1",
		"tests/netcoreapp1.1/mock-assembly.dll", MockAssemblyResult),
	new PackageTest(
		1, "NetCore21PackageTest", "Run mock-assembly.dll targeting .NET Core 2.1",
		"tests/netcoreapp2.1/mock-assembly.dll", MockAssemblyResult),
	new PackageTest(
		1, "NetCore31PackageTest", "Run mock-assembly.dll targeting .NET Core 3.1",
		"tests/netcoreapp3.1/mock-assembly.dll", MockAssemblyResult),
	new PackageTest(
		1, "Net50PackageTest", "Run mock-assembly.dll targeting .NET 5.0",
		"tests/net5.0/mock-assembly.dll", MockAssemblyResult),
	new PackageTest(
		1, "Net60PackageTest", "Run mock-assembly.dll targeting .NET 6.0",
		"tests/net6.0/mock-assembly.dll", MockAssemblyResult),
	new PackageTest(
		1, $"AspNetCore60Test", $"Run test using AspNetCore targeting .NET 6.0",
		$"tests/net6.0/aspnetcore-test.dll", AspNetCoreResult),
// Run Windows test for target framework >= 5.0 (6.0 on AppVeyor)
//if (TargetVersion >= V_6_0 || TargetVersion >= V_5_0 && !BuildSettings.IsRunningOnAppVeyor)
	new PackageTest(
		1, "Net60WindowsFormsTest", $"Run test using windows forms under .NET 6.0",
		"tests/net6.0-windows/windows-forms-test.dll", WindowsFormsResult)
};

BuildSettings.Packages.Add(new NuGetPackage(
	"TestCentric.Extension.Net60PluggableAgent",
	title: ".NET 6.0 Pluggable Agent",
	description: "TestCentric engine extension for running tests under .NET 6.0",
	tags: new [] { "testcentric", "pluggable", "agent", "net60" },
	packageContent: new PackageContent()
		.WithRootFiles("../../LICENSE.txt", "../../README.md", "../../testcentric.png")
		.WithDirectories(
			new DirectoryContent("tools").WithFiles(
				"net60-agent-launcher.dll", "net60-agent-launcher.pdb",
				"testcentric.extensibility.api.dll", "testcentric.engine.api.dll" ),
			new DirectoryContent("tools/agent").WithFiles(
				"agent/net60-agent.dll", "agent/net60-agent.pdb", "agent/net60-agent.dll.config",
				"agent/net60-agent.deps.json", $"agent/net60-agent.runtimeconfig.json",
				"agent/testcentric.engine.api.dll", "agent/testcentric.engine.core.dll",
				"agent/testcentric.engine.metadata.dll", "agent/testcentric.extensibility.dll",
				"agent/testcentric.extensibility.api.dll", "agent/Microsoft.Extensions.DependencyModel.dll") ),
	testRunner: new AgentRunner(BuildSettings.NuGetTestDirectory + "TestCentric.Extension.Net60PluggableAgent/tools/agent/net60-agent.dll"),
	tests: PackageTests) );
	
BuildSettings.Packages.Add(new ChocolateyPackage(
		"testcentric-extension-net60-pluggable-agent",
		title: ".NET 60 Pluggable Agent",
		description: "TestCentric engine extension for running tests under .NET 6.0",
		tags: new [] { "testcentric", "pluggable", "agent", "net60" },
		packageContent: new PackageContent()
			.WithRootFiles("../../testcentric.png")
			.WithDirectories(
				new DirectoryContent("tools").WithFiles(
					"../../LICENSE.txt", "../../README.md", "../../VERIFICATION.txt",
					"net60-agent-launcher.dll", "net60-agent-launcher.pdb",
					"testcentric.extensibility.api.dll", "testcentric.engine.api.dll" ),
				new DirectoryContent("tools/agent").WithFiles(
					"agent/net60-agent.dll", "agent/net60-agent.pdb", "agent/net60-agent.dll.config",
					"agent/net60-agent.deps.json", $"agent/net60-agent.runtimeconfig.json",
					"agent/testcentric.engine.api.dll", "agent/testcentric.engine.core.dll",
					"agent/testcentric.engine.metadata.dll", "agent/testcentric.extensibility.dll",
					"agent/testcentric.extensibility.api.dll", "agent/Microsoft.Extensions.DependencyModel.dll") ),
		testRunner: new AgentRunner(BuildSettings.ChocolateyTestDirectory + "testcentric-extension-net60-pluggable-agent/tools/agent/net60-agent.dll"),
		tests: PackageTests) );

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
