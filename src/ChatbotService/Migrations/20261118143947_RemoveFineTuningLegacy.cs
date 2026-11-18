using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChatbotService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFineTuningLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FineTuningJobs");

            migrationBuilder.DropTable(
                name: "TrainingData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FineTuningJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CurrentEpoch = table.Column<int>(type: "integer", nullable: false),
                    CurrentLoss = table.Column<float>(type: "real", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalEpochs = table.Column<int>(type: "integer", nullable: false),
                    TrainingDataCount = table.Column<int>(type: "integer", nullable: false),
                    TrainingParameters = table.Column<string>(type: "text", nullable: true),
                    ValidationAccuracy = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FineTuningJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Entities = table.Column<string>(type: "text", nullable: true),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingData", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TrainingData",
                columns: new[] { "Id", "CreatedAt", "Entities", "ExpectedOutput", "Input", "Intent", "IsActive", "IsValidated", "Source", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Hi! I'm doing well, thanks. How can I help you today?", "Hi, how are you?", "greeting", true, true, "Seed", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\"order_id\": \"12345\"}", "Let me look up the information for your order #12345. One moment...", "What's the status of my order 12345?", "order_status", true, true, "Seed", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "I'm sorry to hear you want to cancel your order. Can you give me the order number?", "I want to cancel my order", "cancel_order", true, true, "Seed", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\"product_name\": \"gaming laptop\"}", "I'm searching for gaming laptops available in our catalog...", "Search for a gaming laptop", "product_search", true, true, "Seed", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FineTuningJobs_CreatedBy",
                table: "FineTuningJobs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FineTuningJobs_StartedAt",
                table: "FineTuningJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FineTuningJobs_Status",
                table: "FineTuningJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingData_CreatedAt",
                table: "TrainingData",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingData_Intent",
                table: "TrainingData",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingData_IsActive",
                table: "TrainingData",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingData_IsValidated",
                table: "TrainingData",
                column: "IsValidated");
        }
    }
}
