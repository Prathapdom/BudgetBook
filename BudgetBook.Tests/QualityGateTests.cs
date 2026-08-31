using System.Xml.Linq;

namespace BudgetBook.Tests;

[TestClass]
public sealed class QualityGateTests
{
    [TestMethod]
    public void ThemeDictionaries_ExposeTheSameSemanticKeysInLightDarkAndHighContrast()
    {
        var path = Path.Combine(ProjectRoot(), "Themes", "Colors.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var dictionaries = document.Descendants(presentation + "ResourceDictionary")
            .Where(element => element.Attribute(x + "Key") is not null)
            .ToDictionary(element => element.Attribute(x + "Key")!.Value,
                element => element.Elements().Select(resource => resource.Attribute(x + "Key")?.Value).Where(key => key is not null).Cast<string>().OrderBy(key => key).ToArray());

        CollectionAssert.AreEqual(dictionaries["Light"], dictionaries["Dark"]);
        CollectionAssert.AreEqual(dictionaries["Light"], dictionaries["HighContrast"]);
    }

    [TestMethod]
    public void AllIconOnlyPageButtons_HaveAccessibleNames()
    {
        var pages = Path.Combine(ProjectRoot(), "Pages");
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        foreach (var file in Directory.EnumerateFiles(pages, "*.xaml"))
        {
            var document = XDocument.Load(file);
            foreach (var button in document.Descendants(presentation + "Button"))
            {
                var content = button.Attribute("Content")?.Value;
                if (string.IsNullOrWhiteSpace(content) || content.Length != 1 || content[0] < '\uE000') continue;
                Assert.IsNotNull(button.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "AutomationProperties.Name"),
                    $"Icon-only button in {Path.GetFileName(file)} needs AutomationProperties.Name.");
            }
        }
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BudgetBook.csproj"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate BudgetBook.csproj from the test output directory.");
    }
}
