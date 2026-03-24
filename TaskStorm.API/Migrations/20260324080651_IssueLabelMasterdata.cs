using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class IssueLabelMasterdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "labels",
                schema: "public",
                table: "issues");

            migrationBuilder.CreateTable(
                name: "masterdata_values",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_masterdata_values", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issue_labels",
                schema: "public",
                columns: table => new
                {
                    issue_id = table.Column<int>(type: "integer", nullable: false),
                    masterdata_value_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_labels", x => new { x.issue_id, x.masterdata_value_id });
                    table.ForeignKey(
                        name: "fk_issue_labels_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "public",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_labels_masterdata_values_masterdata_value_id",
                        column: x => x.masterdata_value_id,
                        principalSchema: "public",
                        principalTable: "masterdata_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issue_labels_masterdata_value_id",
                schema: "public",
                table: "issue_labels",
                column: "masterdata_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_masterdata_values_type_code",
                schema: "public",
                table: "masterdata_values",
                columns: new[] { "type", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_labels",
                schema: "public");

            migrationBuilder.DropTable(
                name: "masterdata_values",
                schema: "public");

            migrationBuilder.AddColumn<int[]>(
                name: "labels",
                schema: "public",
                table: "issues",
                type: "integer[]",
                nullable: true);
        }
    }
}
