using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMaterial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutenticacaoPerfisNotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000027"));

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Materiais",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AssinaturaDigital",
                table: "ListasLocais",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegistradoPorUsuarioId",
                table: "ListasLocais",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemanaListaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NomeCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Perfil = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RefreshTokenExpiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000017"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000018"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000019"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000021"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000022"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000023"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000024"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000025"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000026"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000028"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000029"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000030"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000031"),
                column: "Ativo",
                value: true);

            migrationBuilder.UpdateData(
                table: "Materiais",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000032"),
                column: "Ativo",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioDestinoId_Lida",
                table: "Notificacoes",
                columns: new[] { "UsuarioDestinoId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Materiais");

            migrationBuilder.DropColumn(
                name: "AssinaturaDigital",
                table: "ListasLocais");

            migrationBuilder.DropColumn(
                name: "RegistradoPorUsuarioId",
                table: "ListasLocais");

            migrationBuilder.InsertData(
                table: "Materiais",
                columns: new[] { "Id", "Nome", "OrdemExibicao", "Unidade" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000027"), "Saco de Lixo Preto 40L", 27, "PCT" });
        }
    }
}
