namespace TaskStorm.Model.Request
{
    public record UpdateDescriptionRequest
        (
            int issueId,
            string newDescription
        )
    {
    }
}
