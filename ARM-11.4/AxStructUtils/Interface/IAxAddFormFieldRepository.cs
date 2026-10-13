using ARMCommon.Model;

namespace AxStructUtils
{
    public interface IAxAddFormFieldRepository
    {
        Task<APIResult> AxAddForm(AxAddFormField formData);
        Task<APIResult> AxAddField(AxAddFormField fieldData);
    }
}
