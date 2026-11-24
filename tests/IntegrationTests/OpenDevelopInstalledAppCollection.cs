using OpenDevelop.IntegrationTests;
using Xunit;

namespace OpenDevelop.HttpClient.IntegrationTests;

public sealed class OpenDevelopInstalledAppCollection : ICollectionFixture<OpenDevelopAppFixture>
{
    public const string Name = "OpenDevelopInstalledApp";
}
