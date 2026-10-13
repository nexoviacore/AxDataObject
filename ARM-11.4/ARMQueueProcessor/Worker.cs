using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace WorkerServiceDemo
{
    public class Worker : IHostedService, IDisposable
    {

        private readonly ILogger<Worker> _logger;
        private Timer _timer;
        private int executionCount = 0;
        private readonly IConnection _connection;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672")
            };
            _connection = factory.CreateConnection();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(2));
            return Task.CompletedTask;

        }

        private void DoWork(object state)
        {

            var count = Interlocked.Increment(ref executionCount);
            _logger.LogInformation(
                 "MessageListener is working. Count: {Count}", count);
            try
            {
                var channel = _connection.CreateModel();
                channel.QueueDeclare(queue: "logQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    _logger.LogInformation($"consumer Message {JsonConvert.DeserializeObject(message)}");
                    StreamWriter sw = new StreamWriter("D:\\logs.txt", true);
                    var messages = "Consumer Message " + DateTime.Now.ToString() + message;
                    sw.WriteLine(messages);
                    //Close the file
                    sw.Close();
                    channel.BasicAck(ea.DeliveryTag, false);

                };
                channel.BasicConsume(queue: "logQueue", autoAck: false, consumer: consumer);
                Process currentProcess = Process.GetCurrentProcess();
                killProcess(currentProcess, 15000);
                Console.ReadKey();
            }
            catch (Exception exception)
            {
                _logger.LogError("BackgroundTask Failed", exception.Message);
            }


        }

        private void killProcess(Process process, int timeoutInMilliseconds)
        {

            if (!process.WaitForExit(timeoutInMilliseconds))
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // The process already finished by itself, so use the counter value.
                    process.WaitForExit();
                }
                process.WaitForExit();
            }


        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MessageListener is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //Close _connection
            _timer?.Dispose();
        }
    }
}