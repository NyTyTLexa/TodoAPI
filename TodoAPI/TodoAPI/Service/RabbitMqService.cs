using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TodoAPI.Models;

namespace TodoAPI.Service;

public class RabbitMqService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqService(IOptions<RabbitMqSettings> options)
        {
            var settings = options.Value;
            var factory = new ConnectionFactory()
            {
                HostName = settings.Host,
                UserName = settings.Username,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                Port = settings.Port
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: "task_exchange",
                type: ExchangeType.Topic);
        }

        public void Publish<T>(string routingKey, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "task_exchange",
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );
        }
    }


