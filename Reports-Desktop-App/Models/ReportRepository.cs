using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;

namespace Reports_Desktop_App.Models;

/// <summary>
/// Repository for report data using SQLite.
/// Mirrors the db.js module from the Electron/Angular app.
/// All operations are in-process; no IPC bridge needed.
/// </summary>
public class ReportRepository
{
    private readonly string _dbPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportRepository"/> class.
    /// Creates the database and schema if they don't exist.
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file. If null/empty, uses a default in the user's data folder.</param>
    public ReportRepository(string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            // Use a default location in the app's user data directory
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dbPath = Path.Combine(appData, "reports.db");
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
            CREATE TABLE IF NOT EXISTS reports (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                source_file TEXT NOT NULL,
                imported_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS report_rows (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                report_id INTEGER NOT NULL REFERENCES reports(id) ON DELETE CASCADE,
                date TEXT NOT NULL,
                order_id INTEGER NOT NULL,
                description TEXT NOT NULL,
                cost REAL NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    #region Report CRUD

    /// <summary>
    /// Inserts a new report with its rows.
    /// </summary>
    public ReportDetail InsertReport(string name, string sourceFile, List<ReportRow> rows)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var tx = conn.BeginTransaction();

        // Insert report header
        using var repCmd = conn.CreateCommand();
        repCmd.Transaction = tx;
        repCmd.CommandText = @"
            INSERT INTO reports (name, source_file, imported_at)
            VALUES (@name, @sourceFile, @importedAt);
            SELECT last_insert_rowid();";
        var now = DateTime.UtcNow.ToString("o");
        repCmd.Parameters.AddWithValue("@name", name);
        repCmd.Parameters.AddWithValue("@sourceFile", sourceFile);
        repCmd.Parameters.AddWithValue("@importedAt", now);
        var reportId = Convert.ToInt32(repCmd.ExecuteScalar());

        // Insert rows
        using var rowCmd = conn.CreateCommand();
        rowCmd.Transaction = tx;
        rowCmd.CommandText = @"
            INSERT INTO report_rows (report_id, date, order_id, description, cost)
            VALUES (@reportId, @date, @orderId, @description, @cost)";

        foreach (var row in rows)
        {
            rowCmd.Parameters.AddWithValue("@reportId", reportId);
            rowCmd.Parameters.AddWithValue("@date", row.Date.ToString("yyyy-MM-dd"));
            rowCmd.Parameters.AddWithValue("@orderId", row.OrderId);
            rowCmd.Parameters.AddWithValue("@description", row.Description);
            rowCmd.Parameters.AddWithValue("@cost", row.Cost);
            rowCmd.ExecuteNonQuery();
            rowCmd.Parameters.Clear();
        }

        tx.Commit();

        return GetReport(reportId);
    }

    /// <summary>
    /// Gets a single report by ID, including all its rows.
    /// </summary>
    public ReportDetail? GetReport(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        // Get report header
        using var repCmd = conn.CreateCommand();
        repCmd.CommandText = @"
            SELECT id, name, source_file, imported_at
            FROM reports WHERE id = @id";
        repCmd.Parameters.AddWithValue("@id", id);
        using var repReader = repCmd.ExecuteReader();
        if (!repReader.Read())
            return null;

        var detail = new ReportDetail
        {
            Id = repReader.GetInt32(0),
            Name = repReader.GetString(1),
            SourceFile = repReader.GetString(2),
            ImportedAt = repReader.GetDateTime(3),
            Rows = new()
        };

        // Get rows
        using var rowCmd = conn.CreateCommand();
        rowCmd.CommandText = @"
            SELECT id, report_id, date, order_id, description, cost
            FROM report_rows WHERE report_id = @id ORDER BY id";
        rowCmd.Parameters.AddWithValue("@id", id);
        using var rowReader = rowCmd.ExecuteReader();
        while (rowReader.Read())
        {
            detail.Rows.Add(new ReportRow
            {
                Id = rowReader.GetInt32(0),
                ReportId = rowReader.GetInt32(1),
                Date = rowReader.GetDateTime(2),
                OrderId = rowReader.GetInt32(3),
                Description = rowReader.GetString(4),
                Cost = rowReader.GetDecimal(5)
            });
        }

        return detail;
    }

    /// <summary>
    /// Renames a report.
    /// </summary>
    public bool RenameReport(int id, string newName)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE reports SET name = @newName WHERE id = @id";
        cmd.Parameters.AddWithValue("@newName", newName);
        cmd.Parameters.AddWithValue("@id", id);
        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    /// <summary>
    /// Updates a single row.
    /// </summary>
    public ReportRow? UpdateRow(int rowId, ReportRowUpdate values)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE report_rows
            SET date = @date, order_id = @orderId, description = @description, cost = @cost
            WHERE id = @rowId";
        cmd.Parameters.AddWithValue("@date", values.Date);
        cmd.Parameters.AddWithValue("@orderId", values.OrderId);
        cmd.Parameters.AddWithValue("@description", values.Description);
        cmd.Parameters.AddWithValue("@cost", values.Cost);
        cmd.Parameters.AddWithValue("@rowId", rowId);

        var rowsAffected = cmd.ExecuteNonQuery();
        if (rowsAffected == 0)
            return null;

        // Return the updated row
        using var readCmd = conn.CreateCommand();
        readCmd.CommandText = @"
            SELECT id, report_id, date, order_id, description, cost
            FROM report_rows WHERE id = @rowId";
        readCmd.Parameters.AddWithValue("@rowId", rowId);
        using var reader = readCmd.ExecuteReader();
        if (reader.Read())
        {
            return new ReportRow
            {
                Id = reader.GetInt32(0),
                ReportId = reader.GetInt32(1),
                Date = reader.GetDateTime(2),
                OrderId = reader.GetInt32(3),
                Description = reader.GetString(4),
                Cost = reader.GetDecimal(5)
            };
        }

        return null;
    }

    /// <summary>
    /// Deletes a report (and its rows via CASCADE).
    /// </summary>
    public bool DeleteReport(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM reports WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var rowsAffected = cmd.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    /// <summary>
    /// Lists all reports with row counts.
    /// </summary>
    public List<ReportSummary> ListReports()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT r.id, r.name, r.source_file, r.imported_at,
                   (SELECT COUNT(*) FROM report_rows WHERE report_id = r.id) AS row_count
            FROM reports r
            ORDER BY r.imported_at DESC";
        using var reader = cmd.ExecuteReader();
        var results = new List<ReportSummary>();
        while (reader.Read())
        {
            results.Add(new ReportSummary
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SourceFile = reader.GetString(2),
                ImportedAt = reader.GetDateTime(3),
                RowCount = reader.GetInt32(4)
            });
        }
        return results;
    }

