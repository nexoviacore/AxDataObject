using AxData.Api.Errors;
using AxData.Api.Requests;
using Microsoft.AspNetCore.Http;

namespace AxData.ContractTests;

public sealed class RequestMetadataParserTests
{
    [Fact]
    public void If_match_is_primary_and_accepts_documented_quoted_version()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfMatch = "\"14\"";

        Assert.Equal(14, RequestMetadataParser.GetExpectedVersion(context.Request));
        Assert.Equal(14, RequestMetadataParser.GetExpectedVersion(context.Request, 14));
    }

    [Fact]
    public void Expected_version_body_value_is_the_fallback()
    {
        var context = new DefaultHttpContext();

        Assert.Equal(7, RequestMetadataParser.GetExpectedVersion(context.Request, 7));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("W/\"4\"")]
    [InlineData("4")]
    [InlineData("\"0\"")]
    [InlineData("\"not-a-version\"")]
    public void Invalid_if_match_is_rejected(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfMatch = value;

        var exception = Assert.Throws<AxDataApiException>(
            () => RequestMetadataParser.GetExpectedVersion(context.Request));

        Assert.Equal("AXD-REQ-001", exception.Error.Code);
    }

    [Fact]
    public void Conflicting_header_and_body_versions_are_rejected()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfMatch = "\"5\"";

        Assert.Throws<AxDataApiException>(
            () => RequestMetadataParser.GetExpectedVersion(context.Request, 6));
    }

    [Fact]
    public void Idempotency_key_accepts_one_bounded_opaque_value()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestMetadataParser.IdempotencyKeyHeader] = "save-01JABC";

        Assert.Equal("save-01JABC", RequestMetadataParser.GetIdempotencyKey(context.Request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains whitespace")]
    public void Invalid_idempotency_key_is_rejected(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestMetadataParser.IdempotencyKeyHeader] = value;

        Assert.Throws<AxDataApiException>(() => RequestMetadataParser.GetIdempotencyKey(context.Request));
    }
}
