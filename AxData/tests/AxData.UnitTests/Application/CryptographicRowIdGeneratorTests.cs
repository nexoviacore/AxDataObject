using AxData.Application.Ports;

namespace AxData.UnitTests.Application;

public sealed class CryptographicRowIdGeneratorTests
{
    [Fact]
    public void Generates_opaque_unique_row_identifiers()
    {
        var generator = new CryptographicRowIdGenerator();

        var first = generator.NewRowId();
        var second = generator.NewRowId();

        Assert.Matches("^row_[0-9a-f]{32}$", first);
        Assert.Matches("^row_[0-9a-f]{32}$", second);
        Assert.NotEqual(first, second);
    }
}
