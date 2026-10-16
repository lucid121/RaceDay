using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Roleid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rolename = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Roleid);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Userid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Roleid = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Passwordhash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Datecreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Userid);
                    table.ForeignKey(
                        name: "FK_User_Role_Roleid",
                        column: x => x.Roleid,
                        principalTable: "Role",
                        principalColumn: "Roleid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Eventid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Organiserid = table.Column<int>(type: "int", nullable: false),
                    Eventname = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Eventdate = table.Column<DateTime>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Eventid);
                    table.ForeignKey(
                        name: "FK_Event_User_Organiserid",
                        column: x => x.Organiserid,
                        principalTable: "User",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Categoryid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Eventid = table.Column<int>(type: "int", nullable: false),
                    Categoryname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Distance = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Entryfee = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Categoryid);
                    table.ForeignKey(
                        name: "FK_Category_Event_Eventid",
                        column: x => x.Eventid,
                        principalTable: "Event",
                        principalColumn: "Eventid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolment",
                columns: table => new
                {
                    Enrolmentid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<int>(type: "int", nullable: false),
                    Categoryid = table.Column<int>(type: "int", nullable: false),
                    Enrolmentdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolment", x => x.Enrolmentid);
                    table.ForeignKey(
                        name: "FK_Enrolment_Category_Categoryid",
                        column: x => x.Categoryid,
                        principalTable: "Category",
                        principalColumn: "Categoryid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrolment_User_Userid",
                        column: x => x.Userid,
                        principalTable: "User",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Result",
                columns: table => new
                {
                    Resultid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enrolmentid = table.Column<int>(type: "int", nullable: false),
                    Finishtime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Result", x => x.Resultid);
                    table.ForeignKey(
                        name: "FK_Result_Enrolment_Enrolmentid",
                        column: x => x.Enrolmentid,
                        principalTable: "Enrolment",
                        principalColumn: "Enrolmentid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Category_Eventid",
                table: "Category",
                column: "Eventid");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_Categoryid",
                table: "Enrolment",
                column: "Categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_Userid_Categoryid",
                table: "Enrolment",
                columns: new[] { "Userid", "Categoryid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_Organiserid",
                table: "Event",
                column: "Organiserid");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Enrolmentid",
                table: "Result",
                column: "Enrolmentid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Rolename",
                table: "Role",
                column: "Rolename",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Roleid",
                table: "User",
                column: "Roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Result");

            migrationBuilder.DropTable(
                name: "Enrolment");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
