using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using SkiaSharp;
using Budget_Desktop_App.Models;

namespace Budget_Desktop_App;

public partial class MainWindow : Window
{
    private BudgetRepository _repository;

    public MainWindow()
    {
        InitializeComponent();
        _repository = new BudgetRepository();
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        var contentArea = this.FindControl<StackPanel>("ContentArea");
        if (contentArea == null) return;

        contentArea.Children.Clear();

        // Add welcome/info header
        var infoPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(8),
            Spacing = 16
        };
        infoPanel.Children.Add(new TextBlock
        {
            Text = "Budget Dashboard",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold
        });
        infoPanel.Children.Add(new TextBlock
        {
            Text = "Import CSV banking statements for inflow and outflow tracking.",
            FontSize = 14,
            Foreground = Avalonia.Media.Brushes.Gray,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        contentArea.Children.Add(infoPanel);

        // KPI Cards Row with real data from DB
        var totals = _repository.GetOverallTotals();
        var kpiRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(8),
            Spacing = 16
        };

        kpiRow.Children.Add(CreateKpiCard("Total Spending", totals.TotalExpenses.ToString("C"), "#F87171"));
        kpiRow.Children.Add(CreateKpiCard("Total Earnings", totals.TotalIncome.ToString("C"), "#34D399"));
        kpiRow.Children.Add(CreateKpiCard("Net Gain/Loss", totals.NetAmount.ToString("C"), "#60A5FA"));
        contentArea.Children.Add(kpiRow);

        // Chart placeholder section
        var chartHeader = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(8),
            Spacing = 8
        };
        chartHeader.Children.Add(new TextBlock
        {
            Text = "Spending & Earning Trends",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold
        });
        chartHeader.Children.Add(new TextBlock
        {
            Text = "(Weekly / Monthly / Yearly / Full History — add charts here)",
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Gray,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        contentArea.Children.Add(chartHeader);

        // Chart using LiveChartsCore (v2.1.0-dev-798 for Avalonia 12)
        try
        {
            var chartSummary = _repository.GetMonthlySummary();
            var incomeVal = chartSummary.FirstOrDefault()?.Income ?? 0m;
            var expenseVal = chartSummary.FirstOrDefault()?.Expenses ?? 0m;

            if (incomeVal == 0 && expenseVal == 0)
            {
                contentArea.Children.Add(new TextBlock
                {
                    Text = "No data yet — import a CSV (SampleData/october-2026.csv) to see charts",
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Thickness(8)
                });
            }
            else
            {
                // Custom polyline pie chart (explicit colors, no arc geometry quirks)
                var totalVal = (double)(incomeVal + expenseVal);
                var incomePct = totalVal > 0 ? (double)incomeVal / totalVal : 0.5;
                var expensePct = totalVal > 0 ? (double)expenseVal / totalVal : 0.5;

                const double cx = 125, cy = 125, r = 100;
                var green = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightGreen);
                var red = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.IndianRed);
                int sliceSteps = 120;
                int incomeSteps = Math.Max(2, (int)Math.Round(sliceSteps * incomePct));
                int expenseSteps = Math.Max(2, (int)Math.Round(sliceSteps * expensePct));

                // Income wedge
                var incomeFig = new PathFigure
                {
                    StartPoint = new Avalonia.Point(cx + r * Math.Cos(-Math.PI / 2), cy + r * Math.Sin(-Math.PI / 2)),
                    IsFilled = true,
                    IsClosed = true,
                    Segments = new PathSegments()
                };
                for (int i = 1; i <= incomeSteps; i++)
                {
                    double a = -Math.PI / 2 + (i / (double)incomeSteps) * incomePct * 2 * Math.PI;
                    incomeFig.Segments.Add(new LineSegment { Point = new Avalonia.Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a)) });
                }
                incomeFig.Segments.Add(new LineSegment { Point = new Avalonia.Point(cx, cy) });

