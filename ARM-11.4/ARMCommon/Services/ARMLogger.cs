using ARMCommon.Interface;


namespace ARMCommon.Services
{
    public class ARMLogger
    {
        private readonly IRedisHelper _redis;
        public Guid InstanceId { get; private set; }
        public string[] APIList { get; private set; }
        public string[] ServicesList { get; private set; }

        public ARMLogger(IRedisHelper redis)
        {
            _redis = redis;
            InstanceId = Guid.Empty;
            APIList = null;
            ServicesList = null;
            Task.Run(async () => await AutoSyncLoggingConfigAsync());
        }

        private async Task AutoSyncLoggingConfigAsync()
        {
            while (true)
            {
                try
                {
                    var dictlogdetails = await _redis.HashGetAllDictAsync("logdetails");

                    if (dictlogdetails.TryGetValue("api", out var apiNamesString) && dictlogdetails.TryGetValue("InstanceId", out var instanceIdStr) && dictlogdetails.TryGetValue("service", out var serviceNamesStr))
                    {
                        var apiList = apiNamesString.Split(',').Select(api => api.Trim().ToLower()).ToArray();
                        var servicesList = serviceNamesStr.Split(',').Select(service => service.Trim().ToLower()).ToArray();
                        UpdateInstance(Guid.Parse(instanceIdStr), apiList, servicesList);
                    }
                    else
                    {
                        ResetInstance();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred during Redis retrieval: " + ex.Message);
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private void UpdateInstance(Guid instanceId, string[] apiList, string[] servicesList)
        {
            InstanceId = instanceId;
            APIList = apiList;
            ServicesList = servicesList;
        }
        private void ResetInstance()
        {
            InstanceId = Guid.Empty;
            APIList = null;
            ServicesList = null;
            Console.WriteLine("Values not found in Redis. ARMLogger properties have been reset.");
        }


    }
}
