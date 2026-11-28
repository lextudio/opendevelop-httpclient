using OpenDevelop.IntegrationTests;
using Xunit;

namespace OpenDevelopHttpClient.IntegrationTests;

public sealed class OpenDevelopInstalledAppCollection : ICollectionFixture<OpenDevelopAppFixture>
{
    public const string Name = "OpenDevelopInstalledApp";
}
