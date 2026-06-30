using System.Text;
using System.Text.Json;
using Core.Models;
using RabbitMQ.Client;
using UsersApi.Configs;

namespace UsersApi.Service;

public class RabbitMqService : IRabbitMqService, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;

    private RabbitMqService() { }
    
    public static async Task<RabbitMqService> CreateAsync(RabbitMqSettings settings, ILogger<RabbitMqService> logger, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? settings.Host,
            UserName = settings.User,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        int[] retryDelays = [5, 10, 20, 30];
        IConnection? connection = null;

        foreach (var delay in retryDelays)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"[RabbitMQ] Falha ao conectar, tentando em {delay}s...({ex.Message})", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
        }

        if (connection is null || !connection.IsOpen)
        {
            logger.LogError("[RabbitMQ] Não foi possível conectar após todas as tentativas.");
            throw new InvalidOperationException("[RabbitMQ] Não foi possível conectar após todas as tentativas.");
        }
            
        
        var instance = new RabbitMqService();
        instance._connection = connection;
        instance._channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        Console.WriteLine($"[RabbitMQ] Conectado com sucesso! ({connection.Endpoint.HostName}:{connection.Endpoint.Port})");
        
        return instance;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
