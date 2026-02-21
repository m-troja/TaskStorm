namespace TaskStorm.Model.Request;

public record SlackRegistrationRequest(
    string slackName, 
    string slackUserId)
{}