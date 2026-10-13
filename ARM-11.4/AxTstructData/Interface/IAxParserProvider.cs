using AxParserApp;
using AxStructDef;

namespace AxTstructData
{
    public interface IAxParserProvider
    {
        Task<AxParser> GetParserAsync(
            string armSessionId,
            StructDef structDef);
    }
}