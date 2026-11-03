namespace Forma.Tests.Shared;

/// <summary>The repository the tests run in.</summary>
public static class RepositoryFolder
{
    /// <summary>Finds the repository's folder, the first above the running tests' own that holds a solution.</summary>
    /// <returns>The repository's folder.</returns>
    public static string Find()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (folder.GetFiles("*.slnx").Length > 0)
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The tests run from outside the repository.");
    }
}
