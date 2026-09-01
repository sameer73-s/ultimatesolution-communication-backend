using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase38RolesAndEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_Meetings_MeetingId",
                table: "ActionItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "ChatChannels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ActionItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingSummaryId",
                table: "ActionItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingId",
                table: "ActionItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ActionItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerUserId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMessageId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "ActionItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ActionItemHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItemHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItemHistories_ActionItems_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionItemHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EssAccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HrAssigneeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedServiceType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EssServiceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EssAccessRequests_AspNetUsers_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EssAccessRequests_AspNetUsers_HrAssigneeUserId",
                        column: x => x.HrAssigneeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EssAccessRequests_AspNetUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondPartyUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RestoreReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageDeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageDeletionRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageDeletionRequests_AspNetUsers_SecondPartyUserId",
                        column: x => x.SecondPartyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageDeletionRequests_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatChannels_ProjectId",
                table: "ChatChannels",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_ProjectId",
                table: "ActionItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_ReviewerUserId",
                table: "ActionItems",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItemHistories_ActionItemId",
                table: "ActionItemHistories",
                column: "ActionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItemHistories_ChangedByUserId",
                table: "ActionItemHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EssAccessRequests_EmployeeUserId",
                table: "EssAccessRequests",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EssAccessRequests_HrAssigneeUserId",
                table: "EssAccessRequests",
                column: "HrAssigneeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EssAccessRequests_ManagerUserId",
                table: "EssAccessRequests",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeletionRequests_MessageId",
                table: "MessageDeletionRequests",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeletionRequests_RequestedByUserId",
                table: "MessageDeletionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeletionRequests_SecondPartyUserId",
                table: "MessageDeletionRequests",
                column: "SecondPartyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_AspNetUsers_ReviewerUserId",
                table: "ActionItems",
                column: "ReviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems",
                column: "SourceMessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_Meetings_MeetingId",
                table: "ActionItems",
                column: "MeetingId",
                principalTable: "Meetings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_Projects_ProjectId",
                table: "ActionItems",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ActionItems_AspNetUsers_ReviewerUserId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_ChatMessages_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_Meetings_MeetingId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_Projects_ProjectId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatChannels_Projects_ProjectId",
                table: "ChatChannels");

            migrationBuilder.DropTable(
                name: "ActionItemHistories");

            migrationBuilder.DropTable(
                name: "EssAccessRequests");

            migrationBuilder.DropTable(
                name: "MessageDeletionRequests");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ChatChannels_ProjectId",
                table: "ChatChannels");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_ProjectId",
                table: "ActionItems");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_ReviewerUserId",
                table: "ActionItems");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_SourceMessageId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ChatChannels");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ActionItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ActionItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingSummaryId",
                table: "ActionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingId",
                table: "ActionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_Meetings_MeetingId",
                table: "ActionItems",
                column: "MeetingId",
                principalTable: "Meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
