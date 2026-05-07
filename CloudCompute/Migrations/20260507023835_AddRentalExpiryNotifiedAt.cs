using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudCompute.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalExpiryNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryNotifiedAt",
                table: "Rentals",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryNotifiedAt",
                table: "Rentals");
        }
    }
}
