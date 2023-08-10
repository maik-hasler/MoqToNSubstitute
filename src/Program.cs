using System.Text.RegularExpressions;

namespace MoqToNSubstitute;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: NSubstituteMigrationTool <testDirectory> <nsubstituteVersion>");
            return;
        }

        string testDirectory = args[0];
        string nsubstituteVersion = args[1];

        if (!Directory.Exists(testDirectory))
        {
            Console.WriteLine("The specified directory does not exist.");
            return;
        }

        string[] files = Directory.GetFiles(testDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            string fileContent = File.ReadAllText(file);
            fileContent = PerformMigration(fileContent);
            File.WriteAllText(file, fileContent);
        }

        UpdateCsProjFiles(testDirectory, nsubstituteVersion);

        Console.WriteLine("Migration completed successfully.");
    }

    public static string PerformMigration(string content)
    {
        content = Regex.Replace(content, @"using Moq;", "using NSubstitute;");
        content = Regex.Replace(content, @"^using Moq*;", "", RegexOptions.Multiline);

        content = Regex.Replace(content, @"new Mock<(.+?)>\((.*?)\)", "Substitute.For<$1>($2)");
        content = Regex.Replace(content, @"\bMock<(.+?)>", "$1");

        content = Regex.Replace(content, @"(?<!\.)\b(\w+)(\s\n\s*)?\.Setup(Get)?\((\w+) => \4(\.?.+?)\)(?=\.R|\s\n)", "$1$5");
        content = Regex.Replace(content, @"\bGet<(.+?)>\(\)\.Setup\((\w+) => \2(\.?.+?)\)(?=\.R|\s\n)", ".Get<$1>()$3");
        content = Regex.Replace(content, @"\bGet<(.+?)>\(\)\.SetupSequence?\((\w+) => \3(\.?.+?)\)(?=\.R|\s\n)", ".Get<$1>()$3");
        content = Regex.Replace(content, @"(?<!\.)\b(\w+)(\s\n\s*)?\.SetupSequence?\((\w+) => \3(\.?.+?)\)(?=\.R|\s\n)", "$1$4");
        content = Regex.Replace(content, @"\.Get<(.+?)>\(\)\.SetupSequence?\((\w+) => \2(\.?.+?)(\)(?!\)))", ".Get<$1>()$3");

        content = Regex.Replace(content, @"(?<!\.)\b(\w+)\.Verify\((\w+) => \2(.+?), Times\.(Once(\(\))?|Exactly\((?<times>\d+)\))\)", "$1.Received(${times})$3");
        content = Regex.Replace(content, @"(?<!\.)\b(\w+)\.Verify\((\w+) => \2(.+?), Times\.Never\)", "$1.DidNotReceive()$3");

        content = Regex.Replace(content, @"(?<!\.)\b(\w+)(\s\n\s*)?\.Setup\(((\w+) => \4(\..?.+?)\))\)\s*\n*\.Throws", "$1.When($3).Throw");

        content = Regex.Replace(content, @"It.IsAny", "Arg.Any");

        content = Regex.Replace(content, @"MoqMockingKernel", "NSubstituteMockingKernel");
        content = Regex.Replace(content, @"using Ninject.MockingKernel.Moq;", "using Ninject.MockingKernel.NSubstitute;");

        content = Regex.Replace(content, @"\.GetMock<(.+?)>\(\)", ".Get<$1>()");

        content = Regex.Replace(content, @"\.Object", "");

        return content;
    }

    static void UpdateCsProjFiles(string directory, string nsubstituteVersion)
    {
        string[] csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);

        foreach (string csprojFile in csprojFiles)
        {
            string csprojContent = File.ReadAllText(csprojFile);

            // Update references from Moq to NSubstitute
            csprojContent = Regex.Replace(csprojContent, @"<PackageReference Include=""Moq(.+?)"".*?>.*?/>", $@"<PackageReference Include=""NSubstitute"" Version=""{nsubstituteVersion}"" />", RegexOptions.Singleline);

            File.WriteAllText(csprojFile, csprojContent);
        }
    }
}