                // Expense wedge continues clockwise
                var expenseFig = new PathFigure
                {
                    StartPoint = new Avalonia.Point(cx + r * Math.Cos(-Math.PI / 2 + incomePct * 2 * Math.PI), cy + r * Math.Sin(-Math.PI / 2 + incomePct * 2 * Math.PI)),
                    IsFilled = true,
                    IsClosed = true,
                    Segments = new PathSegments()
                };
                for (int i = 1; i <= expenseSteps; i++)
                {
                    double a = -Math.PI / 2 + incomePct * 2 * Math.PI + (i / (double)expenseSteps) * expensePct * 2 * Math.PI;
                    expenseFig.Segments.Add(new LineSegment { Point = new Avalonia.Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a)) });
                }
                expenseFig.Segments.Add(new LineSegment { Point = new Avalonia.Point(cx, cy) });

                var piePanel = new StackPanel { Margin = new Thickness(8), Spacing = 12, Width = 700 };
                var pieCanvas = new Canvas { Width = 250, Height = 250 };
                pieCanvas.Children.Add(new Avalonia.Controls.Shapes.Path { Fill = green, Data = new PathGeometry { Figures = new PathFigures { incomeFig } } });
                pieCanvas.Children.Add(new Avalonia.Controls.Shapes.Path { Fill = red, Data = new PathGeometry { Figures = new PathFigures { expenseFig } } });

                var legendStack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, Spacing = 4 };
                legendStack.Children.Add(new TextBlock
                {
                    Text = $"Income: {incomeVal:C}  ({incomePct:P0})",
                    Foreground = green,
                    FontSize = 14,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });
                legendStack.Children.Add(new TextBlock
                {
                    Text = $"Expenses: {expenseVal:C}  ({expensePct:P0})",
                    Foreground = red,
                    FontSize = 14,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                piePanel.Children.Add(pieCanvas);
                piePanel.Children.Add(legendStack);
                contentArea.Children.Add(piePanel);
            }
        }
        catch
        {
            contentArea.Children.Add(new TextBlock
            {
                Text = "Chart area — LiveChartsCore ready",
                FontSize = 12,
                Foreground = Avalonia.Media.Brushes.Gray,
                Margin = new Thickness(8)
            });
        }

        // Monthly breakdown section
        var monthlyHeader = new TextBlock
        {
            Text = "Monthly Spend / Earn (Historical Overview)",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(8)
        };
        contentArea.Children.Add(monthlyHeader);

        // Simple monthly breakdown table
        var monthlyTable = new Grid
        {
            Margin = new Thickness(8),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(120) },
                new ColumnDefinition { Width = new GridLength(120) },
                new ColumnDefinition { Width = new GridLength(120) },
                new ColumnDefinition { Width = new GridLength(120, GridUnitType.Star) }
            },
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto } }
        };

        monthlyTable.Children.Add(new TextBlock { Text = "Month", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 0);

        monthlyTable.Children.Add(new TextBlock { Text = "Income", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 1);

        monthlyTable.Children.Add(new TextBlock { Text = "Expenses", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 2);

        monthlyTable.Children.Add(new TextBlock { Text = "Net", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 3);

        // Add monthly rows from database
        var monthlySummary = _repository.GetMonthlySummary();
        int rowIndex = 1; // Row 0 is the header
        foreach (var month in monthlySummary.Take(12))
        {
            monthlyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            monthlyTable.Children.Add(new TextBlock { Text = month.Month, Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 0);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            monthlyTable.Children.Add(new TextBlock { Text = month.Income.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 1);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            monthlyTable.Children.Add(new TextBlock { Text = month.Expenses.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 2);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            monthlyTable.Children.Add(new TextBlock { Text = month.Net.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 3);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            rowIndex++;
        }

        if (monthlySummary.Count == 0)
        {
            monthlyTable.Children.Add(new TextBlock
            {
                Text = "No data yet — import a CSV to populate this table.",
                FontSize = 12,
                Foreground = Avalonia.Media.Brushes.Gray,
                Margin = new Thickness(4)
            });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 0);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], 1);
        }

        contentArea.Children.Add(monthlyTable);

        // Recent transactions preview
        var recentHeader = new TextBlock
        {
            Text = "Recent Transactions",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(8)
        };
        contentArea.Children.Add(recentHeader);

        var recentTable = new StackPanel
        {
            Margin = new Thickness(8),
            Spacing = 4
        };

        // Add recent transactions from database
        var recentRows = _repository.GetRecentTransactions(5);
        if (recentRows.Count == 0)
        {
            recentTable.Children.Add(new TextBlock
            {
                Text = "No transactions yet — import a CSV to see recent activity.",
                FontSize = 12,
                Foreground = Avalonia.Media.Brushes.Gray
            });
        }
        else
        {
            foreach (var row in recentRows)
            {
                recentTable.Children.Add(CreateTransactionRow(
                    row.Date.ToString("yyyy-MM-dd"),
                    row.Amount.ToString("C"),
                    row.Type,
                    row.Description
                ));
            }
        }
        contentArea.Children.Add(recentTable);
    }

    private Border CreateKpiCard(string title, string value, string color)
    {
        var card = new Border
        {
            Margin = new Thickness(4),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = Avalonia.Media.Brushes.White,
            BorderBrush = Avalonia.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Width = 240
        };

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Gray
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(color))
        });

        card.Child = stack;
        return card;
    }

    private Border CreateTransactionRow(string date, string amount, string type, string desc)
    {
        var row = new Border
        {
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(8, 4),
            CornerRadius = new CornerRadius(4),
            Background = Avalonia.Media.Brushes.White,
            BorderBrush = Avalonia.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(100) },
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(400, GridUnitType.Star) }
            }
        };

        grid.Children.Add(new TextBlock { Text = date, FontSize = 11, Margin = new Thickness(2) });
        Grid.SetColumn(grid.Children[grid.Children.Count - 1], 0);

        grid.Children.Add(new TextBlock { Text = amount, FontSize = 11, FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(2) });
        Grid.SetColumn(grid.Children[grid.Children.Count - 1], 1);

        grid.Children.Add(new TextBlock { Text = type, FontSize = 11, Margin = new Thickness(2) });
        Grid.SetColumn(grid.Children[grid.Children.Count - 1], 2);

        grid.Children.Add(new TextBlock { Text = desc, FontSize = 11, Foreground = Avalonia.Media.Brushes.Gray, Margin = new Thickness(2) });
        Grid.SetColumn(grid.Children[grid.Children.Count - 1], 3);

        row.Child = grid;
        return row;
    }

    private void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        using (var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv", Title = "Select Budget CSV" })
        {
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var rows = BudgetRepository.ParseCsv(dlg.FileName);
                    _repository.InsertBudget(System.IO.Path.GetFileNameWithoutExtension(dlg.FileName), rows);
                    RefreshDashboard();
                }
                catch (Exception ex)
                {
                    var contentArea = this.FindControl<StackPanel>("ContentArea");
                    if (contentArea != null)
                        contentArea.Children.Add(new TextBlock
                        {
                            Text = $"Import error: {ex.Message}",
                            FontSize = 14,
                            Foreground = Avalonia.Media.Brushes.Red,
                            Margin = new Thickness(16)
                        });
                }
            }
        }
    }

    private void OnExitClicked(object? sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            this.Close();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}