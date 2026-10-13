using ARMCommon.Model;

namespace AxList
{
    public interface ICardsDataRepository
    {
        Task<SQLResult> GetCardsList(CardsData cards);
    }
}
