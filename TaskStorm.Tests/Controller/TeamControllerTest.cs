using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller
{
    public class TeamControllerTest
    {
        private readonly Mock<IUserService> _userMock = new();
        private readonly Mock<ITeamService> _teamMock = new();
        private readonly CommentCnv _commentCnv;
        private readonly ProjectCnv _projectCnv;
        private readonly IssueCnv _issueCnv;
        private readonly TeamCnv _teamCnv;

        public TeamControllerTest()
        {
            var loggerFactory = LoggerFactory.Create(b => { });
            _commentCnv = new CommentCnv(loggerFactory.CreateLogger<CommentCnv>());
            _teamCnv = new TeamCnv(loggerFactory.CreateLogger<TeamCnv>());
            _issueCnv = new IssueCnv(
                _commentCnv,
                loggerFactory.CreateLogger<IssueCnv>(),
                _teamCnv
            );
            _projectCnv = new ProjectCnv(_issueCnv);
        }

        private TeamController CreateController()
        {
            return new TeamController( _userMock.Object,  _teamCnv, LoggerFactory.Create(b => { }).CreateLogger <TeamController>(), _teamMock.Object);
        }

        [Fact]
        public async Task GetTeamById_ShouldReturnTeam_WhenTeamExists()
        {
            // GIVEN
            var team = new Team("Team A") { Id = 1 };

            _teamMock.Setup(s => s.GetTeamByIdAsync(1)).ReturnsAsync(team);
            var controller = CreateController();

            // WHEN
            var result = await controller.GetTeamById(1);

            // THEN
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<TeamDto>(okResult.Value);
            Assert.Equal(team.Id, returnedDto.Id);
            Assert.Equal(team.Name, returnedDto.Name);
        }

        [Fact]
        public async Task GetAllTeams_ShouldReturnListOfTeams()
        {
            // GIVEN
            var teams = new List<Team>
            {
                new Team("Team A") { Id = 1 },
                new Team("Team B") { Id = 2 }
            };

            _teamMock.Setup(s => s.GetAllTeamsAsync()).ReturnsAsync(teams);
            var controller = CreateController();

            // WHEN
            var result = await controller.GetAllTeams();

            // THEN
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<TeamDto>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
            Assert.Equal("Team A", returnedList[0].Name);
            Assert.Equal("Team B", returnedList[1].Name);
        }

        [Fact]
        public async Task CreateTeam_ShouldReturnCreatedTeam()
        {
            // GIVEN
            var request = new CreateTeamRequest("Team C");
            var team = new Team("Team C") { Id = 3 };

            _teamMock.Setup(s => s.AddTeamAsync(request)).ReturnsAsync(team);
            var controller = CreateController();

            // WHEN
            var result = await controller.CreateTeam(request);

            // THEN
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<TeamDto>(okResult.Value);
            Assert.Equal(team.Id, returnedDto.Id);
            Assert.Equal(team.Name, returnedDto.Name);
        }

        [Fact]
        public async Task GetIssuesByTeamId_ShouldReturnListOfIssues()
        {
            // GIVEN

            var issues = BuildListOfIssueDto();
            _teamMock.Setup(s => s.GetIssuesByTeamId(1)).ReturnsAsync(issues);

            var controller = CreateController();

            // WHEN
            var result = await controller.GetIssuesByTeamId(1);

            // THEN
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedIssues = Assert.IsType<List<IssueDto>>(okResult.Value);
            Assert.Equal(2, returnedIssues.Count);
        }

        [Fact]
        public async Task GetUsersByTeamId_ShouldReturnListOfUsers()
        {
            // GIVEN
            var users = new List<UserDto>
            {
                new UserDto(1, "John", "Doe", "a@test.com", new List<string>(), new List<string>(), false, "SLACK1"),
                new UserDto(2, "Jane", "Doe", "b@test.com", new List<string>(), new List<string>(), false, "SLACK2")
            };

            _teamMock.Setup(s => s.GetUsersByTeamId(1)).ReturnsAsync(users);

            var controller = CreateController();

            // WHEN
            var result = await controller.GetUsersByTeamId(1);

            // THEN
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsType<List<UserDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
        }

        private List<IssueDto> BuildListOfIssueDto()
        {
            var issue1 = new IssueDto(1, "ISSUE-1", "Title1", "Desc1", IssueStatus.NEW,
                        IssuePriority.HIGH, 1, 2, DateTime.Parse("2025-11-22"), DateTime.Parse("2025-11-23"), DateTime.Parse("2025-11-24"),
                        new List<CommentDto>(), 1, new TeamDto(1, "New Team", new List<int>(1), new List<int>(1)));
            var issue2 = new IssueDto(2, "ISSUE-2", "Title2", "Desc2", IssueStatus.NEW,
                        IssuePriority.HIGH, 1, 2, DateTime.Parse("2025-11-25"), DateTime.Parse("2025-11-26"), DateTime.Parse("2025-11-27"),
                        new List<CommentDto>(), 1, new TeamDto(1, "New Team", new List<int>(1), new List<int>(1)));
            return new List<IssueDto> { issue1, issue2 };
        }
    }
}
