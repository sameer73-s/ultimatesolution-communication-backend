using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase383ActionItemUniqueSourceMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatChannels_Projects_ProjectId",
                table: "ChatChannels");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatChannels_Projects_ProjectId",
                table: "ChatChannels",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatChannels_Projects_ProjectId",
                table: "ChatChannels");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatChannels_Projects_ProjectId",
                table: "ChatChannels",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
