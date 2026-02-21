using Microsoft.EntityFrameworkCore;
using System.Text;
using TaskStorm.Data;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service.Impl;

public class IssueService : IIssueService
{
    private int SystemUserId = -1;
    private int DummyProjectId = -1;
    private readonly PostgresqlDbContext _db;
    private readonly CommentCnv _commentCnv;
    private readonly IssueCnv _issueCnv;
    private readonly IUserService _userService;
    private readonly IProjectService _projectService;
    private readonly ILogger<IssueService> l;
    private readonly ITeamService _teamService;
    private readonly IActivityService _activityService;
    private readonly ISlackNotificationService _slackNotificationService;

    public IssueService(PostgresqlDbContext db, IUserService userService, CommentCnv commentCnv, IssueCnv issueCnv, IProjectService projectService, ILogger<IssueService> l, ITeamService teamService, 
        IActivityService activityService, ISlackNotificationService slackNotificationService)
    {
        _db = db;
        _userService = userService;
        _commentCnv = commentCnv;
        _issueCnv = issueCnv;
        _projectService = projectService;
        this.l = l;
        _teamService = teamService;
        _activityService = activityService;
        _slackNotificationService = slackNotificationService;
    }

    public async Task<Issue> CreateIssueAsync(CreateIssueRequest req)
    {
        await createSystemUserId();

        l.LogDebug($"Starting issue creation for authorId: {req.assigneeId}, projectId: {req.projectId}");

        User author = await _userService.GetByIdAsync(req.authorId);
        int AuthorIdToSet = req.authorId != 0 ? req.authorId : SystemUserId;
        var authorToSet = author != null ? author : await _userService.GetByIdAsync(SystemUserId);
        User ? assignee = req.assigneeId.HasValue ? await _userService.GetByIdAsync(req.assigneeId.Value) : await _userService.GetByIdAsync(SystemUserId);

        DateTime? dueDateUtc = null;
        if (!string.IsNullOrEmpty(req.dueDate))
        {
            dueDateUtc = DateTime.SpecifyKind(DateTime.Parse(req.dueDate), DateTimeKind.Utc);
            l.LogDebug($"Parsed due date UTC: {dueDateUtc}");
        }

        IssuePriority priorityToSet = IssuePriority.NORMAL;
        if (!string.IsNullOrEmpty(req.priority))
        {
            if (!Enum.TryParse<IssuePriority>(req.priority, true, out var parsedPriority) || !Enum.IsDefined(typeof(IssuePriority), parsedPriority))
            {
                l.LogDebug($"Invalid issue priority: {req.priority}");
            }
            else
            {
                priorityToSet = Enum.Parse<IssuePriority>(req.priority);
            }
        }

        Project project;
        try
        {
            project = await _projectService.GetProjectById(req.projectId.Value);
        }
        catch (ProjectNotFoundException e)
        {
            l.LogDebug($"ProjectId {req.projectId} was not found - assigned DummyProjectId={DummyProjectId}");
            project = await _projectService.GetProjectById(DummyProjectId);
        }

        l.LogDebug($"Retrieved project from DB: {project}");

        Issue issue;

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {

            int maxIdInsideProject = await _db.Issues
                .Where(i => i.ProjectId == project.Id)
                .MaxAsync(i => (int?)i.IdInsideProject) ?? 0;
            l.LogDebug($"Retrieved maxIdInsideProject from DB: {maxIdInsideProject}");

            int nextIdInsideProject = maxIdInsideProject + 1;

            issue = new Issue
            {
                Title = req.title,
                Description = req.description,
                Priority = priorityToSet,
                Author = authorToSet,
                AuthorId = AuthorIdToSet,
                Assignee = assignee,
                AssigneeId = assignee?.Id,
                DueDate = dueDateUtc,
                ProjectId = project.Id,
                IdInsideProject = nextIdInsideProject
            };

            l.LogDebug($"Defined new issue entity: {issue}");

            _db.Issues.Add(issue);
            await _db.SaveChangesAsync();

            l.LogDebug($"Issue of ID {issue.Id} created successfully");
            
            var key = new Key(project, issue);
            issue.Key = key;

            _db.Keys.Add(key);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            
            l.LogDebug($"Keystring {issue.Key.KeyString} for id {issue.Id} and IdInsideProject {issue.IdInsideProject}");

            var activity = await _activityService.CreateActivityPropertyCreatedAsync(ActivityType.CREATED_ISSUE, issue.Id);
        }
        catch (System.Exception )
        {
            l.LogDebug($"Error occurred while creating issue for project {req.projectId}");
            await transaction.RollbackAsync();
            throw;
        }
        l.LogDebug($"Issue creation transaction completed successfully for issue ID {issue.Id}");
        await _slackNotificationService.SendIssueCreatedNotificationAsync(issue);
        l.LogDebug($"Sent issue created notification for issue ID {issue.Id}");

        return issue;
    }

