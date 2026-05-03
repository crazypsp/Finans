using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Finans.Data.Migrations
{
    /// <inheritdoc />
    public partial class StabilizeBankImportAndTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_ExternalUniqueKey",
                table: "BankTransactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfterTransaction",
                table: "BankTransactions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "BankTransactions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "ExternalTransactionId",
                table: "BankTransactions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinAmount",
                table: "BankTransactionRules",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxAmount",
                table: "BankTransactionRules",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "CompanyUsers",
                columns: new[] { "Id", "CompanyId", "CreatedAtUtc", "CreatedByUserId", "IsActive", "IsCompanyAdmin", "IsDeleted", "PreferredCultureName", "RowVersion", "UpdatedAtUtc", "UpdatedByUserId", "UserId" },
                values: new object[,]
                {
                    { 4, 1, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, false, null, null, null, null, 4 },
                    { 5, 1, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, false, null, null, null, null, 5 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AssignedAtUtc", "AssignedByUserId", "CompanyId", "CreatedAtUtc", "CreatedByUserId", "IsDeleted", "RoleId", "RowVersion", "ScopeCompanyId", "UpdatedAtUtc", "UpdatedByUserId", "UserId" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 4, null, 1, null, null, 4 },
                    { 5, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 6, null, 1, null, null, 5 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CompanyId", "CreatedAtUtc", "CreatedByUserId", "Email", "FirstName", "IsActive", "IsDeleted", "IsEmailVerified", "IsSystemAdmin", "LastLoginAtUtc", "LastName", "PasswordHash", "PasswordSalt", "UpdatedAtUtc", "UpdatedByUserId", "UserName" },
                values: new object[,]
                {
                    { 4, 0, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, "operator@finans.local", "Finans", true, false, true, false, null, "Operator", "5w11B56xzOpVrDfIB5YlMT/ibbmsIAAxKi+GT8/p4ls=", "AQIDBAUGBwgJCgsMDQ4PEA==", null, null, "operator" },
                    { 5, 0, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, "izleyici@finans.local", "Finans", true, false, true, false, null, "Izleyici", "sfecl/+wAtOelrVn2k0al1DzR1GazfL3LBjYERdixbY=", "AQIDBAUGBwgJCgsMDQ4PEA==", null, null, "izleyici" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_CompanyId_BankId_ExternalTransactionId",
                table: "BankTransactions",
                columns: new[] { "CompanyId", "BankId", "ExternalTransactionId" },
                unique: true,
                filter: "[ExternalTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_CompanyId_BankId_ExternalUniqueKey",
                table: "BankTransactions",
                columns: new[] { "CompanyId", "BankId", "ExternalUniqueKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_CompanyId_BankId_ExternalTransactionId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_CompanyId_BankId_ExternalUniqueKey",
                table: "BankTransactions");

            migrationBuilder.DeleteData(
                table: "CompanyUsers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CompanyUsers",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "ExternalTransactionId",
                table: "BankTransactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfterTransaction",
                table: "BankTransactions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "BankTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinAmount",
                table: "BankTransactionRules",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxAmount",
                table: "BankTransactionRules",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_ExternalUniqueKey",
                table: "BankTransactions",
                column: "ExternalUniqueKey",
                unique: true);
        }
    }
}
