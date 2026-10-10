using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LevelUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RetiredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.ActivityId);
                    table.CheckConstraint("CK_Activity_RetiredDate", "[RetiredDate] IS NULL OR [RetiredDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "Goal",
                columns: table => new
                {
                    GoalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goal", x => x.GoalId);
                    table.CheckConstraint("CK_Goal_ProgressPercentage", "[ProgressPercentage] BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    RecommendationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.RecommendationId);
                });

            migrationBuilder.CreateTable(
                name: "ActivityScores",
                columns: table => new
                {
                    ActivityScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityScores", x => x.ActivityScoreId);
                    table.CheckConstraint("CK_ActivityScores_Score", "[Score] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_ActivityScores_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalCheckpoints",
                columns: table => new
                {
                    GoalCheckpointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<int>(type: "int", nullable: false),
                    CheckpointDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PercentComplete = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalCheckpoints", x => x.GoalCheckpointId);
                    table.CheckConstraint("CK_GoalCheckpoints_PercentComplete", "[PercentComplete] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_GoalCheckpoints_Goal_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goal",
                        principalColumn: "GoalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationHistory",
                columns: table => new
                {
                    RecommendationHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: true),
                    RecommendationText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    GoalId = table.Column<int>(type: "int", nullable: true),
                    Trigger = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShownDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationHistory", x => x.RecommendationHistoryId);
                    table.ForeignKey(
                        name: "FK_RecommendationHistory_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecommendationHistory_Goal_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goal",
                        principalColumn: "GoalId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecommendationHistory_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "RecommendationId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Recommendations",
                columns: new[] { "RecommendationId", "Category", "CreatedBy", "CreatedDate", "IsActive", "ModifiedBy", "ModifiedDate", "RecommendationText" },
                values: new object[,]
                {
                    { 1, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Reschedule the activity to a better time of day when you have more energy." },
                    { 2, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Break the activity into smaller tasks you can finish in under 10 minutes." },
                    { 3, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Set a reminder on your phone or calendar for tomorrow." },
                    { 4, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Reduce the task complexity - do a minimal version on busy days." },
                    { 5, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Improve consistency by doing it at the same time and place every day." },
                    { 6, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Stack it onto an existing habit (e.g. right after your morning coffee)." },
                    { 7, "Activity", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Prepare everything you need the night before to remove friction." },
                    { 8, "Goal", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Break the goal into monthly milestones and focus only on the next one." },
                    { 9, "Goal", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Schedule a weekly 15-minute review of this goal in your calendar." },
                    { 10, "Goal", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Share the goal with a friend or mentor for accountability." },
                    { 11, "Goal", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Identify the single biggest obstacle and plan one action to remove it." },
                    { 12, "Goal", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Revisit the target date - an achievable deadline beats an abandoned one." },
                    { 13, "General", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Celebrate small wins - progress is progress." },
                    { 14, "General", "seed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "Track it visibly: a streak you can see is a streak you will protect." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_Status",
                table: "Activity",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_Title",
                table: "Activity",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityScores_ActivityId_EvaluationDate",
                table: "ActivityScores",
                columns: new[] { "ActivityId", "EvaluationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityScores_EvaluationDate",
                table: "ActivityScores",
                column: "EvaluationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Goal_ProgressPercentage",
                table: "Goal",
                column: "ProgressPercentage");

            migrationBuilder.CreateIndex(
                name: "IX_GoalCheckpoints_GoalId_CheckpointDate",
                table: "GoalCheckpoints",
                columns: new[] { "GoalId", "CheckpointDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistory_ActivityId",
                table: "RecommendationHistory",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistory_GoalId",
                table: "RecommendationHistory",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistory_RecommendationId",
                table: "RecommendationHistory",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationHistory_ShownDate",
                table: "RecommendationHistory",
                column: "ShownDate");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_Category_IsActive",
                table: "Recommendations",
                columns: new[] { "Category", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityScores");

            migrationBuilder.DropTable(
                name: "GoalCheckpoints");

            migrationBuilder.DropTable(
                name: "RecommendationHistory");

            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropTable(
                name: "Goal");

            migrationBuilder.DropTable(
                name: "Recommendations");
        }
    }
}
