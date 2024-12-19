using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoubleYou.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "TEXT", nullable: false),
                    Culture_Code = table.Column<string>(type: "TEXT", nullable: false),
                    Translation_Language = table.Column<string>(type: "TEXT", nullable: false),
                    Favorite_Topic = table.Column<int>(type: "INTEGER", nullable: false),
                    Is_Dialog_Show_Install_Voice = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created_Utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "TEXT", nullable: false),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", nullable: false),
                    Learned_Date = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_ID",
                table: "User",
                column: "ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Words_ID",
                table: "Words",
                column: "ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Words_Learned_Date",
                table: "Words",
                column: "Learned_Date");

            migrationBuilder.CreateIndex(
                name: "IX_Words_Topic",
                table: "Words",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_Words_Word",
                table: "Words",
                column: "Word");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Words");
        }
    }
}
