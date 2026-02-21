namespace TaskStorm.Model.Request;

public record AssignIssueRequestChatGpt(
    string key, 
    string slackUserId)
{}
