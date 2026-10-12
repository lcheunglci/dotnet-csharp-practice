using System;
using System.Collections.Generic;

namespace Reports_Desktop_App.Models;

/// <summary>
/// A single row of data within a report.
/// Mirrors the ReportRow shape from the Electron app.
/// </summary>
public class ReportRow
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public DateTime Date { get; set; }
    public int OrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
}

/// <summary>
/// Detailed report data including its rows.
/// Mirrors the ReportDetail shape from the Electron app.
/// </summary>
public class ReportDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public List<ReportRow> Rows { get; set; } = new();
}

/// <summary>
/// DTO for updating a single row (used in edit mode).
/// </summary>
public class ReportRowUpdate
{
    public string Date { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
}