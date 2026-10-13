using ARM_APIs.Model;
using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface ITstructDataService
    {
        Task<APIResult> AxDataPut(TstructData tstructData);
        Task<APIResult> AxDataPutV2(TstructData tstructData);

        Task<APIResult> AxDataGet(TstructGetData getData);

        Task<APIResult> AxADSDataGet(ADSGetData getData);


        Task<APIResult> AxPut(TstructData tstructData);

        Task<APIResult> AxGet(TstructDataGet getData);
    }
}
