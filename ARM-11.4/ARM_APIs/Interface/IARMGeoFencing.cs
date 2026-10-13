using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IARMGeoFencing
    {
        abstract Task<string> SaveGeoFencingData(ARMGeoFencingModel geofencing);
        abstract Task<DataTable> GetGeoFencingData(string ARMSessionid);
        abstract Task<string> UpdateGeoLocation(ARMUpdateLocationModel location);
    }
}
