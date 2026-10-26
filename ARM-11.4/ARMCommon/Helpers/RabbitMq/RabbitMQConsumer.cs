using ARMCommon.Interface;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ARMCommon.Helpers.RabbitMq
{

    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        //private readonly IConfiguration _configuration;
        private readonly IAppSettingsHelper _appsettingshelper;
        public delegate Task<string> MyDelegate(string message);
        public RabbitMQConsumer(IAppSettingsHelper appsettingsHelper)
        {
            _appsettingshelper = appsettingsHelper;
        }

        public void DoConsume(string queueName, MyDelegate OnConsume)
        {
            var factory = new ConnectionFactory
            {
                //Uri = new Uri(_configuration["AppConfig:RMQIP"])
                Uri = new Uri(_appsettingshelper.GetRMQueueURL())
            };

            bool delayedmessage = true;
            var connection = factory.CreateConnection();

            using (var channel = connection.CreateModel())
            {
                if (delayedmessage)
                {
                    IDictionary<string, object> args = new Dictionary<string, object>
                {
                    {"x-delayed-type", "direct"}
                };

                    channel.ExchangeDeclare($"{queueName}-exchange", "x-delayed-message", true, false, args);
                }
                else
                {
                    channel.ExchangeDeclare($"{queueName}-exchange", ExchangeType.Direct, true, false, null);
                }
                var queue = channel.QueueDeclare(queueName, true, false, false, null);
                channel.QueueBind(queue, $"{queueName}-exchange", $"{queueName}-route");
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var data = new EventArgsData
                    {
                        ExchangeName = ea.Exchange,
                        RoutingKey = ea.RoutingKey,
                        Headers = ea.BasicProperties.Headers,
                        Body = ea.Body.ToArray(),
                        Message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray())
                    };
                    var headers = ea.BasicProperties.Headers;
                    var Body = ea.Body.ToArray();
                    string message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine(" consumer Received message from producer: {0}", DateTime.Now.ToString());
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QueueLogs.txt");
                    bool traceValue = true;//headers.ContainsKey("trace") ? Convert.ToBoolean(headers["trace"].ToString()) : false;
                    if (traceValue)
                    {
                        StreamWriter sw = new StreamWriter(path, true);
                        var messages = " Traces of Consumer Message are  " + DateTime.Now.ToString() + message;
                        sw.WriteLine(messages);
                        sw.Close();
                    }
                    OnConsume(data.Message);

                };
                channel.BasicConsume(queueName, true, consumer);
                Console.WriteLine($"Connected to {queueName}.");
                Console.ReadLine();
            }
        }

        public void DeleteQueue(string queueName)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_appsettingshelper.GetRMQueueURL())
            };

            var connection = factory.CreateConnection();

            using (var channel = connection.CreateModel())
            {
                channel.QueueDelete(queueName);
            }
        }
        //Concurrent requests working but autorecovery not working in case consumer disapear with queue.
        public void DoConsumeWorkerService(string queueName, MyDelegate OnConsume)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_appsettingshelper.GetRMQueueURL()),
                DispatchConsumersAsync = true
            };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            bool delayedmessage = true;
            if (delayedmessage)
            {
                IDictionary<string, object> args = new Dictionary<string, object> { { "x-delayed-type", "direct" } };
                channel.ExchangeDeclare($"{queueName}-exchange", "x-delayed-message", true, false, args);
            }
            else
            {
                channel.ExchangeDeclare($"{queueName}-exchange", ExchangeType.Direct, true, false, null);
            }
            var queue = channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queue, $"{queueName}-exchange", $"{queueName}-route");
            ushort maxParallel = 10; // tune this
            channel.BasicQos(0, maxParallel, false);
            var semaphore = new SemaphoreSlim(maxParallel);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                await semaphore.WaitAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                        Console.WriteLine($"Consumer received at {DateTime.Now}");
                        await OnConsume(message);
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        channel.BasicNack(ea.DeliveryTag, false, true); // requeue
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
        //Autorecovery working but concurrent requests not working.
        public void DoConsumeWorkerServiceNew(string queueName, Func<string, Task<string>> OnConsume, CancellationToken stoppingToken)
        {
            Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    IConnection connection = null;
                    IModel channel = null;
                    try
                    {
                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(_appsettingshelper.GetRMQueueURL()),
                            DispatchConsumersAsync = true,
                            AutomaticRecoveryEnabled = true,
                            TopologyRecoveryEnabled = true,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(1)
                        };
                        connection = factory.CreateConnection();
                        channel = connection.CreateModel();
                        bool delayedmessage = true;
                        if (delayedmessage)
                        {
                            var args = new Dictionary<string, object>
                    {
                        { "x-delayed-type", "direct" }
                    };

                            channel.ExchangeDeclare(
                                $"{queueName}-exchange",
                                "x-delayed-message",
                                durable: true,
                                autoDelete: false,
                                arguments: args);
                        }
                        else
                        {
                            channel.ExchangeDeclare(
                                $"{queueName}-exchange",
                                ExchangeType.Direct,
                                durable: true,
                                autoDelete: false);
                        }
                        channel.QueueDeclare(
                            queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
                        channel.QueueBind(
                            queue: queueName,
                            exchange: $"{queueName}-exchange",
                            routingKey: $"{queueName}-route");
                        ushort maxParallel = 5;
                        channel.BasicQos(0, maxParallel, false);
                        var semaphore = new SemaphoreSlim(maxParallel);
                        var consumer = new AsyncEventingBasicConsumer(channel);
                        consumer.Received += async (model, ea) =>
                        {
                            await semaphore.WaitAsync(stoppingToken);

                            try
                            {
                                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                                await OnConsume(message);
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                channel.BasicNack(ea.DeliveryTag, false, true);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        };
                        var tcs = new TaskCompletionSource<bool>();
                        void SignalReconnect()
                        {
                            if (!tcs.Task.IsCompleted)
                                tcs.TrySetResult(true);
                        }
                        consumer.ConsumerCancelled += (s, e) =>
                        {
                            SignalReconnect();
                            return Task.CompletedTask;
                        };

                        connection.ConnectionShutdown += (s, e) =>
                        {
                            SignalReconnect();
                        };

                        channel.ModelShutdown += (s, e) =>
                        {
                            SignalReconnect();
                        };
                        channel.BasicConsume(
                            queue: queueName,
                            autoAck: false,
                            consumer: consumer);
                        await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Connection error: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            channel?.Close();
                            connection?.Close();
                        }
                        catch { }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }, stoppingToken);
        }
        //Autorecovery + concurrent requests working fine.
        public void DoConsumeWorkerServiceStable(string queueName, MyDelegate OnConsume, CancellationToken stoppingToken)
        {
            Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    IConnection connection = null;
                    IModel channel = null;

                    try
                    {
                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(_appsettingshelper.GetRMQueueURL()),
                            DispatchConsumersAsync = true,
                            AutomaticRecoveryEnabled = true,
                            TopologyRecoveryEnabled = true,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(2)
                        };

                        connection = factory.CreateConnection();
                        channel = connection.CreateModel();

                        void DeclareInfrastructure()
                        {
                            var args = new Dictionary<string, object>
                    {
                        { "x-delayed-type", "direct" }
                    };

                            channel.ExchangeDeclare($"{queueName}-exchange", "x-delayed-message", true, false, args);
                            channel.QueueDeclare(queueName, true, false, false, null);
                            channel.QueueBind(queueName, $"{queueName}-exchange", $"{queueName}-route");
                        }

                        DeclareInfrastructure();

                        ushort maxParallel = 10;
                        channel.BasicQos(0, maxParallel, false);

                        var semaphore = new SemaphoreSlim(maxParallel);

                        var consumer = new AsyncEventingBasicConsumer(channel);

                        consumer.Received += async (model, ea) =>
                        {
                            await semaphore.WaitAsync(stoppingToken);

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                                    await OnConsume(message);

                                    lock (channel)
                                    {
                                        if (channel.IsOpen)
                                            channel.BasicAck(ea.DeliveryTag, false);
                                    }
                                }
                                catch
                                {
                                    lock (channel)
                                    {
                                        if (channel.IsOpen)
                                            channel.BasicNack(ea.DeliveryTag, false, true);
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }, stoppingToken);
                        };

                        string consumerTag = channel.BasicConsume(queueName, false, consumer);

                        Console.WriteLine("✅ Consumer attached");

                        // 🔥 CRITICAL: detect consumer drop
                        var tcs = new TaskCompletionSource<bool>();

                        void Restart()
                        {
                            if (!tcs.Task.IsCompleted)
                                tcs.TrySetResult(true);
                        }

                        consumer.ConsumerCancelled += (s, e) =>
                        {
                            Console.WriteLine("⚠️ Consumer cancelled → restarting");
                            Restart();
                            return Task.CompletedTask;
                        };

                        channel.CallbackException += (s, e) =>
                        {
                            Console.WriteLine("⚠️ Channel exception → restarting");
                            Restart();
                        };

                        channel.ModelShutdown += (s, e) =>
                        {
                            Console.WriteLine("⚠️ Channel shutdown → restarting");
                            Restart();
                        };

                        connection.ConnectionShutdown += (s, e) =>
                        {
                            Console.WriteLine("⚠️ Connection shutdown → restarting");
                            Restart();
                        };

                        // 🔥 ALSO re-declare infra periodically (handles queue delete)
                        _ = Task.Run(async () =>
                        {
                            while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
                            {
                                try
                                {
                                    DeclareInfrastructure();
                                }
                                catch { }

                                await Task.Delay(3000, stoppingToken); // 🔥 key fix
                            }
                        }, stoppingToken);

                        await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error: {ex.Message}");
                    }
                    finally
                    {
                        try { channel?.Close(); } catch { }
                        try { connection?.Close(); } catch { }
                    }

                    await Task.Delay(1000, stoppingToken);
                }
            }, stoppingToken);
        }
        public void DoConsumeWorkerServiceJob(string queueName, MyDelegate OnConsume)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_appsettingshelper.GetRMQueueURL()),
                DispatchConsumersAsync = true,

                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            bool delayedmessage = true;
            if (delayedmessage)
            {
                IDictionary<string, object> args = new Dictionary<string, object> { { "x-delayed-type", "direct" } };
                channel.ExchangeDeclare($"{queueName}-exchange", "x-delayed-message", true, false, args);
            }
            else
            {
                channel.ExchangeDeclare($"{queueName}-exchange", ExchangeType.Direct, true, false, null);
            }
            channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queueName, $"{queueName}-exchange", $"{queueName}-route");
            ushort maxParallel = 1000;
            channel.BasicQos(0, maxParallel, false);
            var semaphore = new SemaphoreSlim(maxParallel);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                await semaphore.WaitAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                        Console.WriteLine($"Consumer received at {DateTime.Now}");
                        await OnConsume(message);
                        lock (channel)
                        {
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        lock (channel)
                        {
                            channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
    }
}

