using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using CsvHelper;
using CsvHelper.Configuration;

namespace Budget_Desktop_App.Models
{
    public class BudgetRepository
    {
        private readonly string _dbPath;

        public BudgetRepository(string? dbPath = null)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dbPath = Path.Combine(appData, "budget.db");
            }
            _dbPath = dbPath;
            EnsureSchema();
        }

        private void EnsureSchema()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS budgets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    file_name TEXT NOT NULL,
                    imported_at TEXT NOT NULL,
                    total_income REAL NOT NULL,
                    total_expenses REAL NOT NULL,
                    net_amount REAL NOT NULL
                );

                CREATE TABLE IF NOT EXISTS budget_rows (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    budget_id INTEGER NOT NULL REFERENCES budgets(id) ON DELETE CASCADE,
                    date TEXT NOT NULL,
                    description TEXT NOT NULL,
                    category TEXT NOT NULL,
                    amount REAL NOT NULL,
                    type TEXT NOT NULL
                );";

            cmd.ExecuteNonQuery();
        }

        public BudgetDetail InsertBudget(string fileName, List<BudgetRow> rows)
        {
            var totalIncome = rows.Where(r => r.Type == "Income").Sum(r => r.Amount);
            var totalExpenses = rows.Where(r => r.Type == "Expense").Sum(r => r.Amount);
            var netAmount = totalIncome - totalExpenses;

            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            using var tx = conn.BeginTransaction();

            // Insert budget header
            using var budgetCmd = conn.CreateCommand();
            budgetCmd.Transaction = tx;
            budgetCmd.CommandText = @"
                INSERT INTO budgets (file_name, imported_at, total_income, total_expenses, net_amount)
                VALUES (@fileName, @importedAt, @totalIncome, @totalExpenses, @netAmount);
                SELECT last_insert_rowid();";

            var now = DateTime.UtcNow.ToString("o");
            budgetCmd.Parameters.AddWithValue("@fileName", fileName);
            budgetCmd.Parameters.AddWithValue("@importedAt", now);
            budgetCmd.Parameters.AddWithValue("@totalIncome", totalIncome);
            budgetCmd.Parameters.AddWithValue("@totalExpenses", totalExpenses);
            budgetCmd.Parameters.AddWithValue("@netAmount", netAmount);
            var budgetId = Convert.ToInt32(budgetCmd.ExecuteScalar());

            // Insert rows
            using var rowCmd = conn.CreateCommand();
            rowCmd.Transaction = tx;
            rowCmd.CommandText = @"
                INSERT INTO budget_rows (budget_id, date, description, category, amount, type)
                VALUES (@budgetId, @date, @description, @category, @amount, @type)";

            foreach (var row in rows)
            {
                rowCmd.Parameters.AddWithValue("@budgetId", budgetId);
                rowCmd.Parameters.AddWithValue("@date", row.Date.ToString("yyyy-MM-dd"));
                rowCmd.Parameters.AddWithValue("@description", row.Description);
                rowCmd.Parameters.AddWithValue("@category", row.Category);
                rowCmd.Parameters.AddWithValue("@amount", row.Amount);
                rowCmd.Parameters.AddWithValue("@type", row.Type);
                rowCmd.ExecuteNonQuery();
                rowCmd.Parameters.Clear();
            }

            tx.Commit();

            return GetBudget(budgetId);
        }

        public BudgetDetail GetBudget(int id)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            // Get budget header
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, file_name, imported_at, total_income, total_expenses, net_amount
                FROM budgets WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var detail = new BudgetDetail
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                ImportedAt = reader.GetDateTime(2),
                TotalIncome = reader.GetDecimal(3),
                TotalExpenses = reader.GetDecimal(4),
                NetAmount = reader.GetDecimal(5),
                Rows = new()
            };

            // Get rows
            using var rowCmd = conn.CreateCommand();
            rowCmd.CommandText = @"
                SELECT id, budget_id, date, description, category, amount, type
                FROM budget_rows WHERE budget_id = @id ORDER BY date";
            rowCmd.Parameters.AddWithValue("@id", id);
            using var rowReader = rowCmd.ExecuteReader();
            while (rowReader.Read())
            {
                detail.Rows.Add(new BudgetRow
                {
                    Id = rowReader.GetInt32(0),
                    BudgetId = rowReader.GetInt32(1),
                    Date = rowReader.GetDateTime(2),
                    Description = rowReader.GetString(3),
                    Category = rowReader.GetString(4),
                    Amount = rowReader.GetDecimal(5),
                    Type = rowReader.GetString(6)
                });
            }

            return detail;
        }

        /// <summary>
        /// Gets aggregated totals across all budgets in the database.
        /// </summary>
        public BudgetTotals GetOverallTotals()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    COALESCE(SUM(total_income), 0) as total_income,
                    COALESCE(SUM(total_expenses), 0) as total_expenses,
                    COALESCE(SUM(net_amount), 0) as net_amount
                FROM budgets";

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new BudgetTotals
                {
                    TotalIncome = reader.GetDecimal(0),
                    TotalExpenses = reader.GetDecimal(1),
                    NetAmount = reader.GetDecimal(2)
                };
            }

            return new BudgetTotals();
        }

        /// <summary>
        /// Gets monthly breakdown of income/expenses across all budgets.
        /// </summary>
        public List<MonthlySummary> GetMonthlySummary()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    strftime('%Y-%m', date) as month,
                    SUM(CASE WHEN type = 'Income' THEN amount ELSE 0 END) as income,
                    SUM(CASE WHEN type = 'Expense' THEN amount ELSE 0 END) as expenses
                FROM budget_rows
                GROUP BY strftime('%Y-%m', date)
                ORDER BY month DESC";

            using var reader = cmd.ExecuteReader();
            var results = new List<MonthlySummary>();
            while (reader.Read())
            {
                var monthStr = reader.GetString(0);
                // Convert YYYY-MM to readable format
                if (DateTime.TryParseExact(monthStr + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var monthDate))
                {
                    results.Add(new MonthlySummary
                    {
                        Month = monthDate.ToString("MMMM yyyy"),
                        Income = reader.GetDecimal(1),
                        Expenses = reader.GetDecimal(2),
                        Net = reader.GetDecimal(1) - reader.GetDecimal(2)
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Gets recent transactions across all budgets.
        /// </summary>
        public List<BudgetRow> GetRecentTransactions(int limit = 10)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, budget_id, date, description, category, amount, type
                FROM budget_rows
                ORDER BY date DESC
                LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            var results = new List<BudgetRow>();
            while (reader.Read())
            {
                results.Add(new BudgetRow
                {
                    Id = reader.GetInt32(0),
                    BudgetId = reader.GetInt32(1),
                    Date = reader.GetDateTime(2),
                    Description = reader.GetString(3),
                    Category = reader.GetString(4),
                    Amount = reader.GetDecimal(5),
                    Type = reader.GetString(6)
                });
            }

            return results;
        }

        public static List<BudgetRow> ParseCsv(string filePath)
        {
            var rows = new List<BudgetRow>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<CsvBudgetRow>();

            foreach (var record in records)
            {
                var dateStr = record.Date?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(dateStr))
                    continue;

                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    rows.Add(new BudgetRow
                    {
                        Date = parsedDate,
                        Description = record.Description ?? string.Empty,
                        Category = record.Category ?? string.Empty,
                        Amount = record.Amount ?? 0m,
                        Type = record.Type ?? string.Empty
                    });
                }
            }

            return rows;
        }

        private class CsvBudgetRow
        {
            [CsvHelper.Configuration.Attributes.Name("date")]
            public string? Date { get; set; }
            [CsvHelper.Configuration.Attributes.Name("description")]
            public string? Description { get; set; }
            [CsvHelper.Configuration.Attributes.Name("category")]
            public string? Category { get; set; }
            [CsvHelper.Configuration.Attributes.Name("amount")]
            public decimal? Amount { get; set; }
            [CsvHelper.Configuration.Attributes.Name("type")]
            public string? Type { get; set; }
        }
    }

    public class BudgetDetail
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetAmount { get; set; }
        public List<BudgetRow> Rows { get; set; } = new();
    }

    public class BudgetTotals
    {
        public decimal TotalIncome { get; set; } = 0m;
        public decimal TotalExpenses { get; set; } = 0m;
        public decimal NetAmount { get; set; } = 0m;
    }

    public class MonthlySummary
    {
        public string Month { get; set; } = string.Empty;
        public decimal Income { get; set; } = 0m;
        public decimal Expenses { get; set; } = 0m;
        public decimal Net { get; set; } = 0m;
    }
}