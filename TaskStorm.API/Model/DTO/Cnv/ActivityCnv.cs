using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv
{
    public class ActivityCnv
    {
        ILogger<ActivityCnv> _logger;

        public ActivityDto EntityToDto(Activity a)
        {
            _logger.LogInformation("Converting Activity entity to DTO. Activity ID: {ActivityId}, Type: {ActivityType}", a.Id, a.Type);

            return a switch
            {
                ActivityPropertyCreated activityPropertyCreated => new ActivityDto(

                    ActivityType: activityPropertyCreated.Type,
                    oldUserId: null,
                    newUserId: null,
                    createdIssueId: activityPropertyCreated.IssueId,
                    updatedIssueId: null,
                    createdCommentId: null,
                    updatedCommentId: null,
                    authorUserId: activityPropertyCreated.AuthorId,
                    oldValue: null,
                    newValue: null,
                    Timestamp: activityPropertyCreated.Timestamp

                    ),

                ActivityPropertyUpdated activityPropertyUpdated => new ActivityDto(

                    ActivityType: activityPropertyUpdated.Type,
                    oldUserId: null,
                    newUserId: null,
                    createdIssueId: activityPropertyUpdated.IssueId,
                    updatedIssueId: null,
                    createdCommentId: null,
                    updatedCommentId: null,
                    authorUserId: null,
                    oldValue: activityPropertyUpdated.OldValue,
                    newValue: activityPropertyUpdated.NewValue,
                    Timestamp: activityPropertyUpdated.Timestamp
                    ),

                _ => new ActivityDto(

                   ActivityType: a.Type,
                   oldUserId: null,
                   newUserId: null,
                   createdIssueId: null,
                   updatedIssueId: null,
                   createdCommentId: null,
                   updatedCommentId: null,
                   authorUserId: null,
                   oldValue: null,
                   newValue: null,
                   Timestamp: a.Timestamp
                   )
            };
        }

        public List<ActivityDto> EntityListToDtoList(ICollection<Activity> activities)
        {
            _logger.LogInformation("Converting list of Activity entities to list of DTOs. Number of activities: {ActivityCount}", activities.Count);
            return activities.Select(EntityToDto).ToList();
        }

        public ActivityCnv(ILogger<ActivityCnv> _logger) 
        {
            this._logger = _logger;
        }

    }
}
