using System;
using System.IO;
using System.Windows.Forms;
using Button = Avalonia.Controls.Button;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Reports_Desktop_App.Models;

namespace Reports_Desktop_App;

public partial class MainWindow : Window
{
    private ReportRepository _repository;

    public MainWindow()
    {
        InitializeComponent();
        _repository = new ReportRepository();

        // Initialize with reports list
        RefreshReports();
    }

    /// <summary>
    /// Refreshes the reports list display.
    /// </summary>
    private void RefreshReports()
    {
        var reports = _repository.ListReports();

        // Find the ContentArea (StackPanel in XAML)
        var contentArea = this.FindControl<StackPanel>("ContentArea");
        if (contentArea == null) return;

        contentArea.Children.Clear();

        if (reports == null || reports.Count == 0)
        {
            contentArea.Children.Add(new TextBlock
            {
                Text = "No reports found. Import a CSV file to get started.",
                FontSize = 14,
                Margin = new Thickness(16)
            });
            return;
        }

        // Show a header
        var header = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(16),
            Children = {
                new TextBlock { Text = "Reports", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new TextBlock { Text = $"({reports.Count} reports)", FontSize = 12, Foreground = Avalonia.Media.Brushes.Gray, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 4, 0, 0) }
            }
        };
        contentArea.Children.Add(header);

