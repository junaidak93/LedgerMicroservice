using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCumulativeAndReversalToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add new columns to existing Transactions table; avoid creating tables that already exist
            migrationBuilder.AddColumn<decimal>(
                name: "CumulativeBalance",
                table: "Transactions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReversal",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalTransactionId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OriginalTransactionId",
                table: "Transactions",
                column: "OriginalTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_OriginalTransactionId",
                table: "Transactions",
                column: "OriginalTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill CumulativeBalance using windowed SUM per user (Postgres syntax)
                        migrationBuilder.Sql(@"
WITH ordered AS (
    SELECT ""Id"", ""UserId"", (CASE WHEN ""Type"" = 0 THEN ""Amount"" - ""Fee"" ELSE - (""Amount"" + ""Fee"") END) AS net_amount, ""CreatedAt"",
                 SUM(CASE WHEN ""Type"" = 0 THEN ""Amount"" - ""Fee"" ELSE - (""Amount"" + ""Fee"") END) OVER (PARTITION BY ""UserId"" ORDER BY ""CreatedAt"", ""Id"" ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS running
    FROM ""Transactions""
)
UPDATE ""Transactions"" t
SET ""CumulativeBalance"" = o.running
FROM ordered o
WHERE t.""Id"" = o.""Id"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            // Drop IdempotencyKeys if it exists
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"IdempotencyKeys\";");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Logins");
        }
    }
}
