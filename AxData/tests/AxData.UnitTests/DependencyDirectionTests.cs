using System.Xml.Linq;

namespace AxData.UnitTests;

public sealed class DependencyDirectionTests
{
    private static readonly string[] ForbiddenPackageFragments =
    [
        "redis", "sql", "npgsql", "oracle", "mysql", "rabbitmq",
        "masstransit", "kafka", "azure.messaging.servicebus", "amazon.sqs"
    ];

    [Theory]
    [InlineData("AxData.Domain")]
    [InlineData("AxData.Application")]
    public void Core_projects_do_not_reference_infrastructure_or_axpert_dependencies(string projectName)
    {
        var projectPath = Path.Combine(FindSolutionDirectory(), "src", projectName, $"{projectName}.csproj");
        var project = XDocument.Load(projectPath);

        var forbiddenPackages = project
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Where(IsForbiddenPackage)
            .ToArray();
        var forbiddenAssemblies = project
            .Descendants("Reference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Where(IsForbiddenPackage)
            .ToArray();
        var axpertReferences = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Where(reference => reference.Contains("AxData.Integration.Axpert", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(forbiddenPackages);
        Assert.Empty(forbiddenAssemblies);
        Assert.Empty(axpertReferences);
    }

    [Fact]
    public void Concrete_axpert_namespaces_are_confined_to_the_integration_project()
    {
        var sourceDirectory = Path.Combine(FindSolutionDirectory(), "src");
        var violations = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}AxData.Integration.Axpert{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadLines(path).Any(line =>
                line.TrimStart().StartsWith("using AxStructDef", StringComparison.Ordinal) ||
                line.TrimStart().StartsWith("using AxValidate", StringComparison.Ordinal) ||
                line.TrimStart().StartsWith("using AxParserApp", StringComparison.Ordinal) ||
                line.TrimStart().StartsWith("using AxTstructData", StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(sourceDirectory, path))
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool IsForbiddenPackage(string reference)
        => ForbiddenPackageFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string FindSolutionDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AxData.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AxData.sln.");
    }
}
