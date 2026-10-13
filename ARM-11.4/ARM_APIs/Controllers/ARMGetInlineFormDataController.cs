//using ARM_APIs.Interface;
//using ARMCommon.ActionFilter;
//using ARMCommon.Helpers;
//using ARMCommon.Interface;
//using ARMCommon.Model;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Npgsql;
//using NpgsqlTypes;
//using System.Data;

//namespace ARM_APIs.Controllers
//{
//    [Route("api/v{version:apiVersion}")]
//    [ApiVersion("1")]
//    [ServiceFilter(typeof(ApiResponseFilter))]
//    [ApiController]
//    public class ARMGetInlineFormDataController : Controller
//    {

//        private readonly DataContext _context;
//        private readonly IConfiguration _config;
//        private readonly IRedisHelper _redis;
//        private readonly IPostgresHelper _postGres;
//        private readonly Utils _common;
//        private readonly IARMInlineForm _form;

//        public ARMGetInlineFormDataController(DataContext context, IConfiguration config, IRedisHelper redis, IPostgresHelper postGres, Utils common)
//        {
//            _context = context;
//            _config = config;
//            _redis = redis;
//            _postGres = postGres;
//            _common = common;
//        }


//        [AllowAnonymous]
//        [RequiredFieldsFilter("FormName")]
//        [HttpPost("ARMInlineFormData")]
//        public async Task<IActionResult> ARMGetInlineFormData(InlineForm model)
//        {
//            string rediskey = $"{Constants.REDIS_PREFIX.AXINLINEFORM.ToString()}_{model.FormName}";
//            ARMResult result = new ARMResult();
//            if (string.IsNullOrEmpty(model.FormName))
//            {
//                return BadRequest("FORMNAMEISMISSING");
//            }

//            string formtext = await _redis.StringGetAsync(rediskey);
//            if (!string.IsNullOrEmpty(formtext))
//            {
//                var response = new
//                {
//                    FormText = formtext,
//                };
//                result = new ARMResult();
//                result.result.Add("message", "SUCCESS");
//                result.result.Add("result", response);
//                return Ok(JsonConvert.SerializeObject(result));
//            }
//            else
//            {
//                var inlineFormData = await _context.AxInLineForm.FirstOrDefaultAsync(f => f.Name == model.FormName);
//                if (inlineFormData == null)
//                {
//                    return BadRequest("INLINEFORMHASNORECORD");
//                }
//                var objResult = new
//                {
//                    FormText = inlineFormData?.FormText,
//                    QueueName = inlineFormData?.QueueName,
//                };
//                result = new ARMResult();
//                result.result.Add("message", "SUCCESS");
//                result.result.Add("data", objResult);
//                await _redis.StringSetAsync(rediskey, inlineFormData.FormText);
//                return Ok(result);
//            }

//        }


//        [AllowAnonymous]
//        [HttpPost("ARMSavePageData")]
//        public async Task<IActionResult> ARMSavePageData(ARMSavePageData model)
//        {
//            ARMResult result = new ARMResult();
//            var formName = await _form.GetModulePage(model.pagename);
//            if (formName == null)
//            {
//                return BadRequest(1059);
//            }
//            var inlineFormData = await _context.AxInLineForm.FirstOrDefaultAsync(f => f.Name == model.formname);
//            if (inlineFormData == null)
//            {
//                return BadRequest("INLINEFORMHASNORECORD");
//            }

//            string keyvalue = await _form.GetKeyFieldValue(formName.formdata, model.formname, model.paneldata);
//            var Id = Guid.NewGuid();
//            _context.Database.ExecuteSqlRaw($"INSERT INTO {formName.PageDataTable} (Id, formname, keyvalue, paneldata,status, formmodule, formsubmodule,createddatetime, createdby) VALUES (@Id, @formname, @keyvalue, @paneldata,  @status, @formmodule, @formsubmodule,@createddatetime, @createdby)",
//              new NpgsqlParameter("@Id", Id),
//              new NpgsqlParameter("@formname", model.formname),
//              new NpgsqlParameter("@keyvalue", keyvalue),
//              new NpgsqlParameter("@paneldata", model.paneldata),
//              new NpgsqlParameter("@status", "Vital Taken"),
//              new NpgsqlParameter("@formmodule", formName.Module),
//              new NpgsqlParameter("@formsubmodule", formName.SubModule),
//              new NpgsqlParameter("@createddatetime", DateTime.UtcNow),
//              new NpgsqlParameter("@createdby", "Admin"));
//            return Ok(1060);
//        }

