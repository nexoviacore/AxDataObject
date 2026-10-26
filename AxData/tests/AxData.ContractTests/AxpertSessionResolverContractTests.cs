using AxData.Integration.Axpert.Runtime;

namespace AxData.ContractTests;

public sealed class AxpertSessionResolverContractTests
{
    [Fact]
    public void Session_roles_are_trimmed_and_deduplicated_case_insensitively()
    {
        var roles = AxpertSessionResolver.NormalizeRoles(" reviewer,ADMIN,Reviewer, ,approver ");

        Assert.Equal(3, roles.Count);
        Assert.Contains("reviewer", roles);
        Assert.Contains("admin", roles);
        Assert.Contains("approver", roles);
    }

    [Fact]
    public void Site_number_preserves_legacy_session_normalization()
    {
        Assert.Equal(3, AxpertSessionResolver.NormalizeSiteNumber("3"));
        Assert.Equal(0, AxpertSessionResolver.NormalizeSiteNumber(string.Empty));
        Assert.Equal(0, AxpertSessionResolver.NormalizeSiteNumber("invalid"));
        Assert.Equal(1, AxpertSessionResolver.NormalizeSiteNumber("-2"));
    }
}
