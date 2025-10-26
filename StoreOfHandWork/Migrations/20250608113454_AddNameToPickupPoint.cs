using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreOfHandWork.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToPickupPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PickupPoints",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "PickupPoints");
        }
    }
}
