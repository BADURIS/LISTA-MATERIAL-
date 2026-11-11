using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMaterial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocalSemPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SemPedido",
                table: "ListasLocais",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SemPedido",
                table: "ListasLocais");
        }
    }
}
