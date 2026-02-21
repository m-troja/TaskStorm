namespace TaskStorm.Model.Entity;

public record ChatGptUser(
    int id,
    String slackName,
    String slackUserId
    ){}
