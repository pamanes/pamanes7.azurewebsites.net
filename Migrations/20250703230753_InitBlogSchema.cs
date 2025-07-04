using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Migrations
{
    /// <inheritdoc />
    public partial class InitBlogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "NOCASE"),
                    Slug = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE"),
                    Path = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE"),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true, collation: "NOCASE"),
                    Tags = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE"),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Markdown = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Date",
                table: "Posts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Tags",
                table: "Posts",
                column: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Title",
                table: "Posts",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts");
        }
    }
}
