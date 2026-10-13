using AxData.Domain.Errors;

namespace AxData.UnitTests.Domain;

public sealed class AxDataErrorCatalogTests
{
    private static readonly string[] RequiredPrefixes =
    [
        "AXD-AUTH-", "AXD-NOTFOUND-", "AXD-REQ-", "AXD-VAL-", "AXD-CONC-",
        "AXD-IDEMP-", "AXD-PEG-", "AXD-SAVE-", "AXD-INFRA-"
    ];

    [Fact]
    public void Error_codes_are_unique_and_cover_every_documented_category()
    {
        Assert.Equal(AxDataErrors.All.Count, AxDataErrors.All.Select(error => error.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(RequiredPrefixes, prefix => Assert.Contains(AxDataErrors.All, error => error.Code.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Error_definitions_have_safe_complete_data()
    {
        Assert.All(AxDataErrors.All, error =>
        {
            Assert.StartsWith("AXD-", error.Code);
            Assert.False(string.IsNullOrWhiteSpace(error.Category));
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
            Assert.InRange(error.HttpStatusCode, 400, 599);
        });
    }

    [Fact]
    public void Stale_version_uses_the_documented_contract()
    {
        Assert.Equal("AXD-CONC-001", AxDataErrors.StaleVersion.Code);
        Assert.Equal(409, AxDataErrors.StaleVersion.HttpStatusCode);
    }

    [Fact]
    public void StructDef_change_uses_the_D28_conflict_contract()
    {
        Assert.Equal("AXD-CONC-003", AxDataErrors.StructDefChanged.Code);
        Assert.Equal(409, AxDataErrors.StructDefChanged.HttpStatusCode);
    }

    [Theory]
    [InlineData("AXD-AUTH-001", 401)]
    [InlineData("AXD-AUTH-002", 403)]
    [InlineData("AXD-NOTFOUND-001", 404)]
    [InlineData("AXD-NOTFOUND-002", 410)]
    [InlineData("AXD-REQ-001", 400)]
    [InlineData("AXD-VAL-001", 422)]
    [InlineData("AXD-IDEMP-001", 409)]
    [InlineData("AXD-INFRA-001", 503)]
    public void Representative_errors_have_expected_http_mapping(string code, int status)
    {
        var error = Assert.Single(AxDataErrors.All, candidate => candidate.Code == code);

        Assert.Equal(status, error.HttpStatusCode);
    }
}
