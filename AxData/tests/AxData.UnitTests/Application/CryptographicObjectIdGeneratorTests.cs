using AxData.Application.Ports;
using AxData.TestSupport.Fakes;

namespace AxData.UnitTests.Application;

public sealed class CryptographicObjectIdGeneratorTests
{
    [Fact]
    public void Generates_unique_ulid_shaped_opaque_identifiers()
    {
        var generator = new CryptographicObjectIdGenerator(
            new FakeClock(DateTimeOffset.Parse("2026-09-25T08:00:00Z")));

        var identifiers = Enumerable.Range(0, 100).Select(_ => generator.NewObjectId()).ToArray();

        Assert.Equal(identifiers.Length, identifiers.Distinct(StringComparer.Ordinal).Count());
        Assert.All(identifiers, identifier =>
        {
            Assert.Equal(26, identifier.Length);
            Assert.Matches("^[0-9A-HJKMNP-TV-Z]{26}$", identifier);
        });
    }
}
