using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace UiucResearchPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "papers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenAlexId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Doi = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AbstractText = table.Column<string>(type: "text", nullable: true),
                    Topics = table.Column<List<string>>(type: "text[]", nullable: false),
                    AuthorDisplayNames = table.Column<List<string>>(type: "text[]", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_papers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "researchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Position = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Keywords = table.Column<List<string>>(type: "text[]", nullable: false),
                    OpenAlexAuthorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_researchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "paper_authors",
                columns: table => new
                {
                    PaperId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearcherId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_authors", x => new { x.PaperId, x.ResearcherId });
                    table.ForeignKey(
                        name: "FK_paper_authors_papers_PaperId",
                        column: x => x.PaperId,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_authors_researchers_ResearcherId",
                        column: x => x.ResearcherId,
                        principalTable: "researchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paper_authors_ResearcherId",
                table: "paper_authors",
                column: "ResearcherId");

            migrationBuilder.CreateIndex(
                name: "IX_papers_OpenAlexId",
                table: "papers",
                column: "OpenAlexId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_papers_PublicationDate",
                table: "papers",
                column: "PublicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_researchers_Email",
                table: "researchers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paper_authors");

            migrationBuilder.DropTable(
                name: "papers");

            migrationBuilder.DropTable(
                name: "researchers");
        }
    }
}
