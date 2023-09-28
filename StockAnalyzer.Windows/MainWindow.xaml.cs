﻿using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }

    CancellationTokenSource? cancellationTokenSource;


    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        if (cancellationTokenSource is not null)
        {
            // Already have an instance of the cancellation token source?
            // This means the button has already been pressed!

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Search.Content = "Search";
            return;
        }


        try
        {
            cancellationTokenSource = new();

            cancellationTokenSource.Token.Register(() =>
            {
                Notes.Text = "Cancellation requested";
            });

            Search.Content = "Cancel"; // Button text

            BeforeLoadingStockData();

            var identifiers = StockIdentifier.Text.Split(',', ' ');

            var service = new StockService();

            var loadingTask = new List<Task<IEnumerable<StockPrice>>>();

            foreach ( var identifier in identifiers )
            {
                var loadTask = service.GetStockPricesFor(
                    identifier,
                    cancellationTokenSource.Token);
                loadingTask.Add(loadTask);
            }

            var timeout = Task.Delay(12000);
            var allStocksLoadingTask = Task.WhenAll(loadingTask);
            
            var completedTask = Task.WhenAny(timeout, allStocksLoadingTask);

            if (completedTask == timeout)
            {
                cancellationTokenSource.Cancel();
                throw new OperationCanceledException("Timeout!");
            }

            Stocks.ItemsSource = allStocksLoadingTask.Result.SelectMany(x => x);

            //Task<List<string>> loadLinesTask = SearchForStocks(cancellationTokenSource.Token);

            //loadLinesTask.ContinueWith(t =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        Notes.Text = t.Exception?.InnerException?.InnerException?.Message;
            //    });

            //}, TaskContinuationOptions.OnlyOnFaulted);

            //var processStockTask = loadLinesTask.ContinueWith((completedTask) =>
            //{
            //    var lines = completedTask.Result;
            //    var data = new List<StockPrice>();

            //    foreach (var line in lines.Skip(1))
            //    {
            //        var price = StockPrice.FromCSV(line);

            //        data.Add(price);
            //    }


            //    Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);

            //},
            //cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion,
            //TaskScheduler.Current);

            //_ = processStockTask.ContinueWith(_ =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        AfterLoadingStockData();

            //        cancellationTokenSource?.Dispose();
            //        cancellationTokenSource = null;

            //        Search.Content = "Search";
            //    });
            //});
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally
        {
            AfterLoadingStockData();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            Search.Content = "Search";
        }
    }

    private static Task<List<string>> SearchForStocks(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
            var lines = new List<string>();

            while (await stream.ReadLineAsync() is string line)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                lines.Add(line);
            }

            return lines;
        }, cancellationToken);
    }

    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();
            var responseTask = store.GetStockPrices(StockIdentifier.Text);
            Stocks.ItemsSource = await responseTask;
        }
        catch (Exception)
        {
            throw;
        }

        // when responseTask 
        // code below run responseTask has completed
    }

    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}