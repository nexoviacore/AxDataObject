using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxParserApp;
using AxStructDef;
using Newtonsoft.Json;

namespace AxTstructData
{
    public class AxParserProvider : IAxParserProvider
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        // Scoped provider: snapshots live only for the current request.
        private readonly Dictionary<string, string> _parserSnapshots =
            new(StringComparer.OrdinalIgnoreCase);

        public AxParserProvider(
            Utils common,
            IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<AxParser> GetParserAsync(
            string armSessionId,
            StructDef structDef)
        {
            string parserJson = string.Empty;

            if (!_parserSnapshots.TryGetValue(
                    armSessionId,
                    out parserJson))
            {
                parserJson =
                    await _common.GetAxParserObjFromCache(
                        armSessionId);

                if (!string.IsNullOrEmpty(parserJson))
                {
                    _parserSnapshots[armSessionId] =
                        parserJson;
                }
            }

            if (!string.IsNullOrEmpty(parserJson))
            {
                try
                {
                    var cachedParser =
                        JsonConvert.DeserializeObject<AxParser>(
                            parserJson);

                    if (cachedParser != null)
                    {
                        AttachRuntimeDependencies(
                            cachedParser,
                            structDef);

                        return cachedParser;
                    }
                }
                catch (Exception ex)
                {
                    if (_traceService.IsTracingEnabled())
                    {
                        _traceService.LogError(
                            $"Unable to deserialize cached parser. " +
                            $"Parser will be reconstructed. Error: {ex.Message}");
                    }
                }
            }

            var parser = new AxParser(_common, structDef);

            await _common.RestoreSessionGlobalsToParser(
                parser);

            await PreserveReconstructedParser(
                parser,
                armSessionId);

            AttachRuntimeDependencies(parser, structDef);

            return parser;
        }

        private void AttachRuntimeDependencies(
            AxParser parser,
            StructDef structDef)
        {
            parser._common = _common;
            parser._structDef = structDef;
            structDef.AxParser = parser;
        }

        private async Task PreserveReconstructedParser(
            AxParser parser,
            string armSessionId)
        {
            try
            {
                // Runtime dependencies must not be serialized.
                parser._common = null;
                parser._structDef = null;

                var parserJson =
                    JsonConvert.SerializeObject(parser);

                // Reuse the clean snapshot within this request.
                _parserSnapshots[armSessionId] =
                    parserJson;

                var remainingExpiry =
                    await _common._redisCache.GetKeyExpiryAsync(
                        armSessionId);

                // Do not create a persistent parser key.
                if (remainingExpiry > 0)
                {
                    await _common.StoreAxParserObjInCache(
                        armSessionId,
                        parserJson,
                        remainingExpiry);
                }
            }
            catch (Exception ex)
            {
                // Parser reconstruction succeeded. Cache failure
                // must not fail Get, Put, or Delete.
                if (_traceService.IsTracingEnabled())
                {
                    _traceService.LogError(
                        $"Unable to cache reconstructed parser: " +
                        ex.Message);
                }
            }
        }
    }
}