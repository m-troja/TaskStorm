using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Model.DTO;

namespace TaskStorm.Service
{
    public interface IIssueService
    {
        Task<Issue> GetIssueByIdAsync(int id);
        Task<IssueDto> GetIssueDtoByIdAsync(int id);
        Task<IssueDto> GetIssueDtoByKeyAsync(string keyString);
        Task<Issue> CreateIssueAsync(CreateIssueRequest cir);
        Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest scis);

        Task<Issue> AssignIssueAsync(AssignIssueRequest uir);
        Task<Issue> AssignIssueBySlackAsync(AssignIssueRequestChatGpt uir);
        Task<IssueDto> RenameIssueAsync(RenameIssueRequest rir);
        Task<IssueDto> ChangeIssueStatusAsync(ChangeIssueStatusRequest req);
        Task<IssueDto> ChangeIssuePriorityAsync(ChangeIssuePriorityRequest req);
        Task<IssueDto> AssignTeamAsync(AssignTeamRequest req);
        Task<IssueDto> UpdateDueDateAsync(UpdateDueDateRequest req);
        Task<IEnumerable<IssueDto>> GetIssuesByUserId(int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByProjectId(int projectId);
        Task<int> GetIssueIdFromKey(string key);
        Task<IEnumerable<IssueDto>> GetAllIssues();
        Task deleteAllIssues();
        Task DeleteIssueByIdAsync(int id, int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByTeamId(int teamId);

    }
}
