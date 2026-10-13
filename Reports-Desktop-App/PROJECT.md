# PROJECT.md — Reports-Desktop-App (Avalonia)

Created: 2026-10-11
Related session work: repo-wide `AGENTS.md`, Electron port feasibility study

## What this session covered

1. Created `AGENTS.md` at repo root (`P:\repo\lcheunglci\dotnet-csharp-practice\AGENTS.md`)
2. Investigated `P:\repo\lcheunglci\angular-practice\report-csv-electron` (Electron + Angular 20)
3. Investigated current Avalonia project (`P:\repo\lcheunglci\dotnet-csharp-practice\Reports-Desktop-App/`)
4. Assessed feasibility of porting the Electron report-app to Avalonia

---

## Repo context

- **Root repo:** `P:\repo\lcheunglci\dotnet-csharp-practice`
- **This folder:** `Reports-Desktop-App/` — Avalonia 12.1.4 desktop app (.NET 10.0, WinExe)
- **Sibling codebase (candidate source):** `angular-practice/report-csv-electron/` — Electron desktop + Angular 20 + SQLite (`better-sqlite3`)

---

## Electron/Angular source app — key findings

Read from `report-csv-electron/`:
- `package.json`: Angular 20, Electron 41.7.1, `better-sqlite3`, `papaparse`, `xlsx`, `ng-bootstrap`
- `main.js`: Main Electron process — creates `BrowserWindow`, registers `ipcMain` handlers for CSV import/export/CRUD
- `preload.js`: Exposes `reportApi` to renderer via `contextBridge` (`openCsvDialog`, `importReport`, `listReports`, `getReport`, `renameReport`, `updateRow`, `deleteReport`, `clearAllReports`, `exportReport`)
- `db.js`: SQLite DB (`reports` + `report_rows` tables), CSV parsing via `Papa.parse`, exports to JSON / XLSX via `xlsx`
- Angular components: `reports-list.component`, `report-detail.component`, `add-report-modal.component`, `export.component`, `confirm-clear-all.component`
- Data model (TypeScript): `ReportSummary`, `ReportDetail`, `ReportRow`, `ReportRowUpdate`
- Features: import CSV, list reports, view/sort/edit rows, rename reports, copy-row-to-clipboard, delete, clear-all, export JSON/ODS/XLSX, modal dialogs

Database schema (from `db.js`):
```sql
CREATE TABLE reports (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, source_file TEXT, imported_at TEXT);
CREATE TABLE report_rows (id INTEGER PRIMARY KEY AUTOINCREMENT, report_id INTEGER REFERENCES reports(id) ON DELETE CASCADE, date TEXT, order_id INTEGER, description TEXT, cost REAL);
```

---

## Current Avalonia project state

Files read from `Reports-Desktop-App/`:
- `Reports-Desktop-App.csproj`: Avalonia 12.1.4, Fluent theme, Inter font, DiagnosticsSupport (Debug only). Target `net10.0`. No extra packages.
- `Program.cs`: Standard Avalonia entry (`AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace()`)
- `App.axaml`: Standard Avalonia `Application` root
- `MainWindow.axaml` / `MainWindow.axaml.cs`: Default window

**Status:** Implemented — `ReportRepository` (SQLite CRUD + CSV import via `CsvHelper` + JSON/XLSX export via `ClosedXML`), `MainWindow.axaml` UI with sidebar navigation, card-based reports list (`DataGrid`-style cards), import via `WinForms.OpenFileDialog`, export via `WinForms.SaveFileDialog`. Build passes (`net10.0-windows`, `net10.0-windows` for WinForms support). `FindControl<Button>` wiring fixed; `Window.Styles` hover states added. CSV header mapping fixed with `[Name]` attributes. `AGENTS.md` updated. `Reports-Desktop-App.sln` added.

---

## Port feasibility assessment (from session)

**Verdict:** Possible, but a **rewrite**, not a direct port.

| Electron/Angular feature | Avalonia equivalent / notes |
|---|---|
| Angular components / XAML templates | Avalonia `.axaml` + code-behind; no routing framework |
| `db.js` (SQLite + CSV + export) | Rebuild with `Microsoft.Data.Sqlite`, `CsvHelper`, `ClosedXML` |
| `main.js` / `preload.js` IPC | Not needed — all in-process in .NET |
| `papaparse` CSV import | `CsvHelper` NuGet package |
| `xlsx` export | `ClosedXML` or `EPPlus` NuGet |
| Electron `dialog.showOpenDialog` | Avalonia `FilePicker` / native `OpenFileDialog` |
| `ng-bootstrap` modals | Avalonia `Window` (dialog) or `ContentDialog` |
| Angular Router | Manual `UserControl` navigation |
| `clipboard` API | `Clipboard.SetTextAsync()` (Avalonia) /

**Recommended approach:**
1. Add NuGet packages: `CsvHelper`, `Microsoft.Data.Sqlite`, `ClosedXML`
2. Create `ReportRepository` (C#) mirroring `db.js`
3. Build Avalonia `UserControl`s for list/detail/modal/export
4. Replace Electron dialogs with Avalonia `FilePicker`

Full comparison table and workflow notes are in the session response above (provided in conversation).

---

## References

- `AGENTS.md` (repo root): `P:\repo\lcheunglci\dotnet-csharp-practice\AGENTS.md`
- Source Electron app: `P:\repo\lcheunglci\angular-practice\report-csv-electron/`
- Source Avalonia app: `P:\repo\lcheunglci\dotnet-csharp-practice\Reports-Desktop-App/`
- Related sibling solutions: `stockanalyzer/` (multi-project .NET 6.0, WPF/Web/Tests)
- This file: `P:\repo\lcheunglci\dotnet-csharp-practice\Reports-Desktop-App\PROJECT.md`
