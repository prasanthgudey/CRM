using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskItemRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("3b526d93-1db3-48ac-aef8-d1c28436e2a7"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("7d44afb1-7ac7-4e0a-9d5f-3bb31a1bc1a5"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c4"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c5"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c6"));

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "TaskId",
                keyValue: new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c7"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "TaskId", "CompletedAt", "CreatedAt", "CreatedByUserId", "CustomerId", "Description", "DueDate", "IsRecurring", "Priority", "RecurrenceEndDate", "RecurrenceInterval", "RecurrenceType", "State", "Title" },
                values: new object[,]
                {
                    { new Guid("3b526d93-1db3-48ac-aef8-d1c28436e2a7"), null, new DateTime(2025, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "a3083067-9822-4cb2-8c8a-dcb5eb6a6c11", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c36"), "Send pricing and proposal document", new DateTime(2025, 12, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 2, null, null, 0, 2, "Prepare quotation" },
                    { new Guid("7d44afb1-7ac7-4e0a-9d5f-3bb31a1bc1a5"), null, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "a3083067-9822-4cb2-8c8a-dcb5eb6a6c10", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c35"), "Understand client needs and expectations", new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 3, null, null, 0, 1, "Initial discovery call" },
                    { new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c4"), new DateTime(2025, 12, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "a3083067-9822-4cb2-8c8a-dcb5eb6a6c13", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c37"), "Confirm next meeting and timelines", new DateTime(2025, 12, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 1, null, null, 0, 3, "Follow-up call" },
                    { new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c5"), null, new DateTime(2025, 12, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "e64e41f7-b723-4801-a97a-0a2f5f8f8480", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c38"), "Share final agreement via email", new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 3, null, null, 0, 1, "Send agreement" },
                    { new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c6"), null, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "c5f6bcfa-c607-4a55-9d8d-e1f3cba797df", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c39"), "Regular monthly feedback call", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 2, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 3, 1, "Monthly check-in" },
                    { new Guid("f72bf8d4-fe53-4d5e-bc7a-c7a0bdf9c3c7"), null, new DateTime(2025, 12, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "4a19b68c-6767-4bbd-93ee-2ea49ccd65f2", new Guid("d2e2e38b-6a44-4b19-8c4a-4f8fd3869c30"), "Track service progress and issues", new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 4, new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 2, 2, "Weekly support meeting" }
                });
        }
    }
}
