using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service
{
    public interface IIssueService
    {
        Task<Issue> GetIssueByIdAsync(int id);
        Task<IssueDto> GetIssueDtoByIdAsync(int id);
        Task<IssueDto> GetIssueDtoByKeyAsync(string keyString);
        Task<Issue> CreateIssueAsync(CreateIssueRequest cir);
        Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest scis);

        Task<Issue> AssignIssueAsync(AssignIssueRequest uir, int userId);
        Task<Issue> AssignIssueBySlackAsync(AssignIssueRequestChatGpt uir, int userId);
        Task<IssueDto> RenameIssueAsync(RenameIssueRequest rir, int userId);
        Task<IssueDto> ChangeIssueStatusAsync(ChangeIssueStatusRequest req, int userId);
        Task<IssueDto> ChangeIssuePriorityAsync(ChangeIssuePriorityRequest req, int userId);
        Task<IssueDto> AssignTeamAsync(AssignTeamRequest req, int userId);
        Task<IssueDto> UpdateDueDateAsync(UpdateDueDateRequest req,  int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByUserId(int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByProjectId(int projectId);
        Task<int> GetIssueIdFromKey(string key);
        Task<IEnumerable<IssueDto>> GetAllIssues();
        Task deleteAllIssues();
        Task DeleteIssueByIdAsync(int id, int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByTeamId(int teamId);
        Task<Issue> UpdateDescriptionAsync(UpdateDescriptionRequest req, int userId);

    }
}
