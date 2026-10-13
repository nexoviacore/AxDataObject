using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARMCommon.Migrations
{
    public partial class APIDef13JAN : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.CreateTable(
                name: "PatientRegistration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    formname = table.Column<string>(type: "text", nullable: false),
                    keyvalue = table.Column<string>(type: "text", nullable: false),
                    paneldata = table.Column<string>(type: "text", nullable: false),
                    createddatetime = table.Column<string>(type: "text", nullable: true),
                    formmodule = table.Column<string>(type: "text", nullable: false),
                    formsubmodule = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientRegistration", x => x.Id);
                });
 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.DropTable(
                name: "PatientRegistration");
        }
    }
}
