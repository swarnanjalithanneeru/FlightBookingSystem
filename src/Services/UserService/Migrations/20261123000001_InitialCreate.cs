using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UserService.Data;

#nullable disable

namespace UserService.Migrations
{
    [DbContext(typeof(UserDbContext))]
    [Migration("20261123000001_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [Role] WHERE RoleId = 1) BEGIN SET IDENTITY_INSERT [Role] ON; INSERT INTO [Role] (RoleId, RoleName) VALUES (1, 'Admin'), (2, 'Customer'); SET IDENTITY_INSERT [Role] OFF; END");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [User] WHERE UserId = 1) BEGIN SET IDENTITY_INSERT [User] ON; INSERT INTO [User] (UserId, RoleId, FullName, Email, PasswordHash, PhoneNumber, CreatedAt) VALUES (1, 1, 'System Administrator', 'admin@flightbooking.com', '$2a$11$91/GfXbF15aK61z/i3M/z.M0c0Kx7b.R18e1EaJ5eFvj1mZ20wLfa', '+1234567890', GETUTCDATE()); SET IDENTITY_INSERT [User] OFF; END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
