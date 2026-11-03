namespace Forma.Examples.Tests;

using System.Text.RegularExpressions;

using Forma.Tests.Shared;

/// <summary>The library's README setting a host up as the example hosts' own files do.</summary>
public sealed partial class ExampleHostTests
{
    private static readonly string RepositoryRoot = RepositoryFolder.Find();

    [Fact]
    public void Should_set_up_each_example_as_the_readme_says()
    {
        // Arrange
        var readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));

        // Act
        var blocks = ExampleBlock().Matches(readme);

        // Assert — each block's lines appear in its file, in order, MyApp standing for the name of the example whose folder holds the file
        Assert.NotEmpty(blocks);
        Assert.All(blocks, block =>
        {
            var path = block.Groups["path"].Value;
            var name = path.Split('/')[1].Replace(".Client", string.Empty, StringComparison.Ordinal);
            var lines = GetLines(block.Groups["code"].Value.Replace("MyApp", name, StringComparison.Ordinal));
            var file = GetLines(File.ReadAllText(Path.Combine(RepositoryRoot, path)));
            var next = 0;

            foreach (var line in lines)
            {
                next = file.IndexOf(line, next) + 1;
                Assert.True(next > 0, $"{path} holds no line '{line}' where the README's block puts it.");
            }
        });
    }

    private static List<string> GetLines(string text)
        => [.. text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];

    [GeneratedRegex(@"<!-- example: (?<path>[^ ]+) -->\s*```\w*\n(?<code>.*?)```", RegexOptions.Singleline)]
    private static partial Regex ExampleBlock();
}
