using System.Reflection;
using ARMCommon.Interface;
using StackExchange.Redis;

namespace AxData.Integration.Axpert.Runtime;

internal static class NoOpRedisCache
{
    public static IRedisCacheHelper Create()
    {
        var cache = DispatchProxy.Create<IRedisCacheHelper, DefaultProxy>();
        cache.Redis = ConnectedMultiplexer.Create();
        return cache;
    }

    private class ConnectedMultiplexer : DispatchProxy
    {
        public static IConnectionMultiplexer Create() =>
            DispatchProxy.Create<IConnectionMultiplexer, ConnectedMultiplexer>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_IsConnected"
                ? true
                : DefaultValue(targetMethod?.ReturnType);
    }

    private class DefaultProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> properties = new(StringComparer.Ordinal);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
            {
                return null;
            }

            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal) && args is { Length: 1 })
            {
                properties[targetMethod.Name[4..]] = args[0];
                return null;
            }

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal) &&
                properties.TryGetValue(targetMethod.Name[4..], out var value))
            {
                return value;
            }

            return DefaultValue(targetMethod.ReturnType);
        }
    }

    private static object? DefaultValue(Type? returnType)
    {
        if (returnType is null || returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var value = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [value]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
