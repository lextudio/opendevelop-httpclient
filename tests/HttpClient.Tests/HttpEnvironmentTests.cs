using OpenDevelop.HttpClient;
using Xunit;

namespace OpenDevelop.HttpClient.Tests
{
    public class HttpEnvironmentTests
    {
        [Fact]
        public void ResolveVariable_ActiveEnvironment_ReturnsValue()
        {
            HttpEnvironmentManager.SetActiveEnvironment(new HttpEnvironment
            {
                Name = "test",
                Variables = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["host"] = "localhost"
                }
            });
            Assert.Equal("localhost", HttpEnvironmentManager.ResolveVariable("host"));
        }

        [Fact]
        public void ResolveVariable_SharedEnvironment_ReturnsValue()
        {
            HttpEnvironmentManager.SetActiveEnvironment(new HttpEnvironment { Name = "dev" });
            HttpEnvironmentManager.AddEnvironment(new HttpEnvironment
            {
                Name = "$shared",
                Variables = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["version"] = "v1"
                }
            });
            Assert.Equal("v1", HttpEnvironmentManager.ResolveVariable("version"));
        }

        [Fact]
        public void LoadFromWorkspace_NonExistentDir_DoesNotThrow()
        {
            var ex = Record.Exception(() => HttpEnvironmentManager.LoadFromWorkspace("/nonexistent/path"));
            Assert.Null(ex);
        }
    }
}
