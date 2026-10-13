using ARMCommon.Model;

namespace AxStructUtils
{
    public interface IAxAddFormFieldService
    {
        Task<APIResult> AxAddForm(AxAddFormField formData);
        Task<APIResult> AxAddField(AxAddFormField fieldData);
    }
}
