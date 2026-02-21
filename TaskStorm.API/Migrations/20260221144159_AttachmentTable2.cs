using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "public",
                table: "attachments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "public",
                table: "attachments");
        }
    }
}
