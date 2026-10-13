using AxList;
using AxList;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AutoMapper;
using Newtonsoft.Json;
using System.Data;
using static ARMCommon.Helpers.Constants;

namespace AxList.Service
{
    public class CardsDataService : ICardsDataService
    {
        private readonly ICardsDataRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private readonly IADSDataService _adsDataService;

        public CardsDataService(ICardsDataRepository repo, IARMLogTraceService traceService, Utils common, IADSDataService adsDataService)
        {
            _repo = repo;
            _traceService = traceService;
            _common = common;
            _adsDataService = adsDataService;
        }

        public async Task<ARMResult> GetCardsWithDataAsync(CardsData cards)
        {
            if (cards.Trace)
            {
                _traceService.EnableTracing(true);
                _traceService.LogMessage("GetCardsWithDataAsync API call started.");
                _traceService.LogMessage($"Start Time: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(cards)}");
            }

            var result = new ARMResult();
            var isValidSession = await _common.ValidateSession(cards.ARMSessionId);
            if (!isValidSession)
            {
                return new ARMResult(false, "SessionId is not valid");
            }

            try
            {
                // Extract session information
                cards.AppName = _common._sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
                cards.UserName = _common._sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                cards.SchemaName = _common._sessionVars[SESSION_DATA.SCHEMANAME.ToString()].ToString();
                cards.Roles = _common._sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString();
                

                result = await GetCardsWithData(cards);
            }
            catch (Exception ex)
            {
                result.result["success"] = false;
                result.result["message"] = "Error occurred. Please check with administrator.";

                if (cards.Trace) _traceService.LogError("Error in GetCardsWithDataAsync : " + ex.Message + ex.StackTrace + ex.InnerException);
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }

            if (cards.Trace) result.result["Trace"] = await _traceService.GetTraceFile($"GetCardsWithDataAsync-{cards.UserName}");

            return result;

        }

        private async Task<ARMResult> GetCardsWithData(CardsData cards)
        {
            var result = new ARMResult();

            var cardsList = await _repo.GetCardsList(cards);
            if (cardsList != null && !string.IsNullOrEmpty(cardsList?.error))
            {
                return new ARMResult(false, $"Error in fetching cards list based on roles - {cardsList?.error}. Please check with administrator.");
            }
            else if(cardsList == null || cardsList.data == null)
            {
                return new ARMResult(false, $"Error in fetching cards list based on roles. Please check with administrator.");
            }

            if (cardsList != null && cardsList.data.Rows.Count > 0)
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap<CardsData, ADSDataRequest>());
                var mapper = config.CreateMapper();
                ADSDataRequest adsDataRequest = mapper.Map<ADSDataRequest>(cards);

                cardsList.data.Columns.Add("carddata", typeof(string));
                foreach (DataRow drCards in cardsList.data.Rows)
                {
                    if (drCards["card_datasource"] != DBNull.Value && !string.IsNullOrEmpty(drCards["card_datasource"].ToString()))
                    {
                        try
                        {
                            string cardDataSource = drCards["card_datasource"].ToString();                      
                            adsDataRequest.DataSource = cardDataSource;
                            adsDataRequest.RefreshCache = cards.RefreshData;                            
                            adsDataRequest.SQLParams = new Dictionary<string, string>();                            

                            // Fetch data using ADSDataService
                            var adsDataResult = await _adsDataService.GetADSDataAsync(adsDataRequest);
                            
                            if (adsDataResult.result.ContainsKey("success") && Convert.ToBoolean(adsDataResult.result["success"]) == true)
                            {
                                if (adsDataResult.result.ContainsKey("data"))
                                {
                                    drCards["carddata"] = JsonConvert.SerializeObject(adsDataResult.result["data"]);
                                }
                                else
                                {
                                    drCards["carddata"] = "";
                                }
                            }
                            else
                            {
                                // Error occurred while fetching data
                                string errorMessage = adsDataResult.result.ContainsKey("message") ? 
                                    adsDataResult.result["message"].ToString() : "Error fetching card data";
                                drCards["carddata"] = errorMessage;
                                
                                if (cards.Trace)
                                {
                                    _traceService.LogError($"Error fetching data for card datasource '{cardDataSource}': {errorMessage}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = "Exception occurred while fetching card data";
                            if (ex.Message.IndexOf("is missing in input params.") > -1)
                                errorMessage = ex.Message;
                            
                            drCards["carddata"] = errorMessage;
                            
                            if (cards.Trace)
                            {
                                _traceService.LogError($"Exception in GetCardsWithData for card datasource '{drCards["card_datasource"]}': {ex.Message} {ex.StackTrace}");
                            }
                        }
                    }
                    else
                    {
                        drCards["carddata"] = "";
                    }
                }

                if (cards.IsMobile == true)
                    cardsList.data.Columns.Remove("html_editor_card");

                cardsList.data = _common.ConvertNumericColumnsToString(cardsList.data);
            }

            result.result["success"] = true;
            result.result["data"] = cardsList.data;
            result.result["message"] = "Cards data retrieved successfully";
            
            return result;
        }
    }
}
