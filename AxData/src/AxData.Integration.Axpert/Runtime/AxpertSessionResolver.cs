using System.Globalization;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxData.Application.Ports;
using Microsoft.Extensions.Configuration;
using static ARMCommon.Helpers.Constants;

namespace AxData.Integration.Axpert.Runtime;

public sealed record AxpertResolvedSessionRuntime(
    AxpertSessionContext Session,
    Utils Common,
    IARMLogTraceService TraceService,
    string AxpertSessionId);

public interface IAxpertSessionRuntimeAccessor
{
    AxpertResolvedSessionRuntime? Current { get; }
}

public sealed class AxpertSessionResolver(
    IConfiguration configuration,
    IAppSettingsHelper appSettingsHelper,
    IARMLogTraceService traceService) : IAxpertSessionResolver, IAxpertSessionRuntimeAccessor
{
    private AxpertResolvedSessionRuntime? _current;

    public AxpertResolvedSessionRuntime? Current => _current;

    public async Task<AxpertSessionContext?> ResolveAsync(
        AxpertSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.SessionId) ||
            string.IsNullOrWhiteSpace(request.UserName))
        {
            return null;
        }

        if (_current is not null &&
            Same(_current.Session.SessionId, request.SessionId) &&
            Same(_current.Session.UserName, request.UserName))
        {
            return _current.Session;
        }

        _current = null;
        try
        {
            var common = new Utils(
                configuration,
                new RedisCacheHelper(),
                appSettingsHelper,
                traceService);
            var values = await common.GetSessionData(request.SessionId).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (values.Count == 0 ||
                !Required(values, SESSION_DATA.APPNAME, out var appName) ||
                !Required(values, SESSION_DATA.USERNAME, out var userName) ||
                !Required(values, SESSION_DATA.USER_ROLES, out var rolesValue) ||
                !Required(values, SESSION_DATA.AXPERT_SESSIONID, out var axpertSessionId) ||
                !TryGet(values, SESSION_DATA.SITENO, out var siteNumberValue) ||
                !Same(common._appName, appName) ||
                !Same(userName, request.UserName))
            {
                return null;
            }

            var database = appSettingsHelper.GetDBConfigurations(appName);
            if (!database.TryGetValue("DBUser", out var configuredSchema) ||
                string.IsNullOrWhiteSpace(configuredSchema))
            {
                return null;
            }

            var tenantId = configuredSchema.Split('~', 2)[0];
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            var roles = NormalizeRoles(rolesValue);
            var siteNumber = NormalizeSiteNumber(siteNumberValue);
            var session = new AxpertSessionContext(
                request.SessionId,
                tenantId,
                userName,
                roles,
                siteNumber);
            _current = new AxpertResolvedSessionRuntime(
                session,
                common,
                traceService,
                axpertSessionId);
            return session;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _current = null;
            return null;
        }
    }

    internal static IReadOnlySet<string> NormalizeRoles(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    internal static int NormalizeSiteNumber(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var siteNumber))
        {
            return 0;
        }

        return siteNumber < 0 ? 1 : siteNumber;
    }

    private static bool Required(
        IReadOnlyDictionary<string, object> values,
        SESSION_DATA name,
        out string value) =>
        TryGet(values, name, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool TryGet(
        IReadOnlyDictionary<string, object> values,
        SESSION_DATA name,
        out string value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, name.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
