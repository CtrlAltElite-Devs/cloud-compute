using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudCompute.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationIdImageAndGpuRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityImagePath",
                table: "OwnerVerificationRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Gpus",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityImagePath",
                table: "OwnerVerificationRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Gpus");
        }
    }
}
