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
        Task<IEnumerable<Issue>> GetAllAsync();
        Task<Issue> CreateIssueAsync(CreateIssueRequest cir);
        Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest scis);

        Task<bool> DeleteIssueAsync(int id);
        Task<Issue> AssignIssueAsync(AssignIssueRequest uir);
        Task<Issue> AssignIssueBySlackAsync(AssignIssueRequestChatGpt uir);
        Task<Issue> UpdateIssueAsync(Issue issue);
        Task<Project> GetProjectFromKey(string key);
        int GetIssueIdInsideProjectFromKey(string key);
        Task<IssueDto> RenameIssueAsync(RenameIssueRequest rir);
        Task<IssueDto> ChangeIssueStatusAsync(ChangeIssueStatusRequest req);
        Task<IssueDto> ChangeIssuePriorityAsync(ChangeIssuePriorityRequest req);
        Task<IssueDto> AssignTeamAsync(AssignTeamRequest req);
        Task<IssueDto> UpdateDueDateAsync(UpdateDueDateRequest req);
        Task<IEnumerable<IssueDto>> GetAllIssuesByUserId(int userId);
        Task<IEnumerable<IssueDto>> GetAllIssuesByProjectId(int projectId);
        Task<int> GetIssueIdFromKey(string key);
        Task<IEnumerable<IssueDto>> GetAllIssues();
        Task deleteAllIssues();
        Task deleteIssueById(int id);

    }
}
