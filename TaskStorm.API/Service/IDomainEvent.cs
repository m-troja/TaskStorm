namespace TaskStorm.Service;

public interface IDomainEvent
{
    public string EventType { get; }

}