        // Show reports as cards
        foreach (var report in reports)
        {
            var card = CreateReportCard(report);
            contentArea.Children.Add(card);
        }
    }

    private Border CreateReportCard(ReportSummary report)
    {
        var card = new Border
        {
            Margin = new Thickness(8),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = Avalonia.Media.Brushes.White,
            BorderBrush = Avalonia.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel { Spacing = 8 };

        // Report name
        var nameBlock = new TextBlock
        {
            Text = report.Name,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            FontSize = 16,
            Foreground = Avalonia.Media.Brushes.Black
        };
        stack.Children.Add(nameBlock);

        // Metadata row
        var metaRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 16
        };
        metaRow.Children.Add(new TextBlock { Text = $"Source: {report.SourceFile}", Foreground = Avalonia.Media.Brushes.Black, FontSize = 11 });
        metaRow.Children.Add(new TextBlock { Text = $"Imported: {report.ImportedAt:yyyy-MM-dd HH:mm}", Foreground = Avalonia.Media.Brushes.Black, FontSize = 11 });
        metaRow.Children.Add(new TextBlock { Text = $"Rows: {report.RowCount}", Foreground = Avalonia.Media.Brushes.Black, FontSize = 11 });
        stack.Children.Add(metaRow);

        // Action buttons
        var actions = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var viewBtn = new TextBlock
        {
            Text = "View",
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkBlue),
            Foreground = Avalonia.Media.Brushes.White,
            Padding = new Thickness(8, 4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        viewBtn.PointerPressed += (s, e) => ShowReportDetail(report);
        actions.Children.Add(viewBtn);

        var deleteBtn = new TextBlock
        {
            Text = "Delete",
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkRed),
            Foreground = Avalonia.Media.Brushes.White,
            Padding = new Thickness(8, 4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        deleteBtn.PointerPressed += (s, e) => DeleteReport(report);
        actions.Children.Add(deleteBtn);

        stack.Children.Add(actions);
        card.Child = stack;
        return card;
    }

    private void ShowReportDetail(ReportSummary report)
    {
        // For now, show in the content area
        var contentArea = this.FindControl<StackPanel>("ContentArea");
        if (contentArea == null) return;

        contentArea.Children.Clear();

        var detail = _repository.GetReport(report.Id);
        if (detail == null)
        {
            contentArea.Children.Add(new TextBlock { Text = "Report not found.", FontSize = 16, Margin = new Thickness(16) });
            return;
        }

        // Show detail header
        var headerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(16),
            Spacing = 16
        };
        headerPanel.Children.Add(new TextBlock { Text = detail.Name, FontSize = 22, FontWeight = Avalonia.Media.FontWeight.Bold });
        var backBtn = new Button { Content = "Back to List", Tag = "back" };
        backBtn.Click += (s, e) => RefreshReports();
        headerPanel.Children.Add(backBtn);
        contentArea.Children.Add(headerPanel);

        // Show rows info
        contentArea.Children.Add(new TextBlock
        {
            Text = $"Report: {detail.Name} | Source: {detail.SourceFile} | Imported: {detail.ImportedAt:g} | Rows: {detail.Rows.Count}",
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Gray,
            Margin = new Thickness(8)
        });

        // Show rows as cards
        foreach (var row in detail.Rows.Take(10)) // Show first 10 rows
        {
            var rowCard = new Border
            {
                Margin = new Thickness(8),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(6),
                Background = Avalonia.Media.Brushes.LightGray,
                BorderBrush = Avalonia.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            var rowStack = new StackPanel { Spacing = 4 };
            rowStack.Children.Add(new TextBlock { Text = $"Date: {row.Date:yyyy-MM-dd} | Order: {row.OrderId} | Cost: {row.Cost:C}", FontSize = 12, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = Avalonia.Media.Brushes.Black });
            rowStack.Children.Add(new TextBlock { Text = row.Description, FontSize = 11, Foreground = Avalonia.Media.Brushes.DarkSlateGray });

            rowCard.Child = rowStack;
            contentArea.Children.Add(rowCard);
        }
    }

    private void DeleteReport(ReportSummary report)
    {
        // Replace content temporarily with confirmation
        var contentArea = this.FindControl<StackPanel>("ContentArea");
        if (contentArea != null)
        {
            contentArea.Children.Clear();
            // Delete confirmation with working buttons
            var confirmPanel = new StackPanel { Margin = new Thickness(16), Spacing = 16 };
            confirmPanel.Children.Add(new TextBlock { Text = $"Delete report '{report.Name}'?", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.Bold });
            confirmPanel.Children.Add(new TextBlock { Text = "This will delete all rows associated with this report.", Foreground = Avalonia.Media.Brushes.IndianRed });
            var btnStack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            var confirmBtn = new Button { Content = "Confirm Delete", Background = Avalonia.Media.Brushes.IndianRed, Foreground = Avalonia.Media.Brushes.White, Padding = new Thickness(8, 4) };
            confirmBtn.Click += (s, e) => { _repository.DeleteReport(report.Id); RefreshReports(); };
            btnStack.Children.Add(confirmBtn);
            var cancelBtn = new Button { Content = "Cancel", Background = Avalonia.Media.Brushes.LightGray, Foreground = Avalonia.Media.Brushes.Black, Padding = new Thickness(8, 4) };
            cancelBtn.Click += (s, e) => RefreshReports();
            btnStack.Children.Add(cancelBtn);
            confirmPanel.Children.Add(btnStack);
            contentArea.Children.Clear();
            contentArea.Children.Add(confirmPanel);
        }
    }

    private void OnImportClicked(object sender, RoutedEventArgs e)
    {
        using (var dlg = new System.Windows.Forms.OpenFileDialog { Filter = "CSV files (*.csv)|*.csv", Title = "Select CSV" })
        {
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var rows = ReportRepository.ParseCsv(dlg.FileName);
                    var detail = _repository.InsertReport(Path.GetFileNameWithoutExtension(dlg.FileName), Path.GetFileName(dlg.FileName), rows);
                    RefreshReports();
                }
                catch (Exception ex)
                {
                    var contentArea = this.FindControl<StackPanel>("ContentArea");
                    if (contentArea != null)
                        contentArea.Children.Add(new TextBlock { Text = $"Import error: {ex.Message}", FontSize = 14, Margin = new Thickness(16) });
                }
            }
        }
    }

    private void OnExportClicked(object sender, RoutedEventArgs e)
    {
        var reports = _repository.ListReports();
        if (reports.Count == 0)
        {
            var contentArea = this.FindControl<StackPanel>("ContentArea");
            if (contentArea != null)
                contentArea.Children.Add(new TextBlock { Text = "No reports to export.", FontSize = 16, Margin = new Thickness(16) });
            return;
        }

        using (var dlg = new System.Windows.Forms.SaveFileDialog { Filter = "JSON (*.json)|*.json|Excel (*.xlsx)|*.xlsx", Title = "Export Report" })
        {
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var first = _repository.GetReport(reports[0].Id);
                    if (first == null) return;
                    if (dlg.FileName.EndsWith(".xlsx"))
                        ReportRepository.ExportToXlsx(first, dlg.FileName);
                    else
                        ReportRepository.ExportToJson(first, dlg.FileName);
                }
                catch (Exception ex)
                {
                    var contentArea = this.FindControl<StackPanel>("ContentArea");
                    if (contentArea != null)
                        contentArea.Children.Add(new TextBlock { Text = $"Export error: {ex.Message}", FontSize = 14, Margin = new Thickness(16) });
                }
            }
        }
    }

    private void OnReportsClicked(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Reports clicked!");
        RefreshReports();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
