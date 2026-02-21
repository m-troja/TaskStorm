using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskStorm.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_comment_attachment_comments_comment_id",
                schema: "public",
                table: "comment_attachment");

            migrationBuilder.DropPrimaryKey(
                name: "pk_comment_attachment",
                schema: "public",
                table: "comment_attachment");

            migrationBuilder.RenameTable(
                name: "comment_attachment",
                schema: "public",
                newName: "attachments",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_comment_attachment_comment_id",
                schema: "public",
                table: "attachments",
                newName: "ix_attachments_comment_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_attachments",
                schema: "public",
                table: "attachments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_comments_comment_id",
                schema: "public",
                table: "attachments",
                column: "comment_id",
                principalSchema: "public",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_attachments_comments_comment_id",
                schema: "public",
                table: "attachments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_attachments",
                schema: "public",
                table: "attachments");

            migrationBuilder.RenameTable(
                name: "attachments",
                schema: "public",
                newName: "comment_attachment",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_attachments_comment_id",
                schema: "public",
                table: "comment_attachment",
                newName: "ix_comment_attachment_comment_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_comment_attachment",
                schema: "public",
                table: "comment_attachment",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_comment_attachment_comments_comment_id",
                schema: "public",
                table: "comment_attachment",
                column: "comment_id",
                principalSchema: "public",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
