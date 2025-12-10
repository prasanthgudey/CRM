using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTasksModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                table: "Tasks",
                newName: "IsRecurring");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceType",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "DueDate", "Priority", "RecurrenceEndDate", "RecurrenceInterval", "RecurrenceType", "State", "Title" },
                values: new object[] { new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Understand client needs and expectations", new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, null, null, 0, 1, "Initial discovery call" });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "DueDate", "Priority", "RecurrenceEndDate", "RecurrenceInterval", "RecurrenceType", "State" },
                values: new object[] { new DateTime(2025, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Send pricing and proposal document", new DateTime(2025, 12, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, null, null, 0, 2 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 3,
                columns: new[] { "CompletedAt", "CreatedAt", "Description", "DueDate", "IsRecurring", "Priority", "RecurrenceEndDate", "RecurrenceInterval", "RecurrenceType", "State" },
                values: new object[] { new DateTime(2025, 12, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Confirm next meeting and timelines", new DateTime(2025, 12, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 1, null, null, 0, 3 });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "TaskId", "CompletedAt", "CreatedAt", "CustomerId", "Description", "DueDate", "IsRecurring", "Priority", "RecurrenceEndDate", "RecurrenceInterval", "RecurrenceType", "State", "Title" },
                values: new object[,]
                {
                    { 4, null, new DateTime(2025, 12, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 103, "Share final agreement via email", new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 3, null, null, 0, 1, "Send agreement" },
                    { 5, null, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 104, "Regular monthly feedback call", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 2, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 3, 1, "Monthly check-in" },
                    { 6, null, new DateTime(2025, 12, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 105, "Track service progress and issues", new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 4, new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 2, 2, "Weekly support meeting" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceType",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "IsRecurring",
                table: "Tasks",
                newName: "IsCompleted");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "DueDate", "Title" },
                values: new object[] { new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6849), "Talk about requirements", new DateTime(2025, 12, 13, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6843), "Initial client meeting" });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "DueDate" },
                values: new object[] { new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6851), "Send cost estimation", new DateTime(2025, 12, 15, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6850) });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: 3,
                columns: new[] { "CompletedAt", "CreatedAt", "Description", "DueDate", "IsCompleted" },
                values: new object[] { new DateTime(2025, 12, 8, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6852), new DateTime(2025, 12, 7, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6856), "Confirm next meeting", new DateTime(2025, 12, 10, 12, 2, 13, 111, DateTimeKind.Utc).AddTicks(6855), true });
        }
    }
}
