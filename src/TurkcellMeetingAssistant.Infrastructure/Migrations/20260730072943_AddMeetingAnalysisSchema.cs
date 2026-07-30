using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkcellMeetingAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingAnalysisSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAnalysisLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputCharacterCount = table.Column<int>(type: "integer", nullable: false),
                    InputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    OutputTokenCount = table.Column<int>(type: "integer", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    RawResponse = table.Column<string>(type: "text", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalysisLogs_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingPurpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AiProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AiModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OwnerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "MeetingDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RelatedTopic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingDecisions_MeetingSummaries_MeetingSummaryId",
                        column: x => x.MeetingSummaryId,
                        principalTable: "MeetingSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingTopics_MeetingSummaries_MeetingSummaryId",
                        column: x => x.MeetingSummaryId,
                        principalTable: "MeetingSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIssues_MeetingSummaries_MeetingSummaryId",
                        column: x => x.MeetingSummaryId,
                        principalTable: "MeetingSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_MeetingId",
                table: "ActionItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_MeetingSummaryId",
                table: "ActionItems",
                column: "MeetingSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisLogs_MeetingId",
                table: "AiAnalysisLogs",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecisions_MeetingSummaryId",
                table: "MeetingDecisions",
                column: "MeetingSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecisions_SortOrder",
                table: "MeetingDecisions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_ApprovedByUserId",
                table: "MeetingSummaries",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_MeetingId",
                table: "MeetingSummaries",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTopics_MeetingSummaryId",
                table: "MeetingTopics",
                column: "MeetingSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTopics_SortOrder",
                table: "MeetingTopics",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIssues_MeetingSummaryId",
                table: "OpenIssues",
                column: "MeetingSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIssues_SortOrder",
                table: "OpenIssues",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "AiAnalysisLogs");

            migrationBuilder.DropTable(
                name: "MeetingDecisions");

            migrationBuilder.DropTable(
                name: "MeetingTopics");

            migrationBuilder.DropTable(
                name: "OpenIssues");

            migrationBuilder.DropTable(
                name: "MeetingSummaries");
        }
    }
}
