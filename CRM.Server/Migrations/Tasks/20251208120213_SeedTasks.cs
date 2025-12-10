using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.TaskId);
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "TaskId", "CompletedAt", "CreatedAt", "CustomerId", "Description", "DueDate", "IsCompleted", "Title" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6849), 100, "Talk about requirements", new DateTime(2025, 12, 13, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6843), false, "Initial client meeting" },
                    { 2, null, new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6851), 101, "Send cost estimation", new DateTime(2025, 12, 15, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6850), false, "Prepare quotation" },
                    { 3, new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6852), new DateTime(2025, 12, 7, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6856), 102, "Confirm next meeting", new DateTime(2025, 12, 10, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6855), true, "Follow-up call" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
