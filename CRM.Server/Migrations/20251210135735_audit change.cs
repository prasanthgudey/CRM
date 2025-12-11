using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Server.Migrations
{
    public partial class CreateAuditLogsFresh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    TargetUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),

                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),

                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    IsSuccess = table.Column<bool>(nullable: false, defaultValue: false),

                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
