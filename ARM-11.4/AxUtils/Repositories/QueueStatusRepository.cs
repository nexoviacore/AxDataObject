using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;
using AxUtils.Interface;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AxUtils.Repositories
{
    public class QueueStatusRepository : IQueueStatusRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AppSettingsService _appSettingsService;
        public AppSettingsHelper _appSettingsHelper;

        public QueueStatusRepository(
            Utils common,
            IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;

            _appSettingsService = new AppSettingsService();
            _appSettingsHelper = new AppSettingsHelper(_appSettingsService);

            _common._appSettingsHelper = _appSettingsHelper;
        }

        public Task<QueueStatusDataResult> AxQueueStatus(
            QueueStatusData queueStatusData)
        {
            try
            {
                // -----------------------------------------
                // Validate request
                // -----------------------------------------
                if (queueStatusData == null)
                {
                    return Task.FromResult(new QueueStatusDataResult
                    {
                        Success = false,
                        Message = "Queue status data is required.",
                        result = QueueStatus.InvalidSettings.ToString()
                    });
                }

                string queueName = queueStatusData.QueueName;
                string rmQueueURL = _common._appSettingsHelper.GetRMQueueURL();

                if (string.IsNullOrWhiteSpace(queueName))
                {
                    return Task.FromResult(new QueueStatusDataResult
                    {
                        Success = false,
                        Message = "Queue name is required.",
                        result = QueueStatus.InvalidSettings.ToString()
                    });
                }

                if (string.IsNullOrWhiteSpace(rmQueueURL))
                {
                    return Task.FromResult(new QueueStatusDataResult
                    {
                        Success = false,
                        Message = "RabbitMQ URL is required.",
                        result = QueueStatus.InvalidSettings.ToString()
                    });
                }

                if (rmQueueURL == "noRMQSettings")
                {
                    return Task.FromResult(new QueueStatusDataResult
                    {
                        Success = false,
                        Message = "RabbitMQ Server Setting not found, please contact application Administrator.",
                        result = QueueStatus.InvalidSettings.ToString()
                    });
                }

                // -----------------------------------------
                // Check consumer status
                // -----------------------------------------
                QueueStatus status = CheckConsumerStatus(
                    rmQueueURL,
                    queueName);

                // -----------------------------------------
                // Convert status to API response
                // -----------------------------------------
                switch (status)
                {
                    case QueueStatus.ServerDown:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = false,
                            Message = "RMQueue Server is DOWN or not reachable, please contact Administrator.",
                            result = status.ToString()
                        });

                    case QueueStatus.InvalidSettings:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = false,
                            Message = "RMQueue Settings are incorrect, please contact Administrator.",
                            result = status.ToString()
                        });

                    case QueueStatus.QueueNotFound:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = false,
                            Message = "Queue does not exist or queue name incorrect.",
                            result = status.ToString()
                        });

                    case QueueStatus.NoConsumer:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = false,
                            Message = "Required Consumer Service is not running, please contact Administrator.",
                            result = status.ToString()
                        });

                    case QueueStatus.ConsumerActive:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = true,
                            Message = "OK",
                            result = status.ToString()
                        });

                    default:

                        return Task.FromResult(new QueueStatusDataResult
                        {
                            Success = false,
                            Message = "Unknown RabbitMQ status.",
                            result = status.ToString()
                        });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new QueueStatusDataResult
                {
                    Success = false,
                    Message = "error: Exception-" + ex.Message,
                    result = "Exception"
                });
            }
        }


        public QueueStatus CheckConsumerStatus(
            string rmQueueURL,
            string queueName)
        {
            try
            {
                // -----------------------------------------
                // Validate URL
                // -----------------------------------------
                if (string.IsNullOrWhiteSpace(rmQueueURL))
                    return QueueStatus.InvalidSettings;

                if (string.IsNullOrWhiteSpace(queueName))
                    return QueueStatus.InvalidSettings;

                // -----------------------------------------
                // Create RabbitMQ connection
                // -----------------------------------------
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(rmQueueURL)
                };

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // -----------------------------------------
                    // 1. Check whether queue exists
                    // -----------------------------------------
                    try
                    {
                        channel.QueueDeclarePassive(queueName);
                    }
                    catch (OperationInterruptedException ex)
                    {
                        if (ex.ShutdownReason != null &&
                            ex.ShutdownReason.ReplyCode == 404)
                        {
                            return QueueStatus.QueueNotFound;
                        }

                        return QueueStatus.InvalidSettings;
                    }

                    // -----------------------------------------
                    // 2. Check whether consumer is running
                    // -----------------------------------------
                    try
                    {
                        string tag = channel.BasicConsume(
                            queue: queueName,
                            autoAck: false,
                            consumer: new DummyConsumer(channel),
                            exclusive: true
                        );

                        // We successfully became the exclusive consumer.
                        // Therefore no other consumer is currently running.
                        channel.BasicCancel(tag);

                        return QueueStatus.NoConsumer;
                    }
                    catch (OperationInterruptedException)
                    {
                        // Exclusive consumer failed.
                        // Another consumer is already using the queue.
                        return QueueStatus.ConsumerActive;
                    }
                }
            }
            catch (UriFormatException)
            {
                return QueueStatus.InvalidSettings;
            }
            catch
            {
                return QueueStatus.ServerDown;
            }
        }
    }


    public class DummyConsumer : DefaultBasicConsumer
    {
        public DummyConsumer(IModel model)
            : base(model)
        {
        }

        public override void HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            // Intentionally empty.
        }
    }
}