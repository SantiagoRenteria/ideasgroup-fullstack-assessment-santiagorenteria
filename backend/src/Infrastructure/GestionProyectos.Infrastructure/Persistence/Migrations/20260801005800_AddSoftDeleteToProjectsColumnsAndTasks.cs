using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToProjectsColumnsAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_columns_projects_project_id",
                table: "columns");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_columns_column_id",
                table: "tasks");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "columns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "columns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_columns_projects_project_id",
                table: "columns",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_columns_column_id",
                table: "tasks",
                column: "column_id",
                principalTable: "columns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_columns_projects_project_id",
                table: "columns");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_columns_column_id",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "columns");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "columns");

            migrationBuilder.AddForeignKey(
                name: "FK_columns_projects_project_id",
                table: "columns",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_columns_column_id",
                table: "tasks",
                column: "column_id",
                principalTable: "columns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
