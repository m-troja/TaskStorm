using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    short_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    salt = table.Column<byte[]>(type: "bytea", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    slack_user_id = table.Column<string>(type: "text", nullable: true),
                    disabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_inside_project = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    author_id = table.Column<int>(type: "integer", nullable: false),
                    assignee_id = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: true),
                    team_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_issues_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_issues_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "public",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_issues_users_assignee_id",
                        column: x => x.assignee_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_user",
                schema: "public",
                columns: table => new
                {
                    roles_id = table.Column<int>(type: "integer", nullable: false),
                    users_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_user", x => new { x.roles_id, x.users_id });
                    table.ForeignKey(
                        name: "fk_role_user_roles_roles_id",
                        column: x => x.roles_id,
                        principalSchema: "public",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_user_users_users_id",
                        column: x => x.users_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_user",
                schema: "public",
                columns: table => new
                {
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_user", x => new { x.team_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_team_user_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "public",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_user_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issue_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_activities_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "public",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<int>(type: "integer", nullable: false),
                    issue_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "public",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_comments_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "keys",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key_string = table.Column<string>(type: "text", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    issue_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_keys_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "public",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_keys_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_property_updated",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: false),
                    new_value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_property_updated", x => x.id);
                    table.ForeignKey(
                        name: "fk_activity_property_updated_activities_id",
                        column: x => x.id,
                        principalSchema: "public",
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "projects",
                columns: new[] { "id", "created_at", "description", "short_name" },
                values: new object[] { -1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Predefined dummy project", "Dummy" });

            migrationBuilder.InsertData(
                schema: "public",
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "ROLE_USER" },
                    { 2, "ROLE_ADMIN" }
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "users",
                columns: new[] { "id", "disabled", "email", "first_name", "last_name", "password", "refresh_token", "salt", "slack_user_id" },
                values: new object[] { -1, true, "system.user@tasksystem.com", "System User", "System User", "Password", null, new byte[] { 87, 32, 87, 61, 195, 168, 195, 148, 85, 195, 140, 45, 194, 167, 195, 130, 78, 195, 175, 94, 195, 142, 88 }, null });

            migrationBuilder.CreateIndex(
                name: "ix_activities_issue_id",
                schema: "public",
                table: "activities",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_author_id",
                schema: "public",
                table: "comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_issue_id",
                schema: "public",
                table: "comments",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_assignee_id",
                schema: "public",
                table: "issues",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_author_id",
                schema: "public",
                table: "issues",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_project_id",
                schema: "public",
                table: "issues",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_team_id",
                schema: "public",
                table: "issues",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_keys_issue_id",
                schema: "public",
                table: "keys",
                column: "issue_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_keys_project_id",
                schema: "public",
                table: "keys",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_user_users_id",
                schema: "public",
                table: "role_user",
                column: "users_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_user_user_id",
                schema: "public",
                table: "team_user",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_property_updated",
                schema: "public");

            migrationBuilder.DropTable(
                name: "comments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "keys",
                schema: "public");

            migrationBuilder.DropTable(
                name: "role_user",
                schema: "public");

            migrationBuilder.DropTable(
                name: "team_user",
                schema: "public");

            migrationBuilder.DropTable(
                name: "activities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "issues",
                schema: "public");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "public");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");
        }
    }
}
