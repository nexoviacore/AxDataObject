using RabbitMQ.Client;

namespace ARMCommon.Helpers.RabbitMq
{
    public class RabbitMQHelper
    {

        public string CanConnectToRabbitMQ(string uri)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(uri),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                if (connection.IsOpen)
                    return "Connected to RMQ";
                else
                    return "Not able to connect to RMQ";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