    public Task<bool> DeleteIssueAsync(int id)
    {
        l.LogDebug($"DeleteIssueAsync is not implemented yet for issueId {id}");
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Issue>> GetAllAsync()
    {
        l.LogDebug($"GetAllAsync is not implemented yet");
        throw new NotImplementedException();
    }

    public async Task<IssueDto> GetIssueDtoByIdAsync(int id)
    {
        l.LogDebug($"Fetching issue DTO for issueId {id}");
        Issue issue = await GetIssueByIdAsync(id);
        l.LogDebug($"Fetching done");

        if (issue.Key == null)
        {
            l.LogDebug($"issue.key is null :(");
            throw new System.Exception("issue.key is null :(");
        }
        if(issue.Key.KeyString == null)
        {
            l.LogDebug("issue.Key.KeyString is null :(");
            throw new System.Exception("issue.Key.KeyString is null :(");
        }
        l.LogDebug($"Key is {issue.Key}, keystring {issue.Key.KeyString}");



        var issueDto = _issueCnv.ConvertIssueToIssueDto(issue);
        return issueDto;
    }

    public async Task<Issue> GetIssueByIdAsync(int id)
    {
        l.LogDebug($"Fetching issue entity for issueId {id}");
        Issue? issue = await _db.Issues
                                  .Include(i => i.Key)      
                                  .Include(i => i.Author)
                                  .Include(i => i.Assignee)
                                  .Include(i => i.Project)  
                                  .Include(i => i.Comments)  
                                  .FirstOrDefaultAsync(i => i.Id == id); 
        l.LogDebug($"Fetched issue: {issue}");

        return issue ?? throw new IssueNotFoundException("Issue " + id + " was not found");
    }
    public async Task<IssueDto> GetIssueDtoByKeyAsync(string keyString)
    {
        l.LogDebug($"Fetching issue DTO for key {keyString}");
        int issueId = GetIssueIdInsideProjectFromKey(keyString);
        Project? project = await GetProjectFromKey(keyString);

        Issue? issue = await _db.Issues
            .Where(i => i.Id == issueId)
            .Where(i => i.Project != null && i.Project.ShortName == project.ShortName)
            .Include(i => i.Key)
            .Include(i => i.Project)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync();
        l.LogDebug($"Fetched issue: {issue}");
        if (issue == null) throw new IssueNotFoundException("Issue was not found");
        l.LogDebug($"issue.Key {issue.Key}");

        IssueDto issueDto = _issueCnv.ConvertIssueToIssueDto(issue);
        l.LogDebug($"Converted issue to DTO: {issueDto}");
        return issueDto;
    }

    public async Task<Issue> AssignIssueAsync(AssignIssueRequest air)
    {
        l.LogDebug($"Assigning issue {air.IssueId} to user {air.AssigneeId}");
        User newAssignee = await _userService.GetByIdAsync(air.AssigneeId);
        l.LogDebug($"Fetched new assignee: {newAssignee}");
        Issue issue = await GetIssueByIdAsync(air.IssueId);
        User? oldAssignee = null;
        if (issue.AssigneeId.HasValue && issue.AssigneeId != 0) {
            oldAssignee = await _userService.GetByIdAsync(issue.AssigneeId.Value);  }
        else  {
            l.LogDebug("Old assignee was null or 0, assigning to system user for activity log");
            oldAssignee = await _userService.GetByIdAsync(SystemUserId);
        };
        issue.Assignee = newAssignee;
        issue.AssigneeId = newAssignee.Id;
        l.LogDebug($"Set assignee {issue.Assignee} for issue {issue.Id}");

        Issue updatedIssue = await UpdateIssueAsync(issue);
        l.LogDebug($"Updated issue {updatedIssue.Id} in database");

        var activity = await _activityService.CreateActivityPropertyUpdatedAsync(ActivityType.UPDATED_ASSIGNEE, (oldAssignee.Id).ToString(), (newAssignee.Id).ToString(), issue.Id);
        l.LogDebug($"Preparing to send issue assigned notification for issue {issue.Id} to ChatGPT");
        await _slackNotificationService.SendIssueAssignedNotificationAsync(issue);
        l.LogDebug($"Sent issue assigned notification for issue {issue.Id} to ChatGPT");
        return updatedIssue;
    }
    public async Task<Issue> AssignIssueBySlackAsync(AssignIssueRequestChatGpt req)
    {
        l.LogDebug($"Assigning issue by Slack with key {req.key} to Slack user ID {req.slackUserId}");
        int issueId = await GetIssueIdFromKey(req.key);
        int newAssigneeId = await _userService.GetIdBySlackUserId(req.slackUserId);
        var assignIssueRequest = new AssignIssueRequest(issueId, newAssigneeId);
        var issue = await AssignIssueAsync(assignIssueRequest);
        l.LogDebug($"Assigned issue {issueId} to user {newAssigneeId} successfully");
        return issue;
    }

    public int GetIssueIdInsideProjectFromKey(string key)
    {
        l.LogDebug($"Getting issueId from key {key}");
        int lastDash = key.LastIndexOf('-');
        string shortName = lastDash >= 0 ? key.Substring(0, lastDash) : key; // if no dash, take whole string
        string numberPart = lastDash >= 0 ? key.Substring(lastDash + 1) : "";
        int number = int.TryParse(numberPart, out int n) ? n : 0;
        l.LogDebug($"shortName {shortName}, numberPart {numberPart}");

        return number;
    }
    
    public async Task<Project> GetProjectFromKey(string key)
    {
        l.LogDebug($"Getting project from key {key}");
        int lastDash = key.LastIndexOf('-');
        string shortName = lastDash >= 0 ? key.Substring(0, lastDash) : key; // if no dash, take whole string
        Project? project = await _db.Projects
            .Where(p => p.ShortName == shortName)
            .FirstOrDefaultAsync() ?? throw new ProjectNotFoundException("Project was not found");
        l.LogDebug($"Found project with ID {project.Id} for key {key}");
        return project;
    }

    public async Task<Issue> UpdateIssueAsync(Issue issue)
    {
        l.LogDebug($"Updating issue {issue.Id}");
        issue.UpdatedAt = DateTime.UtcNow;
        _db.Issues.Update(issue);
        await _db.SaveChangesAsync();
        return issue;
    }

    public async Task<IssueDto> RenameIssueAsync(RenameIssueRequest rir)
    {
        l.LogDebug($"Renaming issue {rir.id} to new title: {rir.newTitle}");
        Issue issue = await GetIssueByIdAsync(rir.id);
        issue.Title = rir.newTitle;
        Issue updatedIssue = await UpdateIssueAsync(issue);
        l.LogDebug($"Renamed issue {updatedIssue.Id} successfully");
        _db.Issues.Update(updatedIssue);
        await _db.SaveChangesAsync();
        IssueDto issueDto = _issueCnv.ConvertIssueToIssueDto(updatedIssue);
        return issueDto;
    }

    public async Task<IssueDto> ChangeIssueStatusAsync(ChangeIssueStatusRequest req)
    {
        l.LogDebug($"Changing status of issue {req.IssueId} to {req.NewStatus}");

        if (req.NewStatus == null) throw new ArgumentException("NewStatus cannot be null");
        if (req.IssueId <= 0) throw new ArgumentException("IssueId must be positive");
        if (!Enum.TryParse<IssueStatus>(req.NewStatus, true, out var newStatus)  || !Enum.IsDefined(typeof(IssueStatus), newStatus))
        {
            throw new ArgumentException($"Invalid issue status: {req.NewStatus}");
        }

        Issue issue = await GetIssueByIdAsync(req.IssueId);
        IssueStatus oldStatus = issue.Status;
        issue.Status = Enum.Parse<IssueStatus>(req.NewStatus);

        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.ConvertIssueToIssueDto(UpdatedIssue);
        
        var activity = await _activityService.CreateActivityPropertyUpdatedAsync(ActivityType.UPDATED_STATUS, ((int)oldStatus).ToString(), ((int)issue.Status).ToString(), issue.Id);
        await _slackNotificationService.SendIssueStatusChangedNotificationAsync(issue);
        return issueDto;
    }

    public async Task<IssueDto> ChangeIssuePriorityAsync(ChangeIssuePriorityRequest req)
    {
        l.LogDebug($"Changing priority of issue {req.IssueId} to {req.NewPriority}");
        if (req.NewPriority == null) throw new ArgumentException("NewPriority cannot be empty");
        if (req.IssueId <= 0) throw new ArgumentException("IssueId must be greater than 0");
        if (!Enum.TryParse<IssuePriority>(req.NewPriority, true, out var newPriority) || !Enum.IsDefined(typeof(IssuePriority), newPriority))
        {
            throw new ArgumentException($"Invalid issue priority: {req.NewPriority}");
        }
        Issue issue = await GetIssueByIdAsync(req.IssueId);
        IssuePriority? oldPriority = issue.Priority;
        issue.Priority = Enum.Parse<IssuePriority>(req.NewPriority);
        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.ConvertIssueToIssueDto(UpdatedIssue);
        var activity = await _activityService.CreateActivityPropertyUpdatedAsync(
            ActivityType.UPDATED_PRIORITY, 
            oldPriority.HasValue ? ((int)oldPriority.Value).ToString() : "-1", 
            ((int)issue.Priority).ToString(), 
            issue.Id);
        await _slackNotificationService.SendIssuePriorityChangedNotificationAsync(issue);
        return issueDto;
    }

    public async Task<IssueDto> AssignTeamAsync(AssignTeamRequest req)
    {
        l.LogDebug($"Assigning team {req.TeamId} to issue {req.IssueId}");
        Team team = await _teamService.GetTeamByIdAsync(req.TeamId);
        Issue issue = await GetIssueByIdAsync(req.IssueId);
        issue.Team = team;
        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.ConvertIssueToIssueDto(UpdatedIssue);
        l.LogDebug($"Assigned team {team.Name} to issue {issue.Id} successfully");
        return issueDto;
    }

    public async Task<IEnumerable<IssueDto>> GetAllIssuesByUserId(int userId)
    {
        l.LogDebug($"Getting all issues for userId {userId}");
        User user = await _userService.GetByIdAsync(userId);
        List<Issue> issues = await _db.Issues.Where(i => i.AssigneeId == userId || i.AuthorId == userId)
            .Include(i => i.Key)
            .Include(i => i.Project)
            .Include(i => i.Comments)
            .ToListAsync();
        l.LogDebug($"Fetched {issues.Count} issues for userId {userId}");
        var issuesDto = _issueCnv.ConvertIssueListToIssueDtoList(issues);
        return issuesDto;
    }

    public async Task<IEnumerable<IssueDto>> GetAllIssuesByProjectId(int projectId)
    {
        l.LogDebug($"Getting all issues for projectId {projectId}");
        Project project = await _projectService.GetProjectById(projectId);
        List<Issue> issues = await _db.Issues.Where(i => i.ProjectId == projectId )
            .Include(i => i.Key)
            .Include(i => i.Project)
            .Include(i => i.Comments)
            .ToListAsync();
        l.LogDebug($"Fetched {issues.Count} issues for projectId {projectId}");
        var issuesDto = _issueCnv.ConvertIssueListToIssueDtoList(issues);
        return issuesDto;
    }

    public async Task<int> GetIssueIdFromKey(string key)
    {
        int IssueIdInProject = GetIssueIdInsideProjectFromKey(key);
        if (IssueIdInProject == 0) throw new IssueNotFoundException("Issue was not found");
        Project project = await GetProjectFromKey(key);
        Issue issue = await _db.Issues
            .Where(i => i.ProjectId == project.Id && i.IdInsideProject == IssueIdInProject)
            .FirstOrDefaultAsync() ?? throw new ArgumentNullException("Issue was not found") ;
        if (issue == null) throw new IssueNotFoundException("Issue was not found");
        return issue.Id;
    }

    public async Task<IssueDto> UpdateDueDateAsync(UpdateDueDateRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.IssueId <= 0)  throw new ArgumentException("IssueId must be positive.", nameof(req.IssueId));
        if (!req.DueDate.HasValue) throw new ArgumentException("DueDate cannot be null.", nameof(req.DueDate));

        DateTime dueDateUtc = DateTime.SpecifyKind(req.DueDate.Value, DateTimeKind.Utc);
        Issue issue = await GetIssueByIdAsync(req.IssueId);
        issue.DueDate = dueDateUtc;
        l.LogDebug($"Set due date {issue.DueDate} for issue {issue.Id}");
        Issue updatedIssue = await UpdateIssueAsync(issue);
        await _slackNotificationService.SendIssueDueDateUpdatedNotificationAsync(updatedIssue);
        return _issueCnv.ConvertIssueToIssueDto(updatedIssue);
    }

