using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webapp_f26_exam.Migrations
{
    /// <inheritdoc />
    public partial class AddSentAtToBookingChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "BookingChats",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "BookingChats");
        }
    }
}
