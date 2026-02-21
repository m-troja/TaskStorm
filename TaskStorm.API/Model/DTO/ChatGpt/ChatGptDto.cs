
namespace TaskStorm.Model.DTO.ChatGpt;

public record ChatGptDto
(
    ChatGptEvent Event,
    IssueDtoChatGpt Issue
);