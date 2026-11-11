using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LMaterial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materiais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemanasListas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CriadoPorSupervisorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanasListas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListasLocais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SemanaListaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SeparadoPorAlmoxarifeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataSeparacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NomeRetiradoPor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DataRetirada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasLocais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasLocais_Locais_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListasLocais_SemanasListas_SemanaListaId",
                        column: x => x.SemanaListaId,
                        principalTable: "SemanasListas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListaLocalItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListaLocalId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantidadeSolicitada = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeSeparada = table.Column<int>(type: "integer", nullable: true),
                    StatusItem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaLocalItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaLocalItens_ListasLocais_ListaLocalId",
                        column: x => x.ListaLocalId,
                        principalTable: "ListasLocais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListaLocalItens_Materiais_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Locais",
                columns: new[] { "Id", "Ativo", "Nome", "OrdemExibicao", "Tipo" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), true, "Andar 1", 1, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), true, "Andar 2", 2, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), true, "Andar 3", 3, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), true, "Andar 4", 4, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), true, "Andar 5", 5, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), true, "Andar 6", 6, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000007"), true, "Andar 7", 7, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000008"), true, "Andar 8", 8, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000009"), true, "Andar 9", 9, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000010"), true, "Andar 10", 10, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000011"), true, "Andar 11", 11, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000012"), true, "Andar 12", 12, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000013"), true, "Andar 13", 13, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000014"), true, "Andar 14", 14, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000015"), true, "Andar 15", 15, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000016"), true, "Andar 16", 16, "Andar" },
                    { new Guid("20000000-0000-0000-0000-000000000017"), true, "Recepção", 17, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000018"), true, "COI1", 18, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000019"), true, "COI2", 19, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000020"), true, "Docas", 20, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000021"), true, "Refeitório", 21, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000022"), true, "Estoque", 22, "AreaComum" },
                    { new Guid("20000000-0000-0000-0000-000000000023"), true, "Ambulatório", 23, "AreaComum" }
                });

            migrationBuilder.InsertData(
                table: "Materiais",
                columns: new[] { "Id", "Nome", "OrdemExibicao", "Unidade" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Copo Plástico", 1, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Papel Toalha", 2, "CX" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Papel Higiênico", 3, "CX" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Sabonete", 4, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Tela Mictório", 5, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Protetor Assento", 6, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Detergente", 7, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Esponja de Pia", 8, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Fibra Branca", 9, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "Fibra Verde", 10, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Fio Dental", 11, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "Álcool em Gel", 12, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Saco de Marmita", 13, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "Inseticida", 14, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "Sabão Barra", 15, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "Cotonete", 16, "CX" },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "Descarte Absorvente", 17, "UN" },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "Saco de Lixo Azul 20L", 18, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "Saco de Lixo Azul 40L", 19, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000020"), "Saco de Lixo Azul 60L", 20, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000021"), "Saco de Lixo Azul 100L", 21, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000022"), "Saco de Lixo Vermelho 20L", 22, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000023"), "Saco de Lixo Vermelho 40L", 23, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000024"), "Saco de Lixo Vermelho 60L", 24, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000025"), "Saco de Lixo Vermelho 100L", 25, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000026"), "Saco de Lixo Preto 20L", 26, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000027"), "Saco de Lixo Preto 40L", 27, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000028"), "Saco de Lixo Preto 60L", 28, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000029"), "Saco de Lixo Preto 100L", 29, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000030"), "Saco de Lixo Preto 200L", 30, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000031"), "Saco de Lixo Amarelo 100L", 31, "PCT" },
                    { new Guid("10000000-0000-0000-0000-000000000032"), "Saco de Lixo Amarelo 60L", 32, "PCT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListaLocalItens_ListaLocalId_MaterialId",
                table: "ListaLocalItens",
                columns: new[] { "ListaLocalId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListaLocalItens_MaterialId",
                table: "ListaLocalItens",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasLocais_LocalId",
                table: "ListasLocais",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasLocais_SemanaListaId_LocalId",
                table: "ListasLocais",
                columns: new[] { "SemanaListaId", "LocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locais_Nome",
                table: "Locais",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materiais_Nome",
                table: "Materiais",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListaLocalItens");

            migrationBuilder.DropTable(
                name: "ListasLocais");

            migrationBuilder.DropTable(
                name: "Materiais");

            migrationBuilder.DropTable(
                name: "Locais");

            migrationBuilder.DropTable(
                name: "SemanasListas");
        }
    }
}
