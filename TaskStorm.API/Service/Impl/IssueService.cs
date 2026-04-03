using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskStorm.Data;
using TaskStorm.Exception;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Entity.Masterdata;
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
    private readonly ILogger<IssueService> l;
    private readonly IActivityService _activityService;
    private readonly ISlackNotificationService _slackNotificationService;

    public IssueService(PostgresqlDbContext db, IUserService userService, CommentCnv commentCnv, IssueCnv issueCnv, ILogger<IssueService> l,
        IActivityService activityService, ISlackNotificationService slackNotificationService)
    {
        _db = db;
        _userService = userService;
        _commentCnv = commentCnv;
        _issueCnv = issueCnv;
        this.l = l;
        _activityService = activityService;
        _slackNotificationService = slackNotificationService;
    }

    public async Task<Issue> CreateIssueAsync(CreateIssueRequest req)
    {
        l.LogDebug($"Starting new issue creation for authorId: {req.AuthorId}, projectId: {req.ProjectId}");

        await createSystemUserId();

        var issue = await ValidateCreateIssueRequest(req);
        issue.Status = IssueStatus.NEW;

        using var transaction = await _db.Database.BeginTransactionAsync();
       
        try
        {
            int maxIdInsideProject = await _db.Issues
                .Where(i => i.ProjectId == issue.Project.Id)
                .MaxAsync(i => (int?)i.IdInsideProject) ?? 0;
            l.LogDebug($"Retrieved maxIdInsideProject from DB: {maxIdInsideProject}");
            l.LogDebug($"issue.Project.Id used in query: {issue.Project.Id}"); 
            l.LogDebug($"ProjectId used in query: {issue.ProjectId}");

            issue.IdInsideProject = maxIdInsideProject + 1;
            l.LogDebug($"issue.IdInsideProject: {issue.IdInsideProject}");

            await _db.Issues.AddAsync(issue);
            await _db.SaveChangesAsync();

            var key = new Key(issue.Project, issue);
            issue.Key = key;
            l.LogDebug($"issue.Key: {issue.Key}");

            await _db.Keys.AddAsync(key);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            
            l.LogDebug($"Created successfully Keystring: {issue.Key.KeyString},  IssueId: {issue.Id}, IdInsideProject: {issue.IdInsideProject}");

            var activity = await _activityService.CreateIssueAsync(issue.Id, issue.AuthorId);
        }
        catch (System.Exception )
        {
            l.LogDebug($"Error occurred while creating issue for project {issue.ProjectId}");
            await transaction.RollbackAsync();
            throw;
        }
        l.LogDebug($"Issue creation transaction completed successfully for issue ID {issue.Id}");

        await _slackNotificationService.SendIssueCreatedNotificationAsync(issue, issue.Author);

        l.LogDebug($"Sent issue created notification for issue ID {issue.Id}");

        return issue;
    }
    private async Task<Issue> ValidateCreateIssueRequest(CreateIssueRequest req)
    {
        // Project
        Project project = await ValidateProjectAsync(req.ProjectId);

        // Due Date
        DateTime? dueDateUtc = await ValidateDueDateAsync(req.DueDate ?? "2026-01-01");

        // Priority
        IssuePriority priorityToSet = await ValidatePriority(req.Priority ?? "NORMAL");

        // Author 
        User author = await GetUserByIdAsync(req.AuthorId);

        // Assignee
        User? assignee = await GetUserByIdAsync(req.AssigneeId ?? SystemUserId);

        // Title
        string title = await ValidateTitleAsync(req.Title);

        // Description
        string description = await ValidateDescriptionAsync(req.Description ?? "");

        // Issue object
        var issueValidated = new Issue
        {
            Title = title,
            Description = description,
            Priority = priorityToSet,
            AuthorId = author.Id,
            Author = author,
            Assignee = assignee,
            AssigneeId = assignee.Id,
            DueDate = dueDateUtc,
            Project = project,
            ProjectId = project.Id
        };
        return issueValidated;
    }

    private async Task<Project> ValidateProjectAsync(int projectId)
    {
        Project project;
        try
        {
            project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw new ProjectNotFoundException("Project was not found");
        }
        catch (ProjectNotFoundException e)
        {
            l.LogDebug($"ProjectId {projectId} was not found - assigned DummyProjectId={DummyProjectId}");
            project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == DummyProjectId) ?? throw new ProjectNotFoundException("DummyProject was not found");
        }
        return project;
    }
    private async Task<Team> ValidateTeam(int teamId)
    {
        Team team;
        try
        {
            team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId) ?? throw new ContentNotFoundException("Team was not found");
        }
        catch (ContentNotFoundException e)
        {
            l.LogDebug($"TeamId {teamId} was not found - assigned null");
            throw new BadRequestException("Team was not found");
        }
        return team;
    }
    private async Task<IssueStatus> ValidateStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return IssueStatus.NEW;
        if (!Enum.TryParse<IssueStatus>(status, true, out var parsedStatus) || !Enum.IsDefined(typeof(IssueStatus), parsedStatus))
        {
            l.LogDebug($"Invalid issue status: {status}, defaulting to OPEN");
            return IssueStatus.NEW;
        }
        return Enum.Parse<IssueStatus>(status);
    }

    private async Task<DateTime> ValidateDueDateAsync(string dueDdate)
    {
        if (string.IsNullOrEmpty(dueDdate)) return DateTime.UtcNow;
        if (!DateTime.TryParse(dueDdate, out var parsedDueDate))
        {
            l.LogDebug($"Invalid due date: {dueDdate}, defaulting to UtcNow");
            return DateTime.UtcNow;
        }
        return DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);

    }
    private async Task<IssuePriority> ValidatePriority(string priority)
    {
        if (string.IsNullOrEmpty(priority)) return IssuePriority.NORMAL;
        if (!Enum.TryParse<IssuePriority>(priority, true, out var parsedPriority) || !Enum.IsDefined(typeof(IssuePriority), parsedPriority))
        {
            l.LogDebug($"Invalid issue priority: {priority}, defaulting to NORMAL");
            return IssuePriority.NORMAL;
        }
        return Enum.Parse<IssuePriority>(priority);
    }

    private async Task<string> ValidateTitleAsync(string title)
    {
        if (string.IsNullOrEmpty(title)) throw new BadRequestException("Title cannot be empty");
        if (title.Length > 255) title = title.Substring(0, 255);
        return title;
    }
    private async Task<string> ValidateDescriptionAsync(string description)
    {
        if (description.Length > 1000) description = description.Substring(0, 1000);
        return description;
    }

    private async Task<List<MasterdataValue>> ValidateMasterData(IEnumerable<MasterdataValueRequest> masterdataToValidate)
    {
        List<MasterdataValue> validatedMasterdata = new List<MasterdataValue>();

        foreach (var masterdata in  masterdataToValidate)
        {
            if (string.IsNullOrEmpty(masterdata.Value)) throw new BadRequestException("Value cannot be empty");
            if ( !Enum.IsDefined(typeof(MasterdataType), masterdata.Type))
            {
                l.LogError($"Invalid MasterdataType: {masterdata.Value}");
                throw new BadRequestException($"Invalid MasterdataType: {masterdata.Type}");
            }
            var value = await _db.MasterdataValues.FirstOrDefaultAsync(v => v.Value == masterdata.Value && v.Type == masterdata.Type && v.Code == masterdata.Code) ?? throw new BadRequestException("Masterdata value was not found");
            validatedMasterdata.Add(value);
        }
        return validatedMasterdata;
    }

    public async Task<IssueDto> GetIssueDtoByIdAsync(int id)
    {
        l.LogDebug($"Fetching issue DTO for issueId {id}");
        var issue = await GetIssueFromDb(id) ;
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



        var issueDto = _issueCnv.EntityToDto(issue);
        return issueDto;
    }

    public async Task<Issue> GetIssueByIdAsync(int id)
    {
        l.LogDebug($"Fetching issue entity for issueId {id}");
        Issue? issue = await GetIssueFromDb(id); 

        l.LogDebug($"Fetched issue: {issue.Id}");

        return issue ?? throw new IssueNotFoundException("Issue " + id + " was not found");
    }
    public async Task<IssueDto> GetIssueDtoByKeyAsync(string keyString)
    {
        l.LogDebug($"Fetching issue DTO for key {keyString}");
        int issueId = GetIssueIdInsideProjectFromKey(keyString);

        var issue = await GetIssueFromDb(issueId);

        return _issueCnv.EntityToDto(issue);
    }

    public async Task<Issue> AssignIssueAsync(AssignIssueRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        l.LogDebug($"Assigning issue {req.IssueId} to user {req.AssigneeId}, event author={userId}");
        var newAssignee = await _db.Users.AnyAsync(u => u.Id == req.AssigneeId) ? await GetUserByIdAsync(req.AssigneeId) : throw new BadRequestException("Assignee user was not found") ;
        l.LogDebug($"Fetched new assignee: {newAssignee}");
        
        var issue = await GetIssueFromDb(req.IssueId);

        User? oldAssignee;
        if (issue.AssigneeId.HasValue && issue.AssigneeId != 0) {
            oldAssignee = await _db.Users.FirstOrDefaultAsync(u => u.Id == issue.AssigneeId); }
        else  {
            l.LogDebug("Old assignee was null or 0, assigning to system user for activity log");
            oldAssignee = await _db.Users.FirstOrDefaultAsync(u => u.Id == SystemUserId);
        };

        if (oldAssignee == null) {
            l.LogDebug("System user assignee was not found in database");
            throw new ServerException("System user assignee was not found in database");
        }

        issue.Assignee = newAssignee;
        issue.AssigneeId = newAssignee.Id;
        l.LogDebug($"Set assignee {issue.Assignee} for issue {issue.Id}");

        Issue updatedIssue = await UpdateIssueAsync(issue);
        l.LogDebug($"Updated issue {updatedIssue.Id} in database");

        var activity = await _activityService.UpdateAssigneeAsync(oldAssignee.Id, newAssignee.Id, issue.Id, userId);
        await _slackNotificationService.SendIssueAssignedNotificationAsync(issue, eventAuthorUser);
        l.LogDebug($"Sent issue assigned notification for issue {issue.Id} to ChatGPT");
        return updatedIssue;
    }
    public async Task<Issue> AssignIssueBySlackAsync(AssignIssueRequestChatGpt req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        l.LogDebug($"Assigning issue by Slack with key {req.key} to Slack user ID {req.slackUserId}");
        int issueId = await GetIssueIdFromKey(req.key);
        int newAssigneeId = await _userService.GetIdBySlackUserId(req.slackUserId);
        var assignIssueRequest = new AssignIssueRequest(issueId, newAssigneeId);
        var issue = await AssignIssueAsync(assignIssueRequest,userId);
        l.LogDebug($"Assigned issue {issueId} to user {newAssigneeId} successfully");
        return issue;
    }

    private int GetIssueIdInsideProjectFromKey(string key)
    {
        l.LogDebug($"Getting issueId from key {key}");
        int lastDash = key.LastIndexOf('-');
        string shortName = lastDash >= 0 ? key.Substring(0, lastDash) : key; // if no dash, take whole string
        string numberPart = lastDash >= 0 ? key.Substring(lastDash + 1) : "";
        int number = int.TryParse(numberPart, out int n) ? n : 0;
        l.LogDebug($"shortName {shortName}, numberPart {numberPart}");

        return number;
    }
    
    private async Task<Project> GetProjectFromKey(string key)
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

    private async Task<Issue> UpdateIssueAsync(Issue issue)
    {
        l.LogDebug($"Updating issue {issue.Id}");
        _db.Issues.Update(issue);
        await _db.SaveChangesAsync();
        return issue;
    }

    public async Task<IssueDto> RenameIssueAsync(RenameIssueRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }


        l.LogDebug($"Renaming issue {req.IssueId} to new title: {req.newTitle}");
        Issue issue = await GetIssueFromDb(req.IssueId);
        var oldTitle = issue.Title;
        issue.Title = req.newTitle;
        Issue updatedIssue = await UpdateIssueAsync(issue);
        l.LogDebug($"Renamed issue {updatedIssue.Id} successfully");
        _db.Issues.Update(updatedIssue);
        await _db.SaveChangesAsync();
        IssueDto issueDto = _issueCnv.EntityToDto(updatedIssue);

        var activity = await _activityService.UpdateTitleAsync(oldTitle, issue.Title, issue.Id, userId);
        await _slackNotificationService.SendUpdateTitleAsync(issue, eventAuthorUser);

        return issueDto;
    }

    public async Task<IssueDto> ChangeIssueStatusAsync(ChangeIssueStatusRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        l.LogDebug($"Changing status of issue {req.IssueId} to {req.NewStatus}");

        if (req.NewStatus == null) throw new ArgumentException("NewStatus cannot be null");
        if (req.IssueId <= 0) throw new ArgumentException("IssueId must be positive");
        if (!Enum.TryParse<IssueStatus>(req.NewStatus, true, out var newStatus)  || !Enum.IsDefined(typeof(IssueStatus), newStatus))
        {
            throw new ArgumentException($"Invalid issue status: {req.NewStatus}");
        }

        Issue issue = await GetIssueFromDb(req.IssueId);
        IssueStatus oldStatus = issue.Status;
        issue.Status = Enum.Parse<IssueStatus>(req.NewStatus);

        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.EntityToDto(UpdatedIssue);
        
        var activity = await _activityService.UpdateStatusAsync(oldStatus, issue.Status, issue.Id, userId);
        await _slackNotificationService.SendIssueStatusChangedNotificationAsync(issue, eventAuthorUser);

        return issueDto;
    }

    public async Task<IssueDto> ChangeIssuePriorityAsync(ChangeIssuePriorityRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        l.LogDebug($"Changing priority of issue {req.IssueId} to {req.NewPriority}");
        if (req.NewPriority == null) throw new ArgumentException("NewPriority cannot be empty");
        if (req.IssueId <= 0) throw new ArgumentException("IssueId must be greater than 0");
        if (!Enum.TryParse<IssuePriority>(req.NewPriority, true, out var newPriority) || !Enum.IsDefined(typeof(IssuePriority), newPriority))
        {
            throw new ArgumentException($"Invalid issue priority: {req.NewPriority}");
        }
        Issue issue = await GetIssueFromDb(req.IssueId);
        IssuePriority? oldPriority = issue.Priority;
        issue.Priority = Enum.Parse<IssuePriority>(req.NewPriority);
        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.EntityToDto(UpdatedIssue);
    
        var oldPriorityForActivity = oldPriority == null ? IssuePriority.NORMAL : oldPriority.Value;
        var newPriorityForActivity = issue.Priority == null ? IssuePriority.NORMAL : issue.Priority.Value;

        var activity = await _activityService.UpdatePriorityAsync(oldPriorityForActivity, newPriorityForActivity, issue.Id, userId);
        await _slackNotificationService.SendIssuePriorityChangedNotificationAsync(issue, eventAuthorUser);
        return issueDto;
    }

    public async Task<IssueDto> AssignTeamAsync(AssignTeamRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        l.LogDebug($"Assigning team {req.TeamId} to issue {req.IssueId}, userId={userId}");
        Team team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId) ?? throw new ContentNotFoundException("Team was not found");
        Issue issue = await GetIssueFromDb(req.IssueId);
        var oldTeamId = issue.TeamId.HasValue ? issue.TeamId.Value : -1;

        issue.Team = team;
        issue.TeamId = team.Id;

        var UpdatedIssue = await UpdateIssueAsync(issue);
        IssueDto issueDto = _issueCnv.EntityToDto(UpdatedIssue);
        l.LogDebug($"Assigned team {team.Name} to issue {issue.Id} successfully");

        var activity = await _activityService.UpdateTeamAsync(oldTeamId,  team.Id, issue.Id, userId);
        await _slackNotificationService.SendTeamAssignedNotificationAsync(issue, eventAuthorUser);
        return issueDto;
    }

    public async Task<IEnumerable<IssueDto>> GetIssuesByUserId(int userId)
    {
        l.LogDebug($"Getting all issues for userId {userId}");
        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            throw new UserNotFoundException("User not found");
        var issueIds = await _db.Issues.Where(i => i.AssigneeId == userId || i.AuthorId == userId).Select(i => i.Id).ToListAsync();

        var issues = await GetListOfIssuesFromDb(issueIds);

        l.LogDebug($"Fetched {issues.Count} issues for userId {userId}");
        var issuesDto = _issueCnv.EntityListToDtoList(issues);
        return issuesDto;
    }

    public async Task<IEnumerable<Issue>> GetIssuesBySlackUserId(string slackUserId)
    {
        l.LogDebug($"GetIssuesBySlackUserId for slackUserId {slackUserId}");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.SlackUserId == slackUserId) ?? throw new UserNotFoundException("User was not found by slackUserId");
        var issueIds = await _db.Issues.Where(i => i.AssigneeId == user.Id ).Select(i => i.Id).ToListAsync();

        var issues = await GetListOfIssuesFromDb(issueIds);

        l.LogDebug($"Fetched {issues.Count} issues for userId {user.Id} by slackUserId  {slackUserId}");
        return issues;
    }

    public async Task<IEnumerable<IssueDto>> GetIssuesByTeamId(int teamId)
    {
        l.LogDebug($"Getting all issues for teamId {teamId}");
        var team = await _db.Teams.FirstOrDefaultAsync( team => team.Id == teamId) ?? throw new ContentNotFoundException("Team was not found");

        var issueIds = await _db.Issues.Where(i => i.TeamId == teamId).Select(i => i.Id).ToListAsync();
        var issues = await GetListOfIssuesFromDb(issueIds);

        l.LogDebug($"Fetched {issues.Count} issues for userId {teamId}");
        var issuesDto = _issueCnv.EntityListToDtoList(issues);
        return issuesDto;
    }
    public async Task<IEnumerable<IssueDto>> GetIssuesByProjectId(int projectId)
    {
        l.LogDebug($"Getting all issues for projectId {projectId}");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw new ProjectNotFoundException("Project was not found");
        var issueIds = await _db.Issues.Where(i => i.ProjectId == projectId).Select(i => i.Id).ToListAsync();
        var issues = await GetListOfIssuesFromDb(issueIds);
        l.LogDebug($"Fetched {issues.Count} issues for projectId {projectId}");
        var issuesDto = _issueCnv.EntityListToDtoList(issues);
        return issuesDto;
    }

    public async Task<int> GetIssueIdFromKey(string key)
    {
        int IssueIdInProject = GetIssueIdInsideProjectFromKey(key);
        if (IssueIdInProject == 0) throw new IssueNotFoundException("Issue was not found");
        Project project = await GetProjectFromKey(key);
        int issueId = await _db.Issues.Where(i => i.ProjectId == project.Id && i.IdInsideProject == IssueIdInProject).Select(i => i.Id).FirstOrDefaultAsync();
        l.LogDebug($"Fetched issueId {issueId} from key {key}");
        return issueId;
    }

    public async Task<IssueDto> UpdateDueDateAsync(UpdateDueDateRequest req, int userId)
    {
        var eventAuthorUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (eventAuthorUser == null)
        {
            l.LogDebug($"Event author user with ID {userId} was not found");
            throw new BadRequestException("Event author user was not found");
        }

        var dueDateUtc = DateTime.SpecifyKind(req.DueDate.Value, DateTimeKind.Utc);
        var issue = await GetIssueFromDb(req.IssueId);
        issue.DueDate = dueDateUtc;
        l.LogDebug($"Set due date {issue.DueDate} for issue {issue.Id}");
        var updatedIssue = await UpdateIssueAsync(issue);
        await _slackNotificationService.SendIssueDueDateUpdatedNotificationAsync(updatedIssue, eventAuthorUser);
        
        var oldDueDate = issue.DueDate.HasValue ? issue.DueDate.Value : DateTime.MinValue;
        var newDueDate = updatedIssue.DueDate.HasValue ? updatedIssue.DueDate.Value : DateTime.MinValue;

        var activity = await _activityService.UpdateDueDateAsync(oldDueDate , newDueDate, issue.Id, userId);
        return _issueCnv.EntityToDto(updatedIssue);
    }

    public async Task<IssueDtoChatGpt> CreateIssueBySlackAsync(SlackCreateIssueRequest req)
    {
        l.LogDebug("Creating issue via Slack with request: " + req);
        l.LogDebug("Fetching author and assignee IDs from Slack user IDs");
        int authorId = await _userService.GetIdBySlackUserId(req.authorSlackId);
        int assigneeId = -1;
        if (req.assigneeSlackId != null)
        {
            assigneeId = await _userService.GetIdBySlackUserId(req.assigneeSlackId);
        }
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
        var issue = await CreateIssueAsync(createIssueRequest);
        var issueDto = _issueCnv.EntityToIssueDtoChatGpt(issue);
        l.LogDebug($"Created issue via Slack successfully: {issueDto}");
        return issueDto;
    }

    public async Task<IEnumerable<IssueDto>> GetAllIssues()
    {
        var issueIds = await _db.Issues.Select(i => i.Id).ToListAsync();
        var issues = await GetListOfIssuesFromDb(issueIds);

        List<IssueDto> issueDtos = _issueCnv.EntityListToDtoList(issues).ToList();
        l.LogDebug($"GetAllIssues Fetched total {issueDtos.Count} issues from database");
        return issueDtos;
    }

    public async Task DeleteAllIssues()
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Issues");
        l.LogInformation("Deleted all issues from database");
    }

    public async Task DeleteIssueByIdAsync(int id, int userId)
    {
        var issue = await GetIssueFromDb(id);
        if (issue == null)
        {
            throw new IssueNotFoundException("Issue was not found - skip deleting issue");
        }
        var author = await  _db.Users.FirstOrDefaultAsync(u => u.Id == issue.AuthorId);
        if (author == null)
        {
            throw new BadRequestException("Author user was not found - skip deleting issue");
        }


         _db.Issues.Remove(issue);
        await _db.SaveChangesAsync();
        
        l.LogInformation($"Deleted issue where Id={id}");
        await _slackNotificationService.SendIssueDeletedNotificationAsync(issue, author);
        l.LogInformation($"Sent issue deleted notification for issue ID {id} to ChatGPT");

    }

    public async Task<Issue> UpdateDescriptionAsync( UpdateDescriptionRequest req, int userId)
    {
        var issue = await GetIssueFromDb(req.issueId);
        if (issue == null)
        {
            throw new IssueNotFoundException("Issue was not found - skip deleting issue");
        }
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var oldDesc = issue.Description;

        issue.Description = req.newDescription;
        
        var updatedIssue = await UpdateIssueAsync(issue);
        await _slackNotificationService.SendUpdateDescriptionAsync(updatedIssue, user);

        var activity = await _activityService.UpdateDescriptionAsync(oldDesc, issue.Description, issue.Id, userId);

        return updatedIssue;
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

    private async Task<Issue> GetIssueFromDb(int id)
    {
        l.LogDebug($"Fetching issue entity for issueId {id}");
        var issue = await _db.Issues
            .Include(i => i.Key)
            .Include(i => i.Author)
            .Include(i => i.Assignee)
            .Include(i => i.Project)
            .Include(i => i.Team).ThenInclude(t => t.Users)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .Include(i => i.Comments).ThenInclude(c => c.Attachments)
            .Include(i => i.Labels)
            .FirstOrDefaultAsync(i => i.Id == id);
        l.LogDebug($"Fetched issue: {issue}");

        return issue ?? throw new IssueNotFoundException("Issue " + id + " was not found");
    }

    private async Task<List<Issue>> GetListOfIssuesFromDb(List<int> ids)
    {
        l.LogDebug($"GetListOfIssuesFromDb for issueIds {string.Join(", ", ids)}");
        var issues = await _db.Issues
            .Where(i => ids.Contains(i.Id))
            .Include(i => i.Key)
            .Include(i => i.Author)
            .Include(i => i.Assignee)
            .Include(i => i.Project)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .Include(i => i.Comments).ThenInclude(c => c.Attachments)
            .Include(i => i.Team)
            .ToListAsync();
        return issues;
    }

    public async Task<Issue> HandleUpdateIssueRequestAsync(UpdateIssueRequest req, int userId)
    {
        l.LogInformation($"Updating issue {req.IssueId}");

        var issue = await GetIssueFromDb(req.IssueId);
        var userEventAuthor = await GetUserByIdAsync(userId);
        List<MasterdataValue> newMasterdata = new List<MasterdataValue>();

        var changes = new IssueChanges
        {
            OldTitle = issue.Title,
            OldDescription = issue.Description,
            OldStatus = issue.Status,
            OldPriority = issue.Priority,
            OldAssigneeId = issue.AssigneeId,
            OldTeamId = issue.TeamId,
            OldDueDate = issue.DueDate,
            OldLabels = issue.Labels != null ? issue.Labels.Where(v => v.Type == MasterdataType.ISSUE_LABEL).ToList() : new List<MasterdataValue>()
        };

        if (req.Title != null)
            issue.Title = await ValidateTitleAsync(req.Title);

        if (req.Description != null)
            issue.Description = await ValidateDescriptionAsync(req.Description);

        if (req.Priority != null)
            issue.Priority = await ValidatePriority(req.Priority);

        if (req.Status != null)
            issue.Status = await ValidateStatus(req.Status);

        if (req.AssigneeId.HasValue)
        {
            issue.Assignee = await GetUserByIdAsync(req.AssigneeId.Value);
            issue.AssigneeId = req.AssigneeId.Value;
        }

        if (req.DueDate != null)
            issue.DueDate = await ValidateDueDateAsync(req.DueDate);

        if (req.TeamId.HasValue)
        {
            var team = await ValidateTeam(req.TeamId.Value);
            issue.Team = team;
            issue.TeamId = team.Id;
        }
        if (req.MasterDataValues != null)
        {
            newMasterdata = await ValidateMasterData(req.MasterDataValues);
            issue.Labels = newMasterdata.Where(  v => v.Type == MasterdataType.ISSUE_LABEL ).ToList();
        }


        await UpdateIssueAsync(issue);

        changes.NewTitle = issue.Title;
        changes.NewDescription = issue.Description;
        changes.NewStatus = issue.Status;
        changes.NewPriority = issue.Priority;
        changes.NewAssigneeId = issue.AssigneeId;
        changes.NewTeamId = issue.TeamId;
        changes.NewDueDate = issue.DueDate;
        changes.NewLabels = newMasterdata;

        await ProcessIssueChanges(issue, changes, userEventAuthor);

        return issue;
    }

    private (List<MasterdataValue> added, List<MasterdataValue> removed) GetLabelChanges(ICollection<MasterdataValue> oldLabels, ICollection<MasterdataValue> newLabels)
    {
        var oldIds = oldLabels.Select(x => x.Id).ToHashSet();
        var newIds = newLabels.Select(x => x.Id).ToHashSet();

        var added = newLabels.Where(x => !oldIds.Contains(x.Id)).ToList();
        var removed = oldLabels.Where(x => !newIds.Contains(x.Id)).ToList();

        return (added, removed);
    }

    private async Task ProcessIssueChanges(Issue issue, IssueChanges c, User eventAuthor)
    {
        if (c.OldTitle != c.NewTitle)
        {
            await _activityService.UpdateTitleAsync(c.OldTitle, c.NewTitle, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendUpdateTitleAsync(issue, eventAuthor);
        }

        if (c.OldDescription != c.NewDescription)
        {
            await _activityService.UpdateDescriptionAsync(c.OldDescription, c.NewDescription, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendUpdateDescriptionAsync(issue, eventAuthor);
        }

        if (c.OldStatus != c.NewStatus)
        {
            await _activityService.UpdateStatusAsync(c.OldStatus.Value, c.NewStatus.Value, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendIssueStatusChangedNotificationAsync(issue, eventAuthor);
        }

        if (c.OldPriority != c.NewPriority)
        {
            await _activityService.UpdatePriorityAsync(c.OldPriority.Value, c.NewPriority.Value, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendIssuePriorityChangedNotificationAsync(issue, eventAuthor);
        }

        if (c.OldAssigneeId != c.NewAssigneeId)
        {
            await _activityService.UpdateAssigneeAsync(c.OldAssigneeId ?? -1, c.NewAssigneeId ?? -1, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendIssueAssignedNotificationAsync(issue, eventAuthor);
        }

        if (c.OldTeamId != c.NewTeamId)
        {
            await _activityService.UpdateTeamAsync(c.OldTeamId ?? -1, c.NewTeamId ?? -1, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendTeamAssignedNotificationAsync(issue, eventAuthor);
        }

        if (c.OldDueDate != c.NewDueDate)
        {
            await _activityService.UpdateDueDateAsync(c.OldDueDate ?? DateTime.MinValue, c.NewDueDate ?? DateTime.MinValue, issue.Id, eventAuthor.Id);
            await _slackNotificationService.SendIssueDueDateUpdatedNotificationAsync(issue, eventAuthor);
        }
        var (addedLabels, removedLabels) = GetLabelChanges(c.OldLabels, c.NewLabels);

        foreach (var label in addedLabels)
        {
            await _activityService.CreateLabelAsync(issue.Id, label, eventAuthor.Id);
        }

        foreach (var label in removedLabels)
        {
            await _activityService.DeleteLabelAsync(issue.Id, label, eventAuthor.Id);
        }
    }

    private async Task<User> GetUserByIdAsync(int userId)
    {
        l.LogDebug($"Fetching user with ID {userId}");
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new UserNotFoundException("User was not found");

    }

    public Task<Issue> AssignIssuesBySlackAsync(AssignIssueRequestChatGpt uir, int userId)
    {
        throw new NotImplementedException();
    }

}
