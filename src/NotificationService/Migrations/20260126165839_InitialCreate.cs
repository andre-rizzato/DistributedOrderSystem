using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PreferredChannel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentTemplate = table.Column<string>(type: "text", nullable: false),
                    HtmlTemplate = table.Column<string>(type: "text", nullable: true),
                    Variables = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "ContentTemplate", "CreatedAt", "Description", "HtmlTemplate", "IsActive", "Name", "SubjectTemplate", "Type", "UpdatedAt", "Variables" },
                values: new object[,]
                {
                    { 1L, "Ciao {customerName},\\n\\nGrazie per il tuo ordine #{orderId}!\\n\\nDettagli ordine:\\n{orderDetails}\\n\\nTotale: {total}\\n\\nGrazie per averci scelto!\\n\\n{companyName}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Conferma ordine effettuato", "<h2>Conferma ordine #{orderId}</h2><p>Ciao <strong>{customerName}</strong>,</p><p>Grazie per il tuo ordine!</p><div>{orderDetails}</div><p><strong>Totale: {total}</strong></p>", true, "order_confirmation", "Conferma ordine #{orderId} - {companyName}", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"ID ordine\", \"customerName\": \"Nome cliente\", \"orderDetails\": \"Dettagli ordine\", \"total\": \"Totale ordine\", \"companyName\": \"Nome azienda\"}" },
                    { 2L, "Ciao {customerName}! Il tuo ordine #{orderId} è stato spedito. Tracking: {trackingNumber}. Consegna prevista: {deliveryDate}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Notifica spedizione ordine", null, true, "order_shipped", "Ordine #{orderId} spedito", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"ID ordine\", \"customerName\": \"Nome cliente\", \"trackingNumber\": \"Codice tracking\", \"deliveryDate\": \"Data consegna\"}" },
                    { 3L, "Ciao {customerName},\\n\\nIl pagamento per l'ordine #{orderId} scadrà il {dueDate}.\\n\\nImporto: {amount}\\n\\nEffettua il pagamento entro la scadenza per evitare interruzioni del servizio.\\n\\nGrazie!", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Promemoria pagamento in scadenza", "<h3>Promemoria Pagamento</h3><p>Ciao {customerName},</p><p>Il pagamento per l'ordine <strong>#{orderId}</strong> scadrà il <strong>{dueDate}</strong>.</p><p>Importo: <strong>{amount}</strong></p><p>Ti preghiamo di effettuare il pagamento entro la scadenza.</p>", true, "payment_reminder", "Promemoria pagamento - Ordine #{orderId}", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"ID ordine\", \"customerName\": \"Nome cliente\", \"dueDate\": \"Data scadenza\", \"amount\": \"Importo da pagare\"}" },
                    { 4L, "Ciao {userName}! Benvenuto in {appName}. Scopri tutte le funzionalità della nostra app e inizia subito a fare shopping!", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Messaggio di benvenuto nuovo utente", null, true, "welcome_user", "Benvenuto in {appName}!", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"userName\": \"Nome utente\", \"appName\": \"Nome applicazione\"}" },
                    { 5L, "Attenzione: il sistema sarà in manutenzione il {maintenanceDate} dalle {startTime} alle {endTime}. Alcune funzionalità potrebbero non essere disponibili.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Notifica manutenzione sistema", null, true, "system_maintenance", "Manutenzione programmata sistema", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"maintenanceDate\": \"Data manutenzione\", \"startTime\": \"Ora inizio\", \"endTime\": \"Ora fine\"}" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Action",
                table: "NotificationLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_NotificationId",
                table: "NotificationLogs",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Provider",
                table: "NotificationLogs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Timestamp",
                table: "NotificationLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Type_Category",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Type", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReferenceId_ReferenceType",
                table: "Notifications",
                columns: new[] { "ReferenceId", "ReferenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ScheduledAt",
                table: "Notifications",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_IsActive",
                table: "NotificationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Name",
                table: "NotificationTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Type",
                table: "NotificationTemplates",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
