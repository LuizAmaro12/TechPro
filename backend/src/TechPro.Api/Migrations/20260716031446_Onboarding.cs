using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class Onboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "exemplo",
                table: "servicos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "exemplo",
                table: "ordens_servico",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "onboarding_concluido_em",
                table: "empresas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "exemplo",
                table: "clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "exemplo",
                table: "servicos");

            migrationBuilder.DropColumn(
                name: "exemplo",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "onboarding_concluido_em",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "exemplo",
                table: "clientes");
        }
    }
}
