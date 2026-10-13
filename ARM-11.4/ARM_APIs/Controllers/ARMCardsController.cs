using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ARM_APIs.Interface;
using ARM_APIs.Model;


namespace ARM_APIs.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ApiController]
    public class ARMCardsController : Controller
    {
        private readonly ICardsService _cards;

        public ARMCardsController(ICardsService cards)
        {
            _cards = cards;
        }

        //[RequiredFieldsFilter("ARMSessionId")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //[HttpPost("ARMGetCardsData")]
        //public async Task<IActionResult> ARMGetCardsData(Cards cards)
        //{
        //    cards.IsHomePage = true;
        //    var cardlist = await _cards.GetCardsData(cards);
        //    if(cardlist.error is null) { 
        //        ARMResult result = new ARMResult();
        //        result.result.Add("message", "SUCCESS");
        //        var cardsResult = new
        //        {
        //            data = cardlist.data
        //        };
        //        result.result.Add("cards", cardsResult);

        //        return Ok(result);
        //    }
        //    else
        //    {
        //        return BadRequest("NORECORD");
        //    }
        //}

        //[RequiredFieldsFilter("ARMSessionId")]
        //[ServiceFilter(typeof(ApiResponseFilter))]
        //[HttpPost("ARMGetDashboardCardsData")]
        //public async Task<IActionResult> ARMGetDashboardCardsData(Cards cards)
        //{
        //    cards.IsHomePage = false;
        //    var cardlist = await _cards.GetCardsData(cards);
        //    if (cardlist.error is null)
        //    {
        //        ARMResult result = new ARMResult();
        //        result.result.Add("message", "SUCCESS");
        //        var cardsResult = new
        //        {
        //            data = cardlist.data
        //        };
        //        result.result.Add("cards", cardsResult);

        //        return Ok(result);
        //    }
        //    else
        //    {
        //        return BadRequest("NORECORD");
        //    }
        //}

        [RequiredFieldsFilter("ARMSessionId", "CardId")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMGetCardsDataByIds")]
        public async Task<IActionResult> ARMGetCardsDataByIds(Cards cards)
        {
            var cardlist = await _cards.GetCardsData(cards);
            if (cardlist.error is null)
            {
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                var cardsResult = new
                {
                    data = cardlist.data
                };
                result.result.Add("cards", cardsResult);

                return Ok(result);
            }
            else
            {
                return BadRequest("NORECORD");
            }



        }

        [HttpPost("ARMGetProcessCardsData")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        public async Task<IActionResult> ARMGetProcessCardsData(ARMProcessFlowTask processFlow)
        {
            DataTable cardsList = await _cards.GetProcessCards(processFlow);
            if (cardsList.Rows.Count > 0)
            {
                cardsList = await _cards.GetProcessCardsData(processFlow, cardsList);
                ARMResult result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("cards", cardsList);
                return Ok(result);
            }
            else
            {
                return BadRequest("NORECORD");
            }
        }

        [RequiredFieldsFilter("ARMSessionId", "Name", "Context", "HTMLText")]
        [HttpPost("AddorEditHTMLPlugin")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        public async Task<IActionResult> AddorEditHTMLPlugin(HTMLPlugin plugin)
        {
            ARMResult result = new ARMResult();
            var data = await _cards.AddorEditHTMLPlugin(plugin);
            if (!string.IsNullOrEmpty(data.error))
            {
                result.result.Add("message", data.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data);
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("GetHTMLPlugins")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        public async Task<IActionResult> GetHTMLPlugins(HTMLPlugin plugin)
        {
            ARMResult result = new ARMResult();
            var data = await _cards.GetHTMLPlugins(plugin);
            if (!string.IsNullOrEmpty(data.error))
            {
                result.result.Add("message", data.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data);
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("GetCardsWithData_Old")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        public async Task<IActionResult> GetCardsWithData_Old(Cards cards)
        {
            ARMResult result = new ARMResult();
            var data = await _cards.GetCardsWithData(cards);
            if (!string.IsNullOrEmpty(data.error))
            {
                result.result.Add("message", data.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data.data);
                return Ok(result);
            }
        }
    }
}
