//using System.Data.Common;
//using System.Xml;
//using ARM_APIs.Dto;

//using ARM_APIs.Interface;
//using ARM_APIs.Service;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using AxValidate;
//using ARM_APIs.Repositories;
//using ARMCommon.Interface;
//using AxValidate.Dto;
//using ARMCommon.Helpers;
//using AxStructDef.Helpers;
//using AxStructDef;
//using NPOI.OpenXmlFormats.Dml.Chart;
//using AxParserApp;

//namespace ARM_APIs.Controllers
//{
//    [Route("api/v{version:apiVersion}/[controller]")]
//    [ApiVersion("1")]
//    [ApiController]
//    public class ValidateDataController : ControllerBase
//    {
  
      
       
//        private readonly ILogger<ValidateDataController> _logger; 
//        private Validate _validateDataService;
//        private readonly ValidateDataRepository _repo;
//        private readonly IConfiguration _config;
//        private readonly Utils _common; 
       



//        public ValidateDataController(ILogger<ValidateDataController> logger, Validate validateData, ValidateDataRepository repo, IConfiguration config, Utils common)
//        {

//            _logger = logger;
//            _validateDataService = validateData;
//            _repo = repo;
//            _config = config;
//            _common = common;  

//        }
//        // [HttpGet]
//        // public IActionResult ValidateData()
//        // {
//        //     return Ok("Data validation successfull");
//        // }

//        [HttpPost]
//        public async Task<IActionResult> ValidateData([FromBody] TStructRequestDto requestDto)
//        {
//            string connectionString = _config.GetConnectionString("gms_ConnectionString");
            
//            string dbType = "postgre";

//            var result = new OperationResult();
//            IDbHelper dbHelper = DBHelper.CreateDbHelper(null, dbType, connectionString, null, null, null); 

//            if (dbHelper == null)
//            {
//                result.Message = "Cannot Initialize Db Helper";
//                result.Result = "Fail";
//                return BadRequest(result); 
              
//            }

        


//            if (string.IsNullOrEmpty(requestDto.Transid) || string.IsNullOrWhiteSpace(requestDto.Transid))
//            {
//                result.Result = "Fail";
//                result.Message = "TransId is Empty or Invalid";
//                return BadRequest(result); 
//            }
//            var projectName = requestDto.Project; 
//            string pName = requestDto.Transid;
//            string caption = "";
//            string purpose = "";
//            await _validateDataService.InitializeStructDefAsync(projectName, pName, caption, purpose, connectionString, dbType,dbHelper, null);

//            bool validateStructDef = await _validateDataService.ValidateStructDefAsync();

//            if (!validateStructDef)
//            {
//                return BadRequest("StructDef Not Initialized"); 
//            }
//            if (!ModelState.IsValid)
//                {
//                    _logger.LogError("Validation failed: {@Errors}", ModelState.Values.SelectMany(v => v.Errors));
//                    result.Result = "Fail";
//                    result.Message = "Validation failed: {@Errors}" + ModelState.Values.SelectMany(v => v.Errors);


//                    return BadRequest(result);
//                }

//            _logger.LogInformation("Validation passed for request: {@Request}", requestDto);

//            try
//            {
//                var message = await _validateDataService.SubmitValueToStructDef(requestDto);
//                result.Message = message;

//                // _logger.LogInformation("SubmitValuetoStructDef executed Successfully");
//                // Check Field Validation
//                if (_validateDataService.validationPassed)
//                {
//                    result.Result = "Success";
//                    return Ok(result);


//                }
//                else
//                {
//                    result.Result = "Fail";
//                    return BadRequest(result); 
//                }

                



               

//            }
//            catch (Exception ex)
//            {
//                _logger.LogInformation(ex.Message);
//                result.Result = "Fail";
//                // var errorMessage = _validateDataService.GetErrorLog();
//                result.Message = ex.Message;  
            
//                return BadRequest(result);

//            }
            
            
           
                      
            


//        }

//        [HttpPost("TestValidate")]
//        public async Task<IActionResult> TestValidateData([FromBody] TStructRequestDto requestDto)
//        {
//            OperationResult result = new OperationResult();
//            if (!ModelState.IsValid)
//            {
//                _logger.LogError("Validation failed: {@Errors}", ModelState.Values.SelectMany(v => v.Errors));
//                result.Result = "Fail";
//                result.Message = "Validation failed: {@Errors}" + ModelState.Values.SelectMany(v => v.Errors);


//                return BadRequest(result);
//            }
//            var config = await _common.GetDBConfigurations(requestDto.Project);
//            //string connectionString = 
//            //    _config.GetConnectionString("gms_ConnectionString");

//            //string dbType = "postgre";

//            string connectionString = config["ConnectionString"];
//            string dbType = config["DBType"];


//            var projectName = requestDto.Project;
//            string pName = requestDto.Transid;
//            string caption = "";
//            string purpose = "";

//            IDbHelper dbHelper = DBHelper.CreateDbHelper(null, dbType, connectionString, null, null, null);
            
//            _validateDataService._dbHelper = dbHelper;
//            AxProvider axprovider = new AxProvider(dbHelper, connectionString, dbType);
//            _validateDataService._axp = axprovider;
//            StructDef structDef = await StructDef.CreateAsync(axprovider,pName,null);
//            _validateDataService._sdef = structDef; 
//            AxParser parser = new AxParser(_common, structDef);
//            _validateDataService._parser = parser; 
            
//            if (dbHelper == null)
//            {
//                result.Message = "Cannot Initialize Db Helper";
//                result.Result = "Fail";
//                return BadRequest(result);

//            }




//            if (string.IsNullOrEmpty(requestDto.Transid) || string.IsNullOrWhiteSpace(requestDto.Transid))
//            {
//                result.Result = "Fail";
//                result.Message = "TransId is Empty or Invalid";
//                return BadRequest(result);
//            }
           
//            bool isValid = await _validateDataService.InitValidateAsync(pName);


            

//            if (!isValid)
//            {
//                return BadRequest("StructDef Not Initialized");
//            }
           

//            _logger.LogInformation("Validation passed for request: {@Request}", requestDto);

//            try
//            {
//                var message = await _validateDataService.SubmitValueToSdef(requestDto);
//                var refreshResult = await _validateDataService.RefreshFieldsOnLoad(); 
//                result.Message = string.Join("; ", message, refreshResult);

//                // _logger.LogInformation("SubmitValuetoStructDef executed Successfully");
//                // Check Field Validation
//                if (_validateDataService.validationPassed)
//                {
//                    result.Result = "Success";
//                    return Ok(result);


//                }
//                else
//                {
//                    result.Result = "Fail";
//                    return BadRequest(result);
//                }







//            }
//            catch (Exception ex)
//            {
//                _logger.LogInformation(ex.Message);
//                result.Result = "Fail";
//                // var errorMessage = _validateDataService.GetErrorLog();
//                result.Message = ex.Message;

//                return BadRequest(result);

//            }







//        }
//    }
//}
