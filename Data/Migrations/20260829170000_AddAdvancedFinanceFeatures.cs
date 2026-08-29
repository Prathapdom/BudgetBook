using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBook.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829170000_AddAdvancedFinanceFeatures")]
public partial class AddAdvancedFinanceFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Tags", table: "Transactions", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "TransferGroupId", table: "Transactions", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<int>(name: "TransferDirection", table: "Transactions", type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.CreateIndex(name: "IX_Transactions_TransferGroupId", table: "Transactions", column: "TransferGroupId");

        migrationBuilder.CreateTable(
            name: "ExchangeRates",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                BaseCurrency = table.Column<string>(type: "TEXT", nullable: false),
                QuoteCurrency = table.Column<string>(type: "TEXT", nullable: false),
                Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                CapturedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ExchangeRates", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_ExchangeRates_BaseCurrency_QuoteCurrency_CapturedAt", table: "ExchangeRates", columns: new[] { "BaseCurrency", "QuoteCurrency", "CapturedAt" }, unique: true);

        migrationBuilder.CreateTable(
            name: "RecurringTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: false),
                Tags = table.Column<string>(type: "TEXT", nullable: false),
                AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                Interval = table.Column<int>(type: "INTEGER", nullable: false),
                NextOccurrence = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecurringTransactions", x => x.Id);
                table.ForeignKey("FK_RecurringTransactions_Accounts_AccountId", x => x.AccountId, "Accounts", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_RecurringTransactions_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.SetNull);
            });
        migrationBuilder.CreateIndex(name: "IX_RecurringTransactions_AccountId", table: "RecurringTransactions", column: "AccountId");
        migrationBuilder.CreateIndex(name: "IX_RecurringTransactions_CategoryId", table: "RecurringTransactions", column: "CategoryId");

        migrationBuilder.CreateTable(
            name: "TransactionAttachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                TransactionRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                FileName = table.Column<string>(type: "TEXT", nullable: false),
                StoredPath = table.Column<string>(type: "TEXT", nullable: false),
                AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionAttachments", x => x.Id);
                table.ForeignKey("FK_TransactionAttachments_Transactions_TransactionRecordId", x => x.TransactionRecordId, "Transactions", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_TransactionAttachments_TransactionRecordId", table: "TransactionAttachments", column: "TransactionRecordId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExchangeRates");
        migrationBuilder.DropTable(name: "RecurringTransactions");
        migrationBuilder.DropTable(name: "TransactionAttachments");
        migrationBuilder.DropIndex(name: "IX_Transactions_TransferGroupId", table: "Transactions");
        migrationBuilder.DropColumn(name: "Tags", table: "Transactions");
        migrationBuilder.DropColumn(name: "TransferGroupId", table: "Transactions");
        migrationBuilder.DropColumn(name: "TransferDirection", table: "Transactions");
    }
}
