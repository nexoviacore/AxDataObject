using ARMCommon.Model;

namespace ARMCommon.Interface
{
    public interface IAPI
    {
        abstract Task<ARMResult> GetData(string url);
        abstract Task<ARMResult> POSTData(string url, string body, string Mediatype);

    }
}
