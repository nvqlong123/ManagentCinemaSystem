using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagentCinemaSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCodeAndStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingCode",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffIdConfirmed",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingCode",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffIdConfirmed",
                table: "Bookings",
                column: "StaffIdConfirmed");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_StaffIdConfirmed",
                table: "Bookings",
                column: "StaffIdConfirmed",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_StaffIdConfirmed",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingCode",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffIdConfirmed",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StaffIdConfirmed",
                table: "Bookings");
        }
    }
}
