using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Button = Avalonia.Controls.Button;
using AvaloniaTextBox = Avalonia.Controls.TextBox;
using AvaloniaMenu = Avalonia.Controls.Menu;
using AvaloniaMenuItem = Avalonia.Controls.MenuItem;
using AvaloniaApplication = Avalonia.Application;
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
    private TrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        _repository = new ReportRepository();

        // Wire menu events by header text (MenuItem nested items aren't initialized as partial-class fields)
        if (this.FindControl<AvaloniaMenu>("MainMenu") is AvaloniaMenu mainMenu)
        {
            foreach (var fileItem in mainMenu.Items.OfType<AvaloniaMenuItem>())
            {
                if ((string?)fileItem.Header == "File")
                {
                    foreach (var sub in fileItem.Items.OfType<AvaloniaMenuItem>())
                    {
                        switch ((string?)sub.Header)
                        {
                            case "Import CSV": sub.Click += OnImportClicked; break;
                            case "Export Report": sub.Click += (s, e) => { var first = _repository.ListReports().FirstOrDefault(); if (first != null) ExportReport(first); }; break;
                            case "Exit": sub.Click += OnExitClicked; break;
                        }
                    }
                }
                else if ((string?)fileItem.Header == "Theme")
                {
                    foreach (var sub in fileItem.Items.OfType<AvaloniaMenuItem>())
                    {
                        switch ((string?)sub.Header)
                        {
                            case "Light": sub.Click += (s, e) => Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light; break;
                            case "Dark": sub.Click += (s, e) => Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark; break;
                        }
                    }
                }
            }
        }

        // --- Tray Icon ---
        var iconFile = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        System.Diagnostics.Debug.WriteLine($"[Tray] icon file exists: {File.Exists(iconFile)}  path: {iconFile}");
        _trayIcon = new TrayIcon
        {
            IsVisible = true,
            Icon = File.Exists(iconFile) ? new WindowIcon(iconFile) : null,
            ToolTipText = "Reports Insight"
        };
        System.Diagnostics.Debug.WriteLine($"[Tray] icon loaded: {(_trayIcon.Icon != null)}");

        // Tray context menu
        var nativeMenu = new Avalonia.Controls.NativeMenu();
        var openItem = new Avalonia.Controls.NativeMenuItem("Open / Restore");
        openItem.Click += (_, _) => { this.Show(); this.WindowState = WindowState.Normal; };
        nativeMenu.Items.Add(openItem);
        var exitItem = new Avalonia.Controls.NativeMenuItem("Exit");
        exitItem.Click += (_, _) => this.Close();
        nativeMenu.Items.Add(exitItem);
        _trayIcon.Menu = nativeMenu;

        // Initialize with reports list
        RefreshReports();

        // Show window immediately so tray icon appears
        this.Show();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
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

        // Show a header with import button
        var header = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(16),
            Children = {
                new TextBlock { Text = "Reports", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new TextBlock { Text = $"({reports.Count} reports)", FontSize = 12, Foreground = Avalonia.Media.Brushes.Gray, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 4, 0, 0) }
            }
        };
        var importBtn = new Button
        {
            Content = "Import CSV",
            Foreground = Avalonia.Media.Brushes.Black,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightBlue),
            Padding = new Thickness(6, 2),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        importBtn.Click += (s, e) => OnImportClicked(null, null);
        header.Children.Add(importBtn);
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

        // Export button for this report
        var exportBtn = new TextBlock
        {
            Text = "Export",
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkGreen),
            Foreground = Avalonia.Media.Brushes.White,
            Padding = new Thickness(8, 4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        exportBtn.PointerPressed += (s, e) => ExportReport(report);
        actions.Children.Add(exportBtn);

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

        // Show header panel with Back button
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

        // Show rows as table (Grid layout)
        var tableGrid = new Grid
        {
            Margin = new Thickness(16),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(120) },
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(300, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(80) }
            },
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto } }
        };

        // Header
        tableGrid.Children.Add(new TextBlock { Text = "Date", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetRow(tableGrid.Children[tableGrid.Children.Count - 1], 0);
        Grid.SetColumn(tableGrid.Children[tableGrid.Children.Count - 1], 0);

        tableGrid.Children.Add(new TextBlock { Text = "Order ID", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetRow(tableGrid.Children[tableGrid.Children.Count - 1], 0);
        Grid.SetColumn(tableGrid.Children[tableGrid.Children.Count - 1], 1);

        tableGrid.Children.Add(new TextBlock { Text = "Description", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetRow(tableGrid.Children[tableGrid.Children.Count - 1], 0);
        Grid.SetColumn(tableGrid.Children[tableGrid.Children.Count - 1], 2);

        tableGrid.Children.Add(new TextBlock { Text = "Cost", FontWeight = Avalonia.Media.FontWeight.Bold, Margin = new Thickness(4) });
        Grid.SetRow(tableGrid.Children[tableGrid.Children.Count - 1], 0);
        Grid.SetColumn(tableGrid.Children[tableGrid.Children.Count - 1], 3);

        var rowControls = new List<(AvaloniaTextBox dateBox, AvaloniaTextBox orderBox, AvaloniaTextBox descBox, AvaloniaTextBox costBox, ReportRow row)>();

        int currentRow = 1;
        foreach (var row in detail.Rows)
        {
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var dateBox = new AvaloniaTextBox { Text = row.Date.ToString("yyyy-MM-dd"), Margin = new Thickness(4) };
            tableGrid.Children.Add(dateBox);
            Grid.SetRow(dateBox, currentRow);
            Grid.SetColumn(dateBox, 0);

            var orderBox = new AvaloniaTextBox { Text = row.OrderId.ToString(), Margin = new Thickness(4) };
            tableGrid.Children.Add(orderBox);
            Grid.SetRow(orderBox, currentRow);
            Grid.SetColumn(orderBox, 1);

            var descBox = new AvaloniaTextBox { Text = row.Description, Margin = new Thickness(4) };
            tableGrid.Children.Add(descBox);
            Grid.SetRow(descBox, currentRow);
            Grid.SetColumn(descBox, 2);

            var costBox = new AvaloniaTextBox { Text = row.Cost.ToString(), Margin = new Thickness(4) };
            tableGrid.Children.Add(costBox);
            Grid.SetRow(costBox, currentRow);
            Grid.SetColumn(costBox, 3);

            rowControls.Add((dateBox, orderBox, descBox, costBox, row));
            currentRow++;
        }

        contentArea.Children.Add(tableGrid);

        // Save Changes button
        var saveBtn = new Button
        {
            Content = "Save Changes",
            Background = Avalonia.Media.Brushes.LightBlue,
            Foreground = Avalonia.Media.Brushes.Black,
            Padding = new Thickness(8, 4),
            Margin = new Thickness(16, 16, 0, 0)
        };
        saveBtn.Click += (s, e) => {
            try
            {
                foreach (var item in rowControls)
                {
                    var dateBox = item.dateBox;
                    var orderBox = item.orderBox;
                    var descBox = item.descBox;
                    var costBox = item.costBox;
                    var row = item.row;
                    if (dateBox != null && DateTime.TryParse(dateBox.Text, out var d))
                        row.Date = d;
                    if (orderBox != null && int.TryParse(orderBox.Text, out var o))
                        row.OrderId = o;
                    if (descBox != null)
                        row.Description = descBox.Text;
                    if (costBox != null && decimal.TryParse(costBox.Text, out var c))
                        row.Cost = c;
                }

                _repository.UpdateReportRows(detail.Id, detail.Rows);
                var msg = new TextBlock { Text = "Changes saved!", Foreground = Avalonia.Media.Brushes.Green, Margin = new Thickness(16, 8, 0, 0) };
                contentArea.Children.Add(msg);
            }
            catch (Exception ex)
            {
                var msg = new TextBlock { Text = $"Save error: {ex.Message}", Foreground = Avalonia.Media.Brushes.Red, Margin = new Thickness(16, 8, 0, 0) };
                contentArea.Children.Add(msg);
            }
        };
        contentArea.Children.Add(saveBtn);
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

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnLightThemeClicked(object sender, RoutedEventArgs e)
    {
        Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
    }

    private void OnDarkThemeClicked(object sender, RoutedEventArgs e)
    {
        Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
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

    private void ExportReport(ReportSummary report)
    {
        using (var dlg = new System.Windows.Forms.SaveFileDialog { Filter = "JSON (*.json)|*.json|Excel (*.xlsx)|*.xlsx", Title = $"Export Report: {report.Name}" })
        {
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var detail = _repository.GetReport(report.Id);
                    if (detail == null) return;
                    if (dlg.FileName.EndsWith(".xlsx"))
                        ReportRepository.ExportToXlsx(detail, dlg.FileName);
                    else
                        ReportRepository.ExportToJson(detail, dlg.FileName);
                    
                    var contentArea = this.FindControl<StackPanel>("ContentArea");
                    if (contentArea != null)
                        contentArea.Children.Add(new TextBlock { Text = $"Export successful: {Path.GetFileName(dlg.FileName)}", FontSize = 12, Foreground = Avalonia.Media.Brushes.Green, Margin = new Thickness(8) });
                }
                catch (Exception ex)
                {
                    var contentArea = this.FindControl<StackPanel>("ContentArea");
                    if (contentArea != null)
                        contentArea.Children.Add(new TextBlock { Text = $"Export error: {ex.Message}", FontSize = 12, Margin = new Thickness(8) });
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
