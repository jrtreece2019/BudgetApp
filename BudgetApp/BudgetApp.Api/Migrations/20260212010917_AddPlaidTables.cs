using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaidItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlaidItemId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    InstitutionName = table.Column<string>(type: "text", nullable: false),
                    InstitutionId = table.Column<string>(type: "text", nullable: false),
                    TransactionsCursor = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaidItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaidItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaidAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaidItemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlaidAccountId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OfficialName = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    SubType = table.Column<string>(type: "text", nullable: true),
                    Mask = table.Column<string>(type: "text", nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: true),
                    AvailableBalance = table.Column<decimal>(type: "numeric", nullable: true),
                    DefaultCategoryId = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaidAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaidAccounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaidAccounts_Categories_DefaultCategoryId",
                        column: x => x.DefaultCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlaidAccounts_PlaidItems_PlaidItemId",
                        column: x => x.PlaidItemId,
                        principalTable: "PlaidItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlaidAccountId = table.Column<int>(type: "integer", nullable: false),
                    PlaidTransactionId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPending = table.Column<bool>(type: "boolean", nullable: false),
                    PlaidCategory = table.Column<string>(type: "text", nullable: true),
                    PlaidDetailedCategory = table.Column<string>(type: "text", nullable: true),
                    MerchantName = table.Column<string>(type: "text", nullable: true),
                    LinkedTransactionId = table.Column<int>(type: "integer", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedTransactions_PlaidAccounts_PlaidAccountId",
                        column: x => x.PlaidAccountId,
                        principalTable: "PlaidAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedTransactions_Transactions_LinkedTransactionId",
                        column: x => x.LinkedTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransactions_LinkedTransactionId",
                table: "ImportedTransactions",
                column: "LinkedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransactions_PlaidAccountId",
                table: "ImportedTransactions",
                column: "PlaidAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransactions_UserId",
                table: "ImportedTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransactions_UserId_PlaidTransactionId",
                table: "ImportedTransactions",
                columns: new[] { "UserId", "PlaidTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaidAccounts_DefaultCategoryId",
                table: "PlaidAccounts",
                column: "DefaultCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaidAccounts_PlaidAccountId",
                table: "PlaidAccounts",
                column: "PlaidAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaidAccounts_PlaidItemId",
                table: "PlaidAccounts",
                column: "PlaidItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaidAccounts_UserId",
                table: "PlaidAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaidItems_PlaidItemId",
                table: "PlaidItems",
                column: "PlaidItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaidItems_UserId",
                table: "PlaidItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedTransactions");

            migrationBuilder.DropTable(
                name: "PlaidAccounts");

            migrationBuilder.DropTable(
                name: "PlaidItems");
        }
    }
}
