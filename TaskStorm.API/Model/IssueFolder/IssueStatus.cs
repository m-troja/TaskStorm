namespace TaskStorm.Model.IssueFolder
{
    public enum IssueStatus
    {
        NEW = 0,
        TRIAGE = 1,
        TODO = 2,
        IN_PROGRESS = 3,
        WAITING_FOR_TEAM = 4,
        CODE_REVIEW = 5,
        DONE = 6,
        CANCELED = 7
    }
}
