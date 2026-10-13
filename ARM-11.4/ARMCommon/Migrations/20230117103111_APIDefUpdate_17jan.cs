using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARMCommon.Migrations
{
    public partial class APIDefUpdate_17jan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "KeyField",
                table: "AxModulePages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "FormUpdatedBy",
                table: "AxInLineForm",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormUpdatedOn",
                table: "AxInLineForm",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormUpdatedBy",
                table: "AxInLineForm");

            migrationBuilder.DropColumn(
                name: "FormUpdatedOn",
                table: "AxInLineForm");

            migrationBuilder.AlterColumn<string>(
                name: "KeyField",
                table: "AxModulePages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
