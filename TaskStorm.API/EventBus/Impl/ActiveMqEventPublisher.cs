using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System.Text.Json;
using TaskStorm.EventBus;

namespace TaskStorm.EventBus.Impl;

public class ActiveMqEventPublisher : IEventPublisher
{
    private readonly string _brokerUri;
    private readonly string _queueName;

    public ActiveMqEventPublisher(string brokerUri, string queueName)
    {
        _brokerUri = brokerUri;
        _queueName = queueName;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        // Serialize the event to JSON
        string payload = JsonSerializer.Serialize(@event);

        // Connect to ActiveMQ
        IConnectionFactory factory = new ConnectionFactory(_brokerUri);
        using IConnection connection = factory.CreateConnection();
        using Apache.NMS.ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        IDestination destination = session.GetQueue(_queueName);
        using IMessageProducer producer = session.CreateProducer(destination);

        ITextMessage message = session.CreateTextMessage(payload);
        producer.Send(message);

        await Task.CompletedTask; // just to keep async signature
    }
}