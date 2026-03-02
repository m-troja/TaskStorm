
using TaskStorm.Model.Entity;

namespace TaskStorm.Model.DTO.ChatGpt;

public record ChatGptDto
(
    ActivityType Event,
    IssueDtoChatGpt Issue,
    string EventUserSlackId
);