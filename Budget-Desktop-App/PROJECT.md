# PROJECT.md — Budget-Desktop-App

Created: 2026-10-20
Related session work: repo-wide `AGENTS.md`, `Reports-Desktop-App/` reference

## What this session covered

1. Updated `AGENTS.md` (repo root) to reflect `Budget-Desktop-App/` project
2. Investigated `Reports-Desktop-App/` for CSV loading patterns (CsvHelper, SQLite, Avalonia)
3. Created `Budget-Desktop-App.sln` and updated `.csproj`
4. Implemented full budgeting app with:
   - `Models/BudgetRow.cs` — data model
   - `Models/BudgetRepository.cs` — SQLite CRUD + CSV import
   - `Models/BudgetViewModel.cs` — budget metrics
   - `MainWindow.axaml` / `.cs` — dashboard UI with real-time KPI cards
5. Added repository methods: `GetOverallTotals()`, `GetMonthlySummary()`, `GetRecentTransactions()`
6. Wired dashboard to load real data from SQLite (`~/budget.db`)

---

## Repo context

- **Root repo:** `P:\repo\lcheunglci\dotnet-csharp-practice`
- **This folder:** `Budget-Desktop-App/` — Avalonia 12.1.4 desktop app (.NET 10.0-windows)
- **Sibling project:** `Reports-Desktop-App/` — source for CSV loading patterns (CsvHelper, SQLite, Avalonia)
- **Related instruction file:** `AGENTS.md` (repo root) — contains all commands, framework notes, roadmap

---

## Files created / updated

### Project structure (created)
```
Budget-Desktop-App/
├── Budget-Desktop-App.slnx          # Solution file
├── Budget-Desktop-App.csproj        # Updated with dependencies
├── Program.cs                       # Avalonia entry (existing)
├── App.axaml / App.axaml.cs         # Application root (existing)
├── MainWindow.axaml                 # Dashboard UI (rewritten)
├── MainWindow.axaml.cs              # Dashboard logic + CSV import (rewritten)
├── Models/
│   ├── BudgetRow.cs                 # Data model
│   ├── BudgetDetail.cs              # Summary model
│   ├── BudgetRepository.cs          # SQLite + CSV parsing
│   └── BudgetViewModel.cs           # Budget metrics
├── app.manifest                     # Windows manifest (existing)
└── PROJECT.md                       # This file
```

### Key implementation details

- **Target framework:** `net10.0-windows`
- **UI framework:** Avalonia 12.1.4 (Fluent theme, Inter font)
- **File dialogs:** `UseWindowsForms=true` (native `OpenFileDialog` for CSV selection)
- **CSV parsing:** `CsvHelper` with configurable column mapping (`date, description, category, amount, type`)
- **Persistence:** SQLite (`Microsoft.Data.Sqlite`) at `~/budget.db`
- **Dashboard:** Real-time KPI cards, monthly breakdown table, recent transactions list
- **Charts:** Placeholder area (ready for LiveCharts2 integration)
- **Dependencies added:** `CsvHelper 33.0.1`, `Microsoft.Data.Sqlite 8.0.2`, `ClosedXML 0.100.0`, `Avalonia.Controls.DataGrid 12.1.2`

---

## Build & verification

```
dotnet restore
# Restored (with pre-existing SQLite vulnerability warning)
dotnet build
# Build succeeded — 0 errors, 3 warnings (vulnerability + null reference)
```

---

## Data model (CSV format)

Expected CSV column headers (flexible via `CsvConfiguration`):
- `date` — MM/dd/yyyy or any parseable format
- `description` — transaction description
- `category` — spending category
- `amount` — decimal amount (positive = income, negative = expense based on `type`)
- `type` — `"Income"` or `"Expense"`

---

## References

- `AGENTS.md` (repo root): `P:\repo\lcheunglci\dotnet-csharp-practice\AGENTS.md`
- Source Avalonia patterns: `Reports-Desktop-App/`
- SQLite schema defined in `Models/BudgetRepository.cs`
- This file: `P:\repo\lcheunglci\dotnet-csharp-practice\Budget-Desktop-App\PROJECT.md`
