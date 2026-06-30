namespace Core.Models;

public interface IRabbitMqService
{
    Task PublishAsync<T>(string exchange, string routingKey, T message);
}