    /// <summary>
    /// Clears all reports (drops all data).
    /// </summary>
    public int ClearAllReports()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM reports";
        return cmd.ExecuteNonQuery();
    }

    #endregion

    #region CSV Import

    /// <summary>
    /// Parses a CSV file and returns a list of ReportRow objects.
    /// Mirrors the parseCsv function from the Electron app's db.js.
    /// </summary>
    public static List<ReportRow> ParseCsv(string filePath)
    {
        var rows = new List<ReportRow>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<CsvReportRow>();

        foreach (var record in records)
        {
            var dateStr = record.Date?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(dateStr))
                continue; // skip rows without a date (same filter as Papa.parse skipEmptyLines)

            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                rows.Add(new ReportRow
                {
                    Date = parsedDate,
                    OrderId = record.OrderId ?? 0,
                    Description = record.Description ?? string.Empty,
                    Cost = record.Cost ?? 0m
                });
            }
        }

        return rows;
    }

    /// <summary>
    /// Helper record for CSV parsing (keeps column names flexible).
    /// </summary>
    private class CsvReportRow
    {
        [CsvHelper.Configuration.Attributes.Name("date")]
        public string? Date { get; set; }
        [CsvHelper.Configuration.Attributes.Name("orderId")]
        public int? OrderId { get; set; }
        [CsvHelper.Configuration.Attributes.Name("description")]
        public string? Description { get; set; }
        [CsvHelper.Configuration.Attributes.Name("cost")]
        public decimal? Cost { get; set; }
    }

    #endregion

    #region Export

    /// <summary>
    /// Exports a report's rows to a JSON file.
    /// Mirrors the exportRowsToJson function from db.js.
    /// </summary>
    public static bool ExportToJson(ReportDetail detail, string filePath)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(detail.Rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Exports a report's rows to an XLSX file.
    /// Mirrors the exportRowsToSpreadsheet function from db.js.
    /// </summary>
    public static bool ExportToXlsx(ReportDetail detail, string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");

            // Header row
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Order ID";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Cost";

            // Data rows
            for (int i = 0; i < detail.Rows.Count; i++)
            {
                var row = detail.Rows[i];
                worksheet.Cell(i + 2, 1).Value = row.Date.ToString("yyyy-MM-dd");
                worksheet.Cell(i + 2, 2).Value = row.OrderId;
                worksheet.Cell(i + 2, 3).Value = row.Description;
                worksheet.Cell(i + 2, 4).Value = (double)row.Cost;
            }

            workbook.SaveAs(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}