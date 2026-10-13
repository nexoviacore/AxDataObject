using ARMCommon.Model;

namespace AxList
{
    public interface IEntityService
    {

        abstract Task<SQLResult> GetEntityListData(Entity entity);
        abstract Task<SQLResult> GetFilteredEntityListData(Entity entity);
        abstract Task<SQLResult> GetEntityMetaData(Entity entity);

        abstract Task<SQLResult> GetAnalyticsChartsData(AnalyticsCharts entity);
        abstract Task<SQLResult> GetAnalyticsMultipleChartsData(AnalyticsCharts entity);

        abstract Task<SQLResult> GetSubEntityListData(EntityForm entity);


        abstract Task<AnalyticsEntityOutput> GetAnalyticsPageLoadData(AnalyticsEntityInput analyticsInput);
        abstract Task<AnalyticsEntityOutput> GetAnalyticsEntityData(AnalyticsEntityInput analyticsInput);

        abstract Task<EntityListOutput> GetEntityListPageLoadData(Entity entity);

        abstract Task<SQLResult> GetEntityDropdownValues(EntityFilter entityFilter);

        abstract Task<EntityFormOutput> GetEntityFormPageLoadData(EntityForm entityForm);

    }
}
