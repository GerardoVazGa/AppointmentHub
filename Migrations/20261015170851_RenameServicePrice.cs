using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameServicePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "price",
                table: "Services",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Services",
                newName: "price");
        }
    }
}
