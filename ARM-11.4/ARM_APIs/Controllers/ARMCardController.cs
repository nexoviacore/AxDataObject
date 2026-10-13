using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using ARM_APIs.Interface;
using ARM_APIs.Model;
using System.Text;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace ARM_APIs.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMCardController : Controller
    {
        private readonly IARMMenu _menu;
        private readonly ICardsService _cardsService;

        public ARMCardController(IARMMenu menu, ICardsService cardsService)
        {
            _menu = menu;
            _cardsService = cardsService;
        }

        [HttpPost("ARMGetCardsData")]
        public async Task<IActionResult> ARMGetCardsData(ARMSession model)
        {
            DataTable cardlist = new DataTable();
            DataTable cardresult = new DataTable();
            cardlist = await _menu.GetCardsList(model.ARMSessionId, model.IsHomePage);
            if (cardlist.Rows.Count > 0)
            {
                foreach (DataRow row in cardlist.Rows)
                {
                    string cardsql = row["cardsql"].ToString();
                    if (!string.IsNullOrEmpty(cardsql))
                    {
                        int colonIndex = cardsql.IndexOf(':');

                        if (colonIndex != -1)
                        {
                            string variableName = cardsql.Substring(colonIndex);
                            string username = await _menu.GetUserName(model.ARMSessionId);
                            cardsql = cardsql.Replace(":username", $"'{username}'");
                        }
                        try
                        {
                            cardresult = await _menu.GetCardSQL(model.ARMSessionId, cardsql);
                            string charttype = row["charttype"].ToString().ToLower();
                            if (row["cardtype"].ToString() == "chart" && !string.IsNullOrEmpty(charttype))
                            {
                                string[] columnNames = null;
                                Type[] columnTypes = null;
                                bool isValidMetaData = true;
                                if (charttype == "line" || charttype == "bar" || charttype == "stacked-bar" || charttype == "column" || charttype == "stacked-column" || charttype == "stacked-percentage-column" || charttype == "area")
                                {
                                    columnNames = new string[] { "data_label", "x_axis", "value", "link" };
                                    columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                                }
                                else if (charttype == "pie" || charttype == "semi-donut" || charttype == "donut")
                                {
                                    columnNames = new string[] { "data_label", "value", "link" };
                                    columnTypes = new Type[] { typeof(string), typeof(int), typeof(string) };
                                }
                                else if (charttype == "stacked-group-column")
                                {
                                    columnNames = new string[] { "data_label", "group_column", "value", "link" };
                                    columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                                }
                                else if (charttype == "scatter-plot" || charttype == "scatter-plot-3D")
                                {
                                    columnNames = new string[] { "data_label", "x_axis_data", "y_axis_data", "z_axis_data" };
                                    columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                                }
                                else if (charttype == "funnel")
                                {
                                    columnNames = new string[] { "data_label", "value" };
                                    columnTypes = new Type[] { typeof(string), typeof(int) };
                                }
                                foreach (DataColumn column in cardresult.Columns)
                                {
                                    if (!columnNames.Contains(column.ColumnName))
                                    {
                                        isValidMetaData = false;
                                        break;
                                    }
                                }

                                if (!isValidMetaData)
                                {
                                    for (int i = 0; i < columnNames.Length; i++)
                                    {
                                        if (i < cardresult.Columns.Count && cardresult.Columns[i] != null)
                                        {
                                            if (columnNames[i] == "value")
                                            {
                                                if (cardresult.Columns[i].DataType == typeof(int) || cardresult.Columns[i].DataType == typeof(Int32) || cardresult.Columns[i].DataType == typeof(Int64) || cardresult.Columns[i].DataType == typeof(Decimal) || cardresult.Columns[i].DataType == typeof(float) || cardresult.Columns[i].DataType == typeof(Double))
                                                {
                                                    if (cardresult.Columns.Contains(columnNames[i]))
                                                    {
                                                        cardresult.Columns[columnNames[i]].ColumnName = columnNames[i] + i;//Renaming columns
                                                    }
                                                    cardresult.Columns[i].ColumnName = columnNames[i];
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        cardresult.Columns.Add(columnNames[i], columnTypes[i]).SetOrdinal(i);
                                                    }
                                                    catch { }
                                                }
                                            }
                                            else
                                            {
                                                if (cardresult.Columns.Contains(columnNames[i]))
                                                {
                                                    cardresult.Columns[columnNames[i]].ColumnName = columnNames[i] + i;//Renaming columns
                                                }
                                            }
                                            cardresult.Columns[i].ColumnName = columnNames[i];

                                        }
                                        else
                                        {
                                            try
                                            {
                                                cardresult.Columns.Add(columnNames[i], columnTypes[i]).SetOrdinal(i);
                                            }
                                            catch { }
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < columnNames.Length; i++)
                                    {
                                        if (!cardresult.Columns.Contains(columnNames[i]))
                                        {
                                            cardresult.Columns.Add(columnNames[i], columnTypes[i]);
                                        }
                                    }
                                }

                            }
                            StringBuilder sb = new StringBuilder();
                            sb.Append("{");
                            sb.Append("\"fields\": [");
                            foreach (DataColumn column in cardresult.Columns)
                            {
                                sb.Append("{");
                                sb.Append("\"name\": \"" + column.ColumnName + "\",");
                                sb.Append("\"datatype\": \"" + column.DataType.Name + "\"");
                                sb.Append("},");
                            }
                            sb.Remove(sb.Length - 1, 1); // remove the last comma
                            sb.Append("]");
                            sb.Append(",");
                            sb.Append("\"row\": ");

                            if (cardresult.Rows.Count == 0)
                            {
                                sb.Append("[]");
                            }
                            else
                            {
                                sb.Append("[");
                                foreach (DataRow dataRow in cardresult.Rows)
                                {
                                    sb.Append("{");
                                    foreach (DataColumn column in cardresult.Columns)
                                    {
                                        sb.Append("\"" + column.ColumnName + "\":\"" + dataRow[column.ColumnName].ToString().Replace("\r\n", "") + "\",").Replace("\r\n", "");
                                    }
                                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                                    sb.Append("},");
                                }
                                sb.Remove(sb.Length - 1, 1); // remove the last comma
                                sb.Append("]");
                            }

                            sb.Append("}");
                            row["cardsql"] = sb.ToString();
                            if (string.IsNullOrEmpty(row["charttype"].ToString()))
                            {
                                row["charttype"] = "";
                            }
                        }
                        catch (Exception ex)
                        {
                            row["cardsql"] = JsonConvert.SerializeObject(ex.Message, Formatting.Indented);
                        }
                    }
                }

                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                var cardsResult = new
                {
                    data = cardlist
                };
                result.result.Add("cards", cardsResult);

                return Ok(result);
            }
            else
            {
                return BadRequest("NORECORD");
            }
        }

        //[HttpPost("ARMGetCardsDataByIds")]
        //public async Task<IActionResult> ARMGetCardsDataByIds(ARMSession model)
        //{
        //    DataTable cardlist = new DataTable();
        //    DataTable cardresult = new DataTable();
        //    cardlist = await _menu.GetCardListById(model.ARMSessionId, model.CardId);
        //    if (cardlist.Rows.Count > 0)
        //    {
        //        foreach (DataRow row in cardlist.Rows)
        //        {
        //            string cardsql = row["cardsql"].ToString();
        //            if (!string.IsNullOrEmpty(cardsql))
        //            {
        //                int colonIndex = cardsql.IndexOf(':');

        //                if (colonIndex != -1)
        //                {
        //                    string variableName = cardsql.Substring(colonIndex);
        //                    string username = await _menu.GetUserName(model.ARMSessionId);
        //                    cardsql = cardsql.Replace(":username", $"'{username}'");
        //                }

        //                if (model.SqlParams.Count > 0)
        //                {
        //                    foreach (KeyValuePair<string, string> keyValuePair in model.SqlParams)
        //                    {
        //                        if (cardsql.IndexOf(':') == -1)
        //                            break;

        //                        cardsql = cardsql.Replace($":{keyValuePair.Key}", $"'{keyValuePair.Value}'");
        //                    }
        //                }
        //                try
        //                {
        //                    cardresult = await _menu.GetCardSQL(model.ARMSessionId, cardsql);
        //                    string sb = _menu.GenerateCardSql(cardresult);
        //                    string json = sb;
        //                    row["cardsql"] = JObject.Parse(json);

        //                    {
        //                        row["charttype"] = "";
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    row["cardsql"] = JsonConvert.SerializeObject(ex.Message, Formatting.Indented);
        //                }
        //            }
        //        }

        //        ARMResult result = new ARMResult();
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("cards", cardlist);

        //        return Ok(result);
        //    }
        //    else
        //    {
        //        return BadRequest("NORECORD");
        //    }
        //}

        //[HttpPost("ARMGetProcessCardsData")]
        //public async Task<IActionResult> ARMGetProcessCardsData(ARMProcessFlowTask processFlow)
        //{
        //    DataTable cardsList = await _menu.GetProcessCards(processFlow);
        //    if (cardsList.Rows.Count > 0)
        //    {
        //        cardsList = await _menu.GetCardsData(processFlow, cardsList);
        //        ARMResult result = new ARMResult();
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("cards", cardsList);
        //        return Ok(result);
        //    }
        //    else
        //    {
        //        return BadRequest("NORECORD");
        //    }
        //}

        //[RequiredFieldsFilter("ARMSessionId")]
        //[HttpPost("GetCardsWithData")]
        //public async Task<IActionResult> GetCardsWithData(Cards cards)
        //{
        //    ARMResult result = new ARMResult();
        //    var data = await _cardsService.GetCardsWithData(cards);
        //    if (!string.IsNullOrEmpty(data.error))
        //    {
        //        result.result.Add("message", data.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("data", data.data);
        //        return Ok(result);
        //    }
        //}

        //[RequiredFieldsFilter("ARMSessionId", "Name", "Context", "HTMLText")]
        //[HttpPost("AddorEditHTMLPlugin")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //public async Task<IActionResult> AddorEditHTMLPlugin(HTMLPlugin plugin)
        //{
        //    ARMResult result = new ARMResult();
        //    var data = await _cardsService.AddorEditHTMLPlugin(plugin);
        //    if (!string.IsNullOrEmpty(data.error))
        //    {
        //        result.result.Add("message", data.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("data", data);
        //        return Ok(result);
        //    }
        //}

        //[RequiredFieldsFilter("ARMSessionId")]
        //[HttpPost("GetHTMLPlugins")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //public async Task<IActionResult> GetHTMLPlugins(HTMLPlugin plugin)
        //{
        //    ARMResult result = new ARMResult();
        //    var data = await _cardsService.GetHTMLPlugins(plugin);
        //    if (!string.IsNullOrEmpty(data.error))
        //    {
        //        result.result.Add("message", data.error);
        //        result.result.Add("messagetype", "Custom");
        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result.Add("message", "SUCCESS");
        //        result.result.Add("data", data);
        //        return Ok(result);
        //    }
        //}
    }
}
