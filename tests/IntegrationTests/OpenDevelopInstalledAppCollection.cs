using OpenDevelop.IntegrationTests;
using Xunit;

namespace OpenDevelopHttpClient.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenDevelopInstalledAppCollection : ICollectionFixture<OpenDevelopAppFixture>
{
    public const string Name = "OpenDevelopInstalledApp";
}
