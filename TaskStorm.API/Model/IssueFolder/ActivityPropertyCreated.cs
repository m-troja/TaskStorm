using Microsoft.AspNetCore.Http.HttpResults;
using TaskStorm.Model.Entity;

namespace TaskStorm.Model.IssueFolder;

public class ActivityPropertyCreated : Activity
{
    public ActivityPropertyCreated(ActivityType Type, int issueId, int authorId) : base(Type, issueId)
    {

    }
}
