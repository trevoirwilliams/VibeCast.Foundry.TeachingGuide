using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Infrastructure.Options;
using VibeCast.Infrastructure.Storage;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class LocalBlobStorageTests
{
    [TestMethod]
    public async Task OpenReadAsync_RejectsSiblingPrefixPathTraversal()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), $"vibecast-storage-{Guid.NewGuid():N}");
        var rootDirectory = Path.Combine(baseDirectory, "root");
        var siblingDirectory = Path.Combine(baseDirectory, "root2");

        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(siblingDirectory);

        var siblingFilePath = Path.Combine(siblingDirectory, "malicious.txt");
        await File.WriteAllTextAsync(siblingFilePath, "outside root");

        var storage = new LocalBlobStorage(Options.Create(new BlobStorageOptions
        {
            RootPath = rootDirectory
        }));

        var traversalKey = $"..{Path.DirectorySeparatorChar}root2{Path.DirectorySeparatorChar}malicious.txt";

        try
        {
            await storage.OpenReadAsync(traversalKey);
            Assert.Fail("Expected the storage root check to reject sibling-prefix traversal.");
        }
        catch (InvalidOperationException exception)
        {
            StringAssert.Contains(exception.Message, "outside the configured storage root");
        }
        finally
        {
            if (Directory.Exists(baseDirectory))
            {
                Directory.Delete(baseDirectory, recursive: true);
            }
        }
    }
}
