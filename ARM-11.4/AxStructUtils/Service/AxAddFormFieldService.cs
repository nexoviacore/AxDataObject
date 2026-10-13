using ARMCommon.Helpers;
using ARMCommon.Model;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;

namespace AxStructUtils
{
    public class AxAddFormFieldService : IAxAddFormFieldService
    {
        private readonly IAxAddFormFieldRepository _repo;
        private readonly Utils _common;
        public AxAddFormFieldService(IAxAddFormFieldRepository repo, Utils common)
        {
            _repo = repo;
            _common = common;
        }
        public async Task<APIResult> AxAddField(AxAddFormField fieldData)
        {
            var result = new APIResult();
            try
            {
                var isValidSession = await _common.ValidateSession(fieldData.ARMSessionId);
                if (!isValidSession)
                {
                    result.success = false;
                    result.data["error"] = $"SessionId is not valid";
                    return result;
                }
                fieldData.projName = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                fieldData.userName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                result = await _repo.AxAddField(fieldData);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
            return result;
        }

        public async Task<APIResult> AxAddForm(AxAddFormField formData)
        {
            var result = new APIResult();
            try
            {
                var isValidSession = await _common.ValidateSession(formData.ARMSessionId);
                if (!isValidSession)
                {
                    result.success = false;
                    result.data["error"] = $"SessionId is not valid";
                    return result;
                }
                formData.projName = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                formData.userName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                result = await _repo.AxAddForm(formData);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
            return result;
        }
    }
}
