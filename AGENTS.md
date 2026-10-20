# AGENTS.md — dotnet-csharp-practice

## Repository Structure

```
dotnet-csharp-practice/
├── Budget-Desktop-App/         # Avalonia UI app (.NET 10.0-windows) — budget tracking
│   ├── Budget-Desktop-App.slnx   # Solution file
│   ├── Budget-Desktop-App.csproj # Project file
│   ├── Program.cs                # Avalonia entry point
│   ├── App.axaml / App.axaml.cs  # Application root
│   ├── MainWindow.axaml.cs       # Dashboard UI + CSV import
│   ├── Models/
│   │   ├── BudgetRow.cs          # Data model (date, description, category, amount, type)
│   │   ├── BudgetDetail.cs       # Aggregated budget summary
│   │   ├── BudgetRepository.cs   # SQLite CRUD + CSV parsing via CsvHelper
│   │   └── BudgetViewModel.cs    # Budget metrics (income/expense/net per month)
│   └── app.manifest
├── Reports-Desktop-App/         # Avalonia UI app (.NET 10.0)
├── stockanalyzer/               # Multi-project solution (.NET 6.0)
│   ├── StockAnalyzer.Core/      # Core library (Newtonsoft.Json)
│   ├── StockAnalyzer.Windows/   # WPF desktop app (references Core)
│   ├── StockAnalyzer.Web/       # ASP.NET Core Web API (references Core)
│   ├── StockAnalyzer.Tests/     # MSTest unit tests (references Core)
│   └── StockAnalyzer.AttachedDetached/  # Console app
├── class-demo/calc-demo/        # Console calculator demo
└── AGENTS.md                    # This file
```

## Key Commands

| Task | Command | From Directory |
|------|---------|----------------|
| Restore all | `dotnet restore` | `stockanalyzer/` or `Reports-Desktop-App/` |
| Build solution | `dotnet build StockAnalyzer.sln` | `stockanalyzer/` |
| Build single project | `dotnet build` | Any project folder |
| Run Budget-Desktop-App | `dotnet run` | `Budget-Desktop-App/` |
| Run Reports-Desktop-App | `dotnet run` | `Reports-Desktop-App/` |
| Run WPF app | `dotnet run` | `stockanalyzer/StockAnalyzer.Windows/` |
| Run Web API | `dotnet run` | `stockanalyzer/StockAnalyzer.Web/` |
| Run tests | `dotnet test StockAnalyzer.sln` | `stockanalyzer/` |
| Run tests (verbose) | `dotnet test --logger "console;verbosity=detailed"` | `stockanalyzer/StockAnalyzer.Tests/` |

## Important Notes

### Framework & Version Mismatches
- **Budget-Desktop-App** and **Reports-Desktop-App** target **net10.0** with **Avalonia 12.1.4**
- **stockanalyzer** projects target **net6.0** (Core/Windows/Web/Tests/AttachedDetached)
- Do not mix dependencies between these independent codebases

### UI Frameworks
- **Budget-Desktop-App** and **Reports-Desktop-App**: Avalonia (cross-platform XAML) — entry: `Program.cs` → `AppBuilder.Configure<App>()`
- **StockAnalyzer.Windows**: WPF (Windows-only) — `<UseWPF>true</UseWPF>` in csproj

### Testing
- **MSTest** (not xUnit/NUnit) — packages: `MSTest.TestFramework`, `MSTest.TestAdapter`, `Microsoft.NET.Test.Sdk`
- Test project: `stockanalyzer/StockAnalyzer.Tests/`
- Coverage: `coverlet.collector` included

### Data Files
- `StockPrices_Small.csv` in `StockAnalyzer.Core/` and `StockAnalyzer.Windows/`
- Marked `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` — ensure present when running apps/tests

### Dependencies
- Core library uses **Newtonsoft.Json 13.0.1** (not System.Text.Json)
- Web API uses minimal ASP.NET Core (no controllers visible — likely Minimal APIs)
- Avalonia projects use Avalonia 12.1.4
- **Budget-Desktop-App uses additionally**: `CsvHelper 33.0.1`, `Microsoft.Data.Sqlite 8.0.2`, `ClosedXML 0.100.0`, `Avalonia.Controls.DataGrid 12.1.2`
- **Budget-Desktop-App target framework**: `net10.0-windows` (uses Windows Forms for native file dialogs)

### Budget-Desktop-App Dependencies
- SQLite persistence at `~/budget.db` (configurable via constructor)
- CSV import: expects columns `date, description, category, amount, type`
- CSV parsing uses `CsvHelper` with configurable column names
- Charts: placeholder section in UI — can integrate LiveCharts2 or other charting library

### No Automation Configured
- No CI/CD (`.github/workflows/` absent)
- No pre-commit hooks
- No formatter/linter config (no `.editorconfig`, `stylecop.json`, `Directory.Build.props`)
- No `global.json` — uses machine-installed SDK

## Budget-Desktop-App Roadmap (Planned)

- **CSV import**: Load inflow/outflow from downloaded banking statements — borrow CSV loading layout and implementation from `Reports-Desktop-App/`
- **Charts & insights**: Pie charts and graphs for weekly, monthly, yearly, and complete history of spendings and earnings
- **Habit analysis**: Track spending patterns and net gains/losses over time

## Developer Workflow

1. **Open in VS/VS Code**: Open `stockanalyzer/StockAnalyzer.sln` or work in `Budget-Desktop-App/` and `Reports-Desktop-App/` independently
2. **Run Avalonia app**: `cd Reports-Desktop-App && dotnet run` or `cd Budget-Desktop-App && dotnet run`
3. **Run WPF app**: `cd stockanalyzer/StockAnalyzer.Windows && dotnet run`
4. **Run Web API**: `cd stockanalyzer/StockAnalyzer.Web && dotnet run` (check `launchSettings.json` for ports)
5. **Run tests**: `cd stockanalyzer && dotnet test`

## Common Issues

- **Missing CSV**: If tests/app fail with file-not-found, verify `StockPrices_Small.csv` copies to `bin/Debug/net6.0/`
- **SDK version**: Ensure .NET 6.0 SDK for stockanalyzer, .NET 10.0 SDK for Avalonia projects (Budget-Desktop-App and Reports-Desktop-App)
- **Platform target**: WPF project requires Windows (`net6.0-windows`); Avalonia is cross-platform