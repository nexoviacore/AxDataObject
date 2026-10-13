using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxList
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class CardsDataController : ControllerBase
    {
        private readonly ICardsDataService _cardsDataService;

        public CardsDataController(ICardsDataService cardsDataService)
        {
            _cardsDataService = cardsDataService;
        }

        //[ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("GetCardsWithData")]
        public async Task<IActionResult> GetCardsWithData(CardsData cards)
        {
            try
            {
                var dataResult = await _cardsDataService.GetCardsWithDataAsync(cards);
                dataResult.result.Add("messagetype", "Custom");
                if (dataResult.result["Success"] != null && Convert.ToBoolean(dataResult.result["Success"]) != true)
                {
                    return BadRequest(dataResult);
                }
                else
                {
                    dataResult.result["message"] = "SUCCESS";
                    return Ok(dataResult);
                }
            }
            catch (Exception ex)
            {
                ARMResult result = new ARMResult();
                result.result.Add("messagetype", "Custom");
                result.result.Add("error", $"Error: {ex.Message}");
                result.result.Add("success", false);
                return StatusCode(500, result);
            }
        }
    }
}
