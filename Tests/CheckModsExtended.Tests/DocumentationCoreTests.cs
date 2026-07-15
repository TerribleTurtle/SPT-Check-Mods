using System;
using System.IO;
using Xunit;

namespace CheckModsExtended.Tests
{
    public class DocumentationCoreTests
    {
        [Fact]
        public void Readme_ShouldContainRequiredInformation()
        {
            var basePath = AppContext.BaseDirectory;
            var rootDir = new DirectoryInfo(basePath);
            while (rootDir != null && !File.Exists(Path.Combine(rootDir.FullName, "README.md")))
            {
                rootDir = rootDir.Parent;
            }

            Assert.NotNull(rootDir);
            var readmePath = Path.Combine(rootDir.FullName, "README.md");
            var content = File.ReadAllText(readmePath);

            Assert.Contains(".NET 9.0 SDK", content);

            var ignoreListIndex = content.IndexOf("**Ignore List Management**", StringComparison.OrdinalIgnoreCase);
            Assert.True(ignoreListIndex >= 0, "Ignore list section not found.");
            
            var nextSectionIndex = content.IndexOf("- **Clean Up**", ignoreListIndex, StringComparison.OrdinalIgnoreCase);
            var ignoreListSection = nextSectionIndex >= 0 ? content.Substring(ignoreListIndex, nextSectionIndex - ignoreListIndex) : content.Substring(ignoreListIndex);
            
            Assert.Contains("-t", ignoreListSection);
            Assert.Contains("-s", ignoreListSection);
            Assert.Contains("--sort", ignoreListSection);
            Assert.Contains("-l", ignoreListSection);
            Assert.Contains("--search", ignoreListSection);
        }
    }
}
