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
        Task<Issue> HandleUpdateIssueRequestAsync(UpdateIssueRequest req, int userId);
        Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest scis);
        Task<Issue> AssignIssuesBySlackAsync(AssignIssueRequestChatGpt uir, int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByUserId(int userId);
        Task<IEnumerable<Issue>> GetIssuesBySlackUserId(string slackUserId);
        Task<IEnumerable<IssueDto>> GetIssuesByProjectId(int projectId);
        Task<int> GetIssueIdFromKey(string key);
        Task<IEnumerable<IssueDto>> GetAllIssues();
        Task DeleteAllIssues();
        Task DeleteIssueByIdAsync(int id, int userId);
        Task<IEnumerable<IssueDto>> GetIssuesByTeamId(int teamId);

    }
}
