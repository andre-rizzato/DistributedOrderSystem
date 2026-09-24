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
                    { 1L, "Hello {customerName},\\n\\nThank you for your order #{orderId}!\\n\\nOrder details:\\n{orderDetails}\\n\\nTotal: {total}\\n\\nThank you for choosing us!\\n\\n{companyName}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Order confirmation", "<h2>Order confirmation #{orderId}</h2><p>Hello <strong>{customerName}</strong>,</p><p>Thank you for your order!</p><div>{orderDetails}</div><p><strong>Total: {total}</strong></p>", true, "order_confirmation", "Order confirmation #{orderId} - {companyName}", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"Order ID\", \"customerName\": \"Customer name\", \"orderDetails\": \"Order details\", \"total\": \"Order total\", \"companyName\": \"Company name\"}" },
                    { 2L, "Hello {customerName}! Your order #{orderId} has been shipped. Tracking: {trackingNumber}. Estimated delivery: {deliveryDate}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Order shipped notification", null, true, "order_shipped", "Order #{orderId} shipped", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"Order ID\", \"customerName\": \"Customer name\", \"trackingNumber\": \"Tracking number\", \"deliveryDate\": \"Delivery date\"}" },
                    { 3L, "Hello {customerName},\\n\\nThe payment for order #{orderId} is due on {dueDate}.\\n\\nAmount: {amount}\\n\\nPlease make the payment before the due date to avoid service interruptions.\\n\\nThank you!", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Payment due reminder", "<h3>Payment Reminder</h3><p>Hello {customerName},</p><p>The payment for order <strong>#{orderId}</strong> is due on <strong>{dueDate}</strong>.</p><p>Amount: <strong>{amount}</strong></p><p>Please make the payment before the due date.</p>", true, "payment_reminder", "Payment reminder - Order #{orderId}", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"orderId\": \"Order ID\", \"customerName\": \"Customer name\", \"dueDate\": \"Due date\", \"amount\": \"Amount due\"}" },
                    { 4L, "Hello {userName}! Welcome to {appName}. Discover all our app's features and start shopping right away!", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New user welcome message", null, true, "welcome_user", "Welcome to {appName}!", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"userName\": \"User name\", \"appName\": \"Application name\"}" },
                    { 5L, "Attention: the system will be under maintenance on {maintenanceDate} from {startTime} to {endTime}. Some features may be unavailable.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System maintenance notification", null, true, "system_maintenance", "Scheduled system maintenance", 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"maintenanceDate\": \"Maintenance date\", \"startTime\": \"Start time\", \"endTime\": \"End time\"}" }
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
