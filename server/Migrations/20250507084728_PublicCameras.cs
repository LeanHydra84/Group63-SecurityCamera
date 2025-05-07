using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityCameraServer.Migrations
{
    /// <inheritdoc />
    public partial class PublicCameras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EMail",
                table: "Users",
                newName: "Username");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Cameras",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Cameras");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Users",
                newName: "EMail");
        }
    }
}
