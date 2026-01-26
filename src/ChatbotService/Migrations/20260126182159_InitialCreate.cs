using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChatbotService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastInteractionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAuthenticated = table.Column<bool>(type: "boolean", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FineTuningJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalEpochs = table.Column<int>(type: "integer", nullable: false),
                    CurrentEpoch = table.Column<int>(type: "integer", nullable: false),
                    CurrentLoss = table.Column<float>(type: "real", nullable: false),
                    ValidationAccuracy = table.Column<float>(type: "real", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TrainingParameters = table.Column<string>(type: "text", nullable: true),
                    ModelPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrainingDataCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
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
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Entities = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(50)", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserMessage = table.Column<string>(type: "text", nullable: false),
                    BotResponse = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Sentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiredHumanEscalation = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TrainingData",
                columns: new[] { "Id", "CreatedAt", "Entities", "ExpectedOutput", "Input", "Intent", "IsActive", "IsValidated", "Source", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Ciao! Sto bene, grazie. Come posso aiutarti oggi?", "Ciao, come stai?", "greeting", true, true, "Seed", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\"order_id\": \"12345\"}", "Cerco le informazioni del tuo ordine #12345. Un momento...", "Qual è lo stato del mio ordine 12345?", "order_status", true, true, "Seed", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Mi dispiace sentire che vuoi cancellare il tuo ordine. Puoi fornirmi il numero dell'ordine?", "Voglio cancellare il mio ordine", "cancel_order", true, true, "Seed", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "{\"product_name\": \"laptop gaming\"}", "Sto cercando laptop gaming disponibili nel nostro catalogo...", "Cerca laptop gaming", "product_search", true, true, "Seed", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Intent",
                table: "ChatMessages",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatedAt",
                table: "ChatSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserEmail",
                table: "ChatSessions",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "FineTuningJobs");

            migrationBuilder.DropTable(
                name: "TrainingData");

            migrationBuilder.DropTable(
                name: "ChatSessions");
        }
    }
}
