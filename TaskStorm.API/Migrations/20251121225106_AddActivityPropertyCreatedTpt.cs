using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityPropertyCreatedTpt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_property_updated",
                schema: "public");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activities",
                schema: "public",
                table: "activities");

            migrationBuilder.AddColumn<string>(
                name: "activity_type",
                schema: "public",
                table: "activities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                schema: "public",
                table: "activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                schema: "public",
                table: "activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_activities",
                schema: "public",
                table: "activities",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_activities",
                schema: "public",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "activity_type",
                schema: "public",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "new_value",
                schema: "public",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "old_value",
                schema: "public",
                table: "activities");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activities",
                schema: "public",
                table: "activities",
                column: "id");

            migrationBuilder.CreateTable(
                name: "activity_property_updated",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    new_value = table.Column<string>(type: "text", nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: false)
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
        }
    }
}
