using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.IdentityService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdentityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalIdentityProvider",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalObjectId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cac43a6e-f7bb-4448-baaf-1add431ccbbf",
                column: "ConcurrencyStamp",
                value: "2e4114ad-e203-4d96-971e-d8756925b186");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cbc43a8e-f7bb-4445-baaf-1add431ffbbf",
                column: "ConcurrencyStamp",
                value: "3cd628d7-ec17-4342-8235-72438d4ac19d");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e445865-a24d-4543-a6c6-9443d048cdb9",
                columns: new[] { "ExternalIdentityProvider", "ExternalObjectId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ExternalIdentityProvider", "ExternalObjectId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ExternalObjectId",
                table: "AspNetUsers",
                column: "ExternalObjectId",
                unique: true,
                filter: "[ExternalObjectId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ExternalObjectId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalIdentityProvider",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExternalObjectId",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cac43a6e-f7bb-4448-baaf-1add431ccbbf",
                column: "ConcurrencyStamp",
                value: "af940ba5-91f3-4425-8b07-635f711dfabc");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cbc43a8e-f7bb-4445-baaf-1add431ffbbf",
                column: "ConcurrencyStamp",
                value: "7367eba8-aa42-4b7b-80cc-7c484fedad5e");
        }
    }
}
