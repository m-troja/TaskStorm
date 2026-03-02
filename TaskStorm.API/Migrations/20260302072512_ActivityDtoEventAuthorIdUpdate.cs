using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class ActivityDtoEventAuthorIdUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "author_id",
                schema: "public",
                table: "activities");

            migrationBuilder.AddColumn<int>(
                name: "event_author_user_id",
                schema: "public",
                table: "activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_author_user_id",
                schema: "public",
                table: "activities");

            migrationBuilder.AddColumn<int>(
                name: "author_id",
                schema: "public",
                table: "activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                schema: "public",
                table: "activities",
                type: "integer",
                nullable: true);
        }
    }
}
