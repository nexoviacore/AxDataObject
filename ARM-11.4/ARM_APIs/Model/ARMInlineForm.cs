using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARM_APIs.Model
{
    public class ARMInlineForm : IARMInlineForm
    {
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly IRedisHelper _redis;
        private readonly ITokenService _tokenService;
        private readonly IPostgresHelper _postGres;
        public ARMInlineForm(DataContext context, IConfiguration configuration, ITokenService tokenService, IRedisHelper redis, IPostgresHelper postGres)
        {
            _context = context;
            _config = configuration;
            _tokenService = tokenService;
            _redis = redis;
            _postGres = postGres;
        }
        public async Task<AxModulePages> GetModulePage(string pageName)
        {
            return await _context.AxModulePages.FirstOrDefaultAsync(p => p.PageName.ToLower() == pageName.ToLower());
        }

        public async Task<string> GetFormNameFromPage( string formdata)
        {
            try
            {
                string jsonStrings = formdata;
                jsonStrings = jsonStrings.Replace("\\", "");
                var Inlineform = JsonConvert.DeserializeObject<List<Form>>(jsonStrings);
                var formslist = Inlineform.Select(x => x.form).ToList();
                string allInlineform = String.Empty;
                foreach (var form in formslist)
                {
                    allInlineform += "'" + form + "', ";
                }
                allInlineform = allInlineform.Remove(allInlineform.Length - 2);
                return allInlineform;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> GetKeyFieldValue(string formdata, string formname, string paneldata)
        {
            string jsonString = formdata;
            jsonString = jsonString.Replace("\\", "");
            var forms = JsonConvert.DeserializeObject<List<Form>>(jsonString);
            var keyfieldValue = forms.Where(x => x.form == formname).Select(x => x.keyfield).FirstOrDefault().ToString();
            JObject jsonObject = JObject.Parse(paneldata);
            string keyvalue = (string)jsonObject[keyfieldValue];
            return keyvalue;
        }

        public async Task<List<FormlIst>> GetFormData(string formdata)
        {
            FormDetails formDetail = new FormDetails();
            formDetail.formNames = new List<FormlIst>();
            string keyfield = "";
            string jsonStrings = formdata;
            jsonStrings = jsonStrings.Replace("\\", "");
            var Inlineform = JsonConvert.DeserializeObject<List<Form>>(jsonStrings);
            string allInlineform = await GetFormNameFromPage(formdata);
            var datalist = _context.AxInLineForm.FromSqlRaw($"select * FROM  public.\"AxInLineForm\" WHERE \"Name\"  IN({allInlineform})").ToList();

            foreach (var user in datalist)
            {
                keyfield = Inlineform.Where(x => x.form == user.Name.ToString()).Select(x => x.keyfield).FirstOrDefault().ToString();

                formDetail.formNames.Add(new FormlIst
                {
                    formname = user.Name.ToString(),
                    metadata = user.FormText.ToString(),
                    statuslist = user.StatusValue.ToString()
                });
            }
            return formDetail.formNames;

        }
    }
}
