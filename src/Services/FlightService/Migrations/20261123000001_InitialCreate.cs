using System;
using FlightService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightService.Migrations
{
    [DbContext(typeof(FlightDbContext))]
    [Migration("20261123000001_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flight",
                columns: table => new
                {
                    FlightId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flight", x => x.FlightId);
                });

            migrationBuilder.CreateTable(
                name: "FlightSeat",
                columns: table => new
                {
                    SeatId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightId = table.Column<int>(type: "int", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SeatClass = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SeatStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Available")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSeat", x => x.SeatId);
                    table.ForeignKey(
                        name: "FK_FlightSeat_Flight_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flight",
                        principalColumn: "FlightId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flight_FlightNumber_DepartureTime",
                table: "Flight",
                columns: new[] { "FlightNumber", "DepartureTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flight_Source_Destination_DepartureTime",
                table: "Flight",
                columns: new[] { "Source", "Destination", "DepartureTime" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightSeat_FlightId_SeatNumber",
                table: "FlightSeat",
                columns: new[] { "FlightId", "SeatNumber" },
                unique: true);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [Flight] WHERE FlightId = 1) BEGIN SET IDENTITY_INSERT [Flight] ON; INSERT INTO [Flight] (FlightId, FlightNumber, Airline, Source, Destination, DepartureTime, ArrivalTime, TotalSeats, BaseFare, IsActive) VALUES (1, 'AI-101', 'Air India', 'DEL', 'BOM', DATEADD(day, 1, GETUTCDATE()), DATEADD(day, 1, DATEADD(hour, 2, GETUTCDATE())), 6, 4500.00, 1), (2, '6E-202', 'IndiGo', 'BOM', 'BLR', DATEADD(day, 1, GETUTCDATE()), DATEADD(day, 1, DATEADD(hour, 2, GETUTCDATE())), 6, 3200.00, 1), (3, 'UK-303', 'Vistara', 'DEL', 'DXB', DATEADD(day, 1, GETUTCDATE()), DATEADD(day, 1, DATEADD(hour, 3, GETUTCDATE())), 6, 12000.00, 1); SET IDENTITY_INSERT [Flight] OFF; END");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [FlightSeat] WHERE SeatId = 1) BEGIN SET IDENTITY_INSERT [FlightSeat] ON; INSERT INTO [FlightSeat] (SeatId, FlightId, SeatNumber, SeatClass, IsAvailable, SeatStatus) VALUES (1, 1, '1A', 'Business', 1, 'Available'), (2, 1, '1B', 'Business', 1, 'Available'), (3, 1, '2A', 'Economy', 1, 'Available'), (4, 1, '2B', 'Economy', 1, 'Available'), (5, 1, '3A', 'Economy', 1, 'Available'), (6, 1, '3B', 'Economy', 1, 'Available'), (7, 2, '1A', 'Business', 1, 'Available'), (8, 2, '1B', 'Business', 1, 'Available'), (9, 2, '2A', 'Economy', 1, 'Available'), (10, 2, '2B', 'Economy', 1, 'Available'), (11, 2, '3A', 'Economy', 1, 'Available'), (12, 2, '3B', 'Economy', 1, 'Available'), (13, 3, '1A', 'Business', 1, 'Available'), (14, 3, '1B', 'Business', 1, 'Available'), (15, 3, '2A', 'Economy', 1, 'Available'), (16, 3, '2B', 'Economy', 1, 'Available'), (17, 3, '3A', 'Economy', 1, 'Available'), (18, 3, '3B', 'Economy', 1, 'Available'); SET IDENTITY_INSERT [FlightSeat] OFF; END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightSeat");

            migrationBuilder.DropTable(
                name: "Flight");
        }
    }
}
