using System;
using System.Collections.Generic;

namespace Reports_Desktop_App.Models;

/// <summary>
/// Summary of a report (used in the reports list view).
/// Mirrors the ReportSummary shape from the Electron app.
/// </summary>
public class ReportSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public int RowCount { get; set; }
}
