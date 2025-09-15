using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

public class RabbitMqService : IRabbitMqService
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly TimeSpan _confirmTimeout = TimeSpan.FromSeconds(10);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly int _maxPoolSize = 20;
    private readonly ConcurrentQueue<IChannel> _publishChannels = new();

   

    public RabbitMqService(ConnectionFactory connectionFactory)
    {
        _factory = connectionFactory;
    }

    public async Task PublishAsync(string queue, object message, CancellationToken cancellationToken = default)
    {
        await using var channel = await GetPublishChannelAsync();

        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            ReturnPublishChannel(channel);
        }

    }

    //public async Task SubscribeAsync<T>(
    //    string queue,
    //    Func<T, Task> handler,
    //    ushort prefetchCount = 10,
    //    int consumerCount = 4, // number of parallel consumers
    //    CancellationToken cancellationToken = default)
    //{
    //    for (int i = 0; i < consumerCount; i++)
    //    {
    //        await using var channel = await GetPublishChannelAsync();

    //        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
    //        await channel.BasicQosAsync(0, prefetchCount, global: false);

    //        var consumer = new AsyncEventingBasicConsumer(channel);

    //        consumer.ReceivedAsync += async (_, ea) =>
    //        {
    //            try
    //            {
    //                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
    //                var message = JsonSerializer.Deserialize<T>(json);

    //                if (message != null)
    //                {
    //                    await handler(message);
    //                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    //                }
    //                else
    //                {
    //                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
    //                }
    //            }
    //            catch (Exception)
    //            {
    //                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
    //            }
    //        };

    //        await channel.BasicConsumeAsync(
    //            queue: queue,
    //            autoAck: false,
    //            consumer: consumer,
    //            cancellationToken: cancellationToken
    //        );
    //    }
    //}

    private async Task<IChannel> GetPublishChannelAsync()
    {
        if (_publishChannels.TryDequeue(out var ch) && ch.IsOpen)
            return ch;

        var conn = await GetConnectionAsync();
        var newCh = await conn.CreateChannelAsync();
        return newCh;
    }

    private void ReturnPublishChannel(IChannel channel)
    {
        try
        {
            if (!channel.IsOpen)
            {
                channel.Dispose();
                return;
            }
            if (_publishChannels.Count >= _maxPoolSize)
            {
                channel.Dispose();
                return;
            }

            _publishChannels.Enqueue(channel);
        }
        catch
        {
            channel.Dispose();
        }

    }

    private async Task<IConnection> GetConnectionAsync()
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _factory.CreateConnectionAsync();
            }
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private readonly List<IChannel> _subscriberChannels = new();
    private readonly List<AsyncEventingBasicConsumer> _consumers = new();
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    // Subscribe method with multiple consumers
    public async Task SubscribeAsync<T>(
        string queue,
        Func<T, Task> handler,
        ushort prefetchCount = 10,
        int consumerCount = 4,
        CancellationToken cancellationToken = default)
    {
        await _channelLock.WaitAsync();
        try
        {
            for (int i = 0; i < consumerCount; i++)
            {
                var connection = await GetConnectionAsync();
                var channel = await connection.CreateChannelAsync();
                _subscriberChannels.Add(channel);

                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
                await channel.BasicQosAsync(0, prefetchCount, global: false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonSerializer.Deserialize<T>(json);

                        if (message != null)
                        {
                            await handler(message);
                            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                        }
                    }
                    catch
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _consumers.Add(consumer);

                await channel.BasicConsumeAsync(
                    queue: queue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken
                );
            }
        }
        finally
        {
            _channelLock.Release();
        }
    }

    // Call this on application shutdown to stop consumers and close channels
    public async Task StopAllSubscribersAsync()
    {
        await _channelLock.WaitAsync();
        try
        {
            foreach (var channel in _subscriberChannels)
            {
                if (channel.IsOpen)
                    await channel.CloseAsync();
                channel.Dispose();
            }

            _subscriberChannels.Clear();
            _consumers.Clear();
        }
        finally
        {
            _channelLock.Release();
        }
    }


}
