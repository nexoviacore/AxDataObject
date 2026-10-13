using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IARMInlineForm
    {
        abstract Task<AxModulePages> GetModulePage(string pageName);
        abstract Task<string> GetFormNameFromPage(string formdata);
        abstract Task<string> GetKeyFieldValue(string formdata, string formname, string paneldata);
        abstract Task<List<FormlIst>> GetFormData(string formdata);
    }
}
