using ARMCommon.Model;
using ARM_APIs.Model;
using ARMCommon.Interface;
using Microsoft.EntityFrameworkCore.Internal;

namespace ARM_APIs.Interface
{
    public interface IEntityService
    {

        //abstract Task<SQLResult> GetEntityListData(Entity entity);
        //abstract Task<SQLResult> GetFilteredEntityListData(Entity entity);
        //abstract Task<SQLResult> GetEntityMetaData(Entity entity);
        //abstract Task<SQLResult> GetEntityMetaDataV2(Entity entity);
        abstract Task<SQLResult> GetEntityChartsData(EntityCharts entity);
        //abstract Task<SQLResult> GetAnalyticsChartsData(AnalyticsCharts entity);
        //abstract Task<SQLResult> GetAnalyticsMultipleChartsData(AnalyticsCharts entity);
        abstract Task<SQLResult> GetEntityFormMetaData(EntityForm entity);
        //abstract Task<SQLResult> GetSubEntityListData(EntityForm entity);
        abstract Task<SQLResult> GetSubEntityMetaData(EntityForm entity);
        abstract Task<SQLResult> GetSubEntityChartsData(EntityCharts entity);
        abstract Task<SQLResult> GetEntityList(Entity entity);
        abstract Task<APIResult> SetAnalyticsData(AnalyticsData analyticsData);
        abstract Task<APIResult> GetAnalyticsData(AnalyticsData analyticsData);

        //abstract Task<AnalyticsEntityOutput> GetAnalyticsPageLoadData(AnalyticsEntityInput analyticsInput);
        //abstract Task<AnalyticsEntityOutput> GetAnalyticsEntityData(AnalyticsEntityInput analyticsInput);

        //abstract Task<EntityListOutput> GetEntityListPageLoadData(Entity entity);

        abstract Task<SQLResult> GetEntityEditableFlag(Entity entity);
        //abstract Task<SQLResult> GetEntityDropdownValues(EntityFilter entityFilter);
        abstract Task<SQLResult> GetEntityFormConnectedDataMetrics(EntityForm entityForm);
        //abstract Task<EntityFormOutput> GetEntityFormPageLoadData(EntityForm entityForm);
    }
}
