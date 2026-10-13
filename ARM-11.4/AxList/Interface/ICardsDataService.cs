using ARMCommon.Model;

namespace AxList
{
    public interface ICardsDataService
    {
        abstract Task<ARMResult> GetCardsWithDataAsync(CardsData cards);
    }
}
