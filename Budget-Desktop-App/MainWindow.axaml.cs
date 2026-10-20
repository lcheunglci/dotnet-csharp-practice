using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
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

        // Get overall totals across all budgets
        var totals = _repository.GetOverallTotals();

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

        // KPI Cards Row
        var kpiRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(8),
            Spacing = 16
        };

        // Total Spending Card
        var spendingCard = CreateKpiCard("Total Spending", totals.TotalExpenses.ToString("C"), "#F87171");
        kpiRow.Children.Add(spendingCard);

        // Total Earnings Card
        var earningsCard = CreateKpiCard("Total Earnings", totals.TotalIncome.ToString("C"), "#34D399");
        kpiRow.Children.Add(earningsCard);

        // Net Gain/Loss Card
        var netCard = CreateKpiCard("Net Gain/Loss", totals.NetAmount.ToString("C"), "#60A5FA");
        kpiRow.Children.Add(netCard);

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

        // Chart placeholder
        var chartPlaceholder = new Border
        {
            Margin = new Thickness(8),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = Avalonia.Media.Brushes.White,
            BorderBrush = Avalonia.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Height = 200
        };
        chartPlaceholder.Child = new TextBlock
        {
            Text = "Chart Area — Pie charts for categories, bar charts for monthly trends",
            FontSize = 14,
            Foreground = Avalonia.Media.Brushes.Gray,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        contentArea.Children.Add(chartPlaceholder);

        // Monthly breakdown section
        var monthlyHeader = new TextBlock
        {
            Text = "Monthly Spend / Earn (Historical Overview)",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Margin = new Thickness(8)
        };
        contentArea.Children.Add(monthlyHeader);

        // Get monthly summary data
        var monthlyData = _repository.GetMonthlySummary();

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
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto } } // Header row
        };

        monthlyTable.Children.Add(new TextBlock { Text = "Month", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 0);

        monthlyTable.Children.Add(new TextBlock { Text = "Income", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 1);

        monthlyTable.Children.Add(new TextBlock { Text = "Expenses", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 2);

        monthlyTable.Children.Add(new TextBlock { Text = "Net", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 3);

        // Add monthly rows
        int rowIndex = 1; // Start after header row (row 0)
        foreach (var month in monthlyData.Take(12)) // Show last 12 months
        {
            // Add new row definition for this month's data
            monthlyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Month
            monthlyTable.Children.Add(new TextBlock { Text = month.Month, Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 0);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            // Income
            monthlyTable.Children.Add(new TextBlock { Text = month.Income.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 1);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            // Expenses
            monthlyTable.Children.Add(new TextBlock { Text = month.Expenses.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 2);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            // Net
            monthlyTable.Children.Add(new TextBlock { Text = month.Net.ToString("C"), Margin = new Thickness(4) });
            Grid.SetColumn(monthlyTable.Children[monthlyTable.Children.Count - 1], 3);
            Grid.SetRow(monthlyTable.Children[monthlyTable.Children.Count - 1], rowIndex);

            rowIndex++;
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

        // Add recent transactions (up to 5)
        var recentRows = _repository.GetRecentTransactions(5);
        foreach (var row in recentRows)
        {
            recentTable.Children.Add(CreateTransactionRow(
                row.Date.ToString("yyyy-MM-dd"),
                row.Amount.ToString("C"),
                row.Type,
                row.Description
            ));
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
                    _repository.InsertBudget(Path.GetFileNameWithoutExtension(dlg.FileName), rows);
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