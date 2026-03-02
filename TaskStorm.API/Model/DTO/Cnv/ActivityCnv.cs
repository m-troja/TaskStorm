using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.DTO.Cnv
{
    public class ActivityCnv
    {
        ILogger<ActivityCnv> _logger;

        public ActivityDto EntityToDto(Activity a)
        {
            _logger.LogInformation("Converting Activity entity to DTO. Activity ID= {ActivityId}, Type= {ActivityType}", a.Id, a.Type);

            return a switch
            {
                ActivityPropertyCreated activityPropertyCreated => activityPropertyCreated.Type switch
                {
                    ActivityType.CREATED_ISSUE => new ActivityDto
                    {
                        ActivityType = activityPropertyCreated.Type,
                        IssueId = activityPropertyCreated.IssueId,
                        EventAuthorUserId = activityPropertyCreated.EventAuthorUserId,
                        Timestamp = activityPropertyCreated.Timestamp
                    },
                    ActivityType.CREATED_COMMENT => new ActivityDto
                    {
                        ActivityType = activityPropertyCreated.Type,
                        IssueId = activityPropertyCreated.IssueId,
                        CommentId = activityPropertyCreated.CommentId,
                        EventAuthorUserId = activityPropertyCreated.EventAuthorUserId,
                        Timestamp = activityPropertyCreated.Timestamp
                    },
                
                    _ => new ActivityDto {

                    }
                },


                ActivityPropertyUpdated activityPropertyUpdated => activityPropertyUpdated.Type switch
                {
                    ActivityType.UPDATED_DESCRIPTION => new ActivityDto
                    {
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        OldValue = activityPropertyUpdated.OldValue,
                        NewValue = activityPropertyUpdated.NewValue,
                        Timestamp = activityPropertyUpdated.Timestamp

                    },
                    ActivityType.UPDATED_STATUS => new ActivityDto
                    { 
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        OldStatus = activityPropertyUpdated.OldValue != null ? Enum.Parse<IssueStatus>(activityPropertyUpdated.OldValue) : null,
                        NewStatus = activityPropertyUpdated.NewValue != null ? Enum.Parse<IssueStatus>(activityPropertyUpdated.NewValue) : null,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        Timestamp = activityPropertyUpdated.Timestamp

                    },
                    ActivityType.UPDATED_PRIORITY => new ActivityDto
                    {
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        OldPriority = activityPropertyUpdated.OldValue != null ? Enum.Parse<IssuePriority>(activityPropertyUpdated.OldValue) : null,
                        NewPriority = activityPropertyUpdated.NewValue != null ? Enum.Parse<IssuePriority>(activityPropertyUpdated.NewValue) : null,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        Timestamp = activityPropertyUpdated.Timestamp
                    },
                    ActivityType.UPDATED_ASSIGNEE => new ActivityDto
                    {
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        OldAssigneeId = activityPropertyUpdated.OldValue != null ? int.Parse(activityPropertyUpdated.OldValue) : null,
                        NewAssigneeId = activityPropertyUpdated.NewValue != null ? int.Parse(activityPropertyUpdated.NewValue) : null,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        Timestamp = activityPropertyUpdated.Timestamp
                    },
                    ActivityType.UPDATED_DUEDATE => new ActivityDto
                    {
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        OldDateTime = activityPropertyUpdated.OldValue != null ? DateTime.Parse(activityPropertyUpdated.OldValue) : null,
                        NewDateTime = activityPropertyUpdated.NewValue != null ? DateTime.Parse(activityPropertyUpdated.NewValue) : null,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        Timestamp = activityPropertyUpdated.Timestamp
                    },
                    ActivityType.UPDATE_TEAM => new ActivityDto
                    {
                        ActivityType = activityPropertyUpdated.Type,
                        IssueId = activityPropertyUpdated.IssueId,
                        OldTeamId = activityPropertyUpdated.OldValue != null ? int.Parse(activityPropertyUpdated.OldValue) : null,
                        NewTeamId = activityPropertyUpdated.NewValue != null ? int.Parse(activityPropertyUpdated.NewValue) : null,
                        EventAuthorUserId = activityPropertyUpdated.EventAuthorUserId,
                        Timestamp = activityPropertyUpdated.Timestamp
                    },
                    _ => new ActivityDto {
                    }
                }

            };
        }

        public List<ActivityDto> EntityListToDtoList(ICollection<Activity> activities)
        {
            _logger.LogInformation("Converting list of Activity entities to list of DTOs. Number of activities= {ActivityCount}", activities.Count);
            return activities.Select(EntityToDto).ToList();
        }

        public ActivityCnv(ILogger<ActivityCnv> _logger) 
        {
            this._logger = _logger;
        }

    }
}
