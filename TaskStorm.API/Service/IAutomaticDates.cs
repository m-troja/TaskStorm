namespace TaskStorm.Service
{
    public interface IAutomaticDates
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
