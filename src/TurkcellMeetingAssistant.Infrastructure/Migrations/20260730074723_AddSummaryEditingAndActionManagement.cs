using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkcellMeetingAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryEditingAndActionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "OpenIssues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OpenIssues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MeetingTopics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeetingTopics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalRevokedAt",
                table: "MeetingSummaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalRevokedReason",
                table: "MeetingSummaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedContentHash",
                table: "MeetingSummaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "MeetingSummaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastEditedByUserId",
                table: "MeetingSummaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManualRevisionNumber",
                table: "MeetingSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MeetingDecisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeetingDecisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "MeetingDecisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastEditedByUserId",
                table: "MeetingDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "MeetingDecisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedParticipantId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "ActionItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ActionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByUserId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "ActionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastEditedByUserId",
                table: "ActionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "ActionItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_LastEditedByUserId",
                table: "MeetingSummaries",
                column: "LastEditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecisions_LastEditedByUserId",
                table: "MeetingDecisions",
                column: "LastEditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssignedParticipantId",
                table: "ActionItems",
                column: "AssignedParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_CompletedByUserId",
                table: "ActionItems",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_LastEditedByUserId",
                table: "ActionItems",
                column: "LastEditedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_MeetingParticipants_AssignedParticipantId",
                table: "ActionItems",
                column: "AssignedParticipantId",
                principalTable: "MeetingParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_Users_CompletedByUserId",
                table: "ActionItems",
                column: "CompletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionItems_Users_LastEditedByUserId",
                table: "ActionItems",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingDecisions_Users_LastEditedByUserId",
                table: "MeetingDecisions",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingSummaries_Users_LastEditedByUserId",
                table: "MeetingSummaries",
                column: "LastEditedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_MeetingParticipants_AssignedParticipantId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_Users_CompletedByUserId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionItems_Users_LastEditedByUserId",
                table: "ActionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingDecisions_Users_LastEditedByUserId",
                table: "MeetingDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingSummaries_Users_LastEditedByUserId",
                table: "MeetingSummaries");

            migrationBuilder.DropIndex(
                name: "IX_MeetingSummaries_LastEditedByUserId",
                table: "MeetingSummaries");

            migrationBuilder.DropIndex(
                name: "IX_MeetingDecisions_LastEditedByUserId",
                table: "MeetingDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_AssignedParticipantId",
                table: "ActionItems");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_CompletedByUserId",
                table: "ActionItems");

            migrationBuilder.DropIndex(
                name: "IX_ActionItems_LastEditedByUserId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OpenIssues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OpenIssues");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MeetingTopics");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeetingTopics");

            migrationBuilder.DropColumn(
                name: "ApprovalRevokedAt",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "ApprovalRevokedReason",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "ApprovedContentHash",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "ManualRevisionNumber",
                table: "MeetingSummaries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "MeetingDecisions");

            migrationBuilder.DropColumn(
                name: "AssignedParticipantId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "LastEditedByUserId",
                table: "ActionItems");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ActionItems");
        }
    }
}
