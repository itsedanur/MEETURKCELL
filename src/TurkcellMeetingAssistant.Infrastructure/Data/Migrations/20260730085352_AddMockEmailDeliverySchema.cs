using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkcellMeetingAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMockEmailDeliverySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryVersion = table.Column<int>(type: "integer", nullable: false),
                    SummaryManualRevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    EmailType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    SenderEmail = table.Column<string>(type: "text", nullable: true),
                    ToRecipientsJson = table.Column<string>(type: "text", nullable: false),
                    CcRecipientsJson = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    BodyText = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsTestEmail = table.Column<bool>(type: "boolean", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogs_MeetingSummaries_MeetingSummaryId",
                        column: x => x.MeetingSummaryId,
                        principalTable: "MeetingSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailLogs_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailLogs_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_IdempotencyKey",
                table: "EmailLogs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_MeetingId",
                table: "EmailLogs",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_MeetingSummaryId",
                table: "EmailLogs",
                column: "MeetingSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_RequestedAt",
                table: "EmailLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_RequestedByUserId",
                table: "EmailLogs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status",
                table: "EmailLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");
        }
    }
}