//        [AllowAnonymous]
//        [HttpPost("ARMGetPageData")]
//        public async Task<IActionResult> ARMGetPageData(ARMGetPageData model)
//        {
//            ARMResult result = new ARMResult();
//            FormDetails formDetail = new FormDetails();
//            formDetail.formNames = new List<FormlIst>();

//            var formNames = await _context.AxModulePages.FirstOrDefaultAsync(p => p.PageName == model.PageName);
//            if (formNames == null)
//            {
//                return BadRequest(1061);
//            }
//            string keyfield = "";
//            string jsonStrings = formNames.formdata;
//            jsonStrings = jsonStrings.Replace("\\", "");
//            var Inlineform = JsonConvert.DeserializeObject<List<Form>>(jsonStrings);
//            string allInlineform = await _form.GetFormNameFromPage(formNames.formdata);
//            var datalist = _context.AxInLineForm.FromSqlRaw($"select * FROM  public.\"AxInLineForm\" WHERE \"Name\"  IN({allInlineform})").ToList();

//            foreach (var user in datalist)
//            {
//                keyfield = Inlineform.Where(x => x.form == user.Name.ToString()).Select(x => x.keyfield).FirstOrDefault().ToString();

//                formDetail.formNames.Add(new FormlIst
//                {
//                    formname = user.Name.ToString(),
//                    metadata = user.FormText.ToString(),
//                    statuslist = user.StatusValue.ToString()
//                });
//            }

//            var dataTable = new DataTable();
//            string connectionString = _config["ConnectionStrings:WebApiDatabase"];
//            string sql = Constants_SQL.ARMGetPageData.ToString().Replace("$formNamesPageDataTable$", formNames.PageDataTable).Replace("$allInlineform$", allInlineform).Replace("$Keyvalue$", model.Keyvalue);
//            dataTable = await _postGres.ExecuteSql(sql, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });
//            string data = string.Empty;
//            string formname = string.Empty;
//            string paneldata = string.Empty;
//            if (dataTable.Rows.Count > 0)
//            {
//                data = JsonConvert.SerializeObject(dataTable, Formatting.Indented);
//            }
//            var dataobj = JsonConvert.DeserializeObject<List<ARMPageData>>(data);
//            keyfield = Inlineform.Where(x => x.form == dataobj[0].formname).Select(x => x.keyfield).FirstOrDefault().ToString();

//            var filteredData = dataobj.Select(x => new dataTablelist()
//            {
//                formname = x.formname,
//                paneldata = x.paneldata,
//                status = x.status,
//                keyvalue = x.keyvalue,
//                Keyfiled = keyfield
//            }).ToList();


//            var objResult = new
//            {
//                data = filteredData,
//                inlineform = formDetail.formNames

//            };
//            result = new ARMResult();
//            result.result.Add("message", 1060);
//            result.result.Add("result", objResult);
//            return Ok(result);

//        }

//        [AllowAnonymous]
//        [HttpPost("ARMUpdateStatus")]
//        public async Task<IActionResult> ARMStatusUpdate(StatusUpdate model)
//        {
//            ARMResult result = new ARMResult();


//            var ModulePage = await _form.GetModulePage(model.pagename);
//            if (ModulePage == null)
//            {
//                return BadRequest(1059);
//            }
//            var inlineFormData = await _context.AxInLineForm.FirstOrDefaultAsync(f => f.Name == model.form);
//            if (string.IsNullOrEmpty(inlineFormData.StatusValue))
//            {
//                return BadRequest(1062);
//            }

//            string inputString = inlineFormData.StatusValue;
//            List<string> list = new List<string>(inputString.Split(','));


//            if (list.Contains(model.updatedstatus))
//            {
//                var updated = _context.Database.ExecuteSqlRaw($"Update {ModulePage.PageDataTable} SET  \"status\" = '{model.updatedstatus}'   WHERE \"formname\"  = '{model.form}' ");


//                if (updated > 0)
//                {
//                    return Ok(1063);
//                }

//                else
//                {
//                    return BadRequest(1064);
//                }


//            }
//            else
//            {
//                return BadRequest(1065);
//            }


//        }
//    }
//}
