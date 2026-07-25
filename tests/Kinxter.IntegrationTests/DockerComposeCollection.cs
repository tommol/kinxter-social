using Xunit;

namespace Kinxter.IntegrationTests;

[CollectionDefinition("Docker Compose")]
public sealed class DockerComposeCollection : ICollectionFixture<ComposeEnvironmentFixture>
{
    public const string Name = "Docker Compose";
}