    public async Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest req)
    {
        l.LogDebug("Creating issue via Slack with request: " + req);
        l.LogDebug("Fetching author and assignee IDs from Slack user IDs");
        int authorId = await _userService.GetIdBySlackUserId(req.authorSlackId);
        int assigneeId = await _userService.GetIdBySlackUserId(req.assigneeSlackId);
        l.LogDebug($"Fetched authorId: {authorId}, assigneeId: {assigneeId}");
        var createIssueRequest = new CreateIssueRequest(
             req.title,
             req.description,
             req.priority,
             authorId,
             assigneeId,
             req.dueDate,
             req.projectId != null ? req.projectId.Value : DummyProjectId   
         );
        Issue issue = await CreateIssueAsync(createIssueRequest);
        var issueDto = _issueCnv.ConvertIssueToIssueDtoChatGpt(issue);
        l.LogDebug($"Created issue via Slack successfully: {issueDto}");
        return issueDto;
    }

    public async Task<IEnumerable<IssueDto>> GetAllIssues()
    {
        List<Issue> issues = await _db.Issues
            .Include(i => i.Key)
            .Include(i => i.Project)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .ToListAsync();
        List<IssueDto> issueDtos = _issueCnv.ConvertIssueListToIssueDtoList(issues).ToList();
        l.LogDebug($"Fetched total {issueDtos.Count} issues from database");
        return issueDtos;
    }

    public async Task deleteAllIssues()
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Issues");
        l.LogInformation("Deleted all issues from database");
    }

    public async Task deleteIssueById(int id)
    {
        await _db.Database.ExecuteSqlAsync($"DELETE FROM Issues WHERE id = {id}");
        l.LogInformation($"Deleted issue where Id={id}");
    }

    private async Task createSystemUserId()
    {
        l.LogInformation("Creating system user!");
        User? user;
        if (await _db.Users.FirstOrDefaultAsync(u => u.Id == SystemUserId) == null)
        {
            user = new User { Id = -1, FirstName = "System User", LastName = "System User", Email = "system.user@tasksystem.com", Password = "Password", Salt = Encoding.UTF8.GetBytes("W W=èÔUÌ-§ÂNï^ÎX"), Disabled = true };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
            l.LogInformation("Created system user");
        }
    }
}
