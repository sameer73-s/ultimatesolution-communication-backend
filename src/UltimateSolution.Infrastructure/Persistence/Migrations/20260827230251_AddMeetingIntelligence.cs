using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UltimateSolution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranscriptionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaRecordingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalJobReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptionJobs_MeetingRecordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "MeetingRecordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranscriptionJobs_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranscriptionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    DecisionsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    ProposedActionItemsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    ExternalSummaryReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_TranscriptionJobs_TranscriptionJobId",
                        column: x => x.TranscriptionJobId,
                        principalTable: "TranscriptionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranscriptionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SpeakerLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StartOffset = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndOffset = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptionSegments_TranscriptionJobs_TranscriptionJobId",
                        column: x => x.TranscriptionJobId,
                        principalTable: "TranscriptionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AssigneeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionItems_AspNetUsers_AssigneeUserId",
                        column: x => x.AssigneeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionItems_MeetingSummaries_MeetingSummaryId",
                        column: x => x.MeetingSummaryId,
                        principalTable: "MeetingSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssigneeUserId_Status_DueAtUtc",
                table: "ActionItems",
                columns: new[] { "AssigneeUserId", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_MeetingId",
                table: "ActionItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_MeetingSummaryId_CreatedAtUtc",
                table: "ActionItems",
                columns: new[] { "MeetingSummaryId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_ApprovedByUserId",
                table: "MeetingSummaries",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_MeetingId_GeneratedAtUtc",
                table: "MeetingSummaries",
                columns: new[] { "MeetingId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_TranscriptionJobId",
                table: "MeetingSummaries",
                column: "TranscriptionJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionJobs_MeetingId_RequestedAtUtc",
                table: "TranscriptionJobs",
                columns: new[] { "MeetingId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionJobs_RecordingId_Status",
                table: "TranscriptionJobs",
                columns: new[] { "RecordingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionSegments_TranscriptionJobId_SequenceNumber",
                table: "TranscriptionSegments",
                columns: new[] { "TranscriptionJobId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "TranscriptionSegments");

            migrationBuilder.DropTable(
                name: "MeetingSummaries");

            migrationBuilder.DropTable(
                name: "TranscriptionJobs");
        }
    }
}
