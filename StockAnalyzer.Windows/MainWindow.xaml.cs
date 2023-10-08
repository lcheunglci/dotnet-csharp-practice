using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private Random random = new Random();

    public MainWindow()
    {
        InitializeComponent();
    }

    CancellationTokenSource? cancellationTokenSource;
    private object progress;

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // without creating a task for this it will deadlock
            //Task.Run(SearchForStocks).Wait();

            BeforeLoadingStockData();

            var stocks = new Dictionary<string, IEnumerable<StockPrice>>()
            {
                //
            };

            var bag = new ConcurrentBag<StockCalculation>();

            Parallel.Invoke(
                () => {
                    var msft = Calculate(stocks["MSFT"]);
                    bag.Add(msft);
                },
                () => {
                    var aapl = Calculate(stocks["AAPL"]);
                    bag.Add(aapl);
                },
                () => { var googl = Calculate(stocks["GOOGL"]);
                    bag.Add(googl);
                },
                () =>
                {
                    var cat = Calculate(stocks["CAT"]);
                    bag.Add(cat);
                }
                );

            Stocks.ItemsSource = bag;

            AfterLoadingStockData();

            // var data = await SearchForStocks();

            //Stocks.ItemsSource = data.Where(price => price.Identifier == StockIdentifier.Text);

            // For showing progress
            //var progress = new Progress<IEnumerable<StockPrice>>();
            //progress.ProgressChanged += (_, stocks) =>
            //{
            //    StockProgress.Value += 1;
            //    Notes.Text += $"Loaded {stocks.Count()} for {stocks.First().Identifier}{Environment.NewLine}";
            //};

            //await SearchForStocks(progress);
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;

        }
        finally
        {
            AfterLoadingStockData();
        }

        //try
        //{
        //    BeforeLoadingStockData();

        //    var identifiers = StockIdentifier.Text.Split(' ',',');

        //    var data = new ObservableCollection<StockPrice>();

        //    Stocks.ItemsSource = data;

        //    var service = new StockDiskStreamService();

        //    var enumerator = service.GetAllStockPrices();

        //    await foreach ( var item in enumerator
        //        // You can implement cancellation on your own!
        //        .WithCancellation(CancellationToken.None))
        //    {
        //        if (identifiers.Contains(item.Identifier))
        //        {
        //            data.Add(item);
        //        }
        //    }

        //    //var data = await GetStocksFor(StockIdentifier.Text);
        //    //Notes.Text = "Stocks loaded!";
        //    //Stocks.ItemsSource = data;
        //}
        //catch (Exception ex)
        //{
        //    Notes.Text = ex.Message;
        //}
        //finally
        //{
        //    AfterLoadingStockData();
        //}

        //if (cancellationTokenSource is not null)
        //{
        //    // Already have an instance of the cancellation token source?
        //    // This means the button has already been pressed!

        //    cancellationTokenSource.Cancel();
        //    cancellationTokenSource.Dispose();
        //    cancellationTokenSource = null;

        //    Search.Content = "Search";
        //    return;
        //}


        //try
        //{
        //    cancellationTokenSource = new();

        //    cancellationTokenSource.Token.Register(() =>
        //    {
        //        Notes.Text = "Cancellation requested";
        //    });

        //    Search.Content = "Cancel"; // Button text

        //    BeforeLoadingStockData();

        //    var identifiers = StockIdentifier.Text.Split(',', ' ');

        //    var service = new StockService();

        //    var loadingTask = new List<Task<IEnumerable<StockPrice>>>();
        //    var stocks = new ConcurrentBag<StockPrice>();

        //    foreach ( var identifier in identifiers )
        //    {
        //        var loadTask = service.GetStockPricesFor(
        //            identifier,
        //            cancellationTokenSource.Token);

        //        loadTask = loadTask.ContinueWith(t =>
        //        {
        //            var aFewStocks = t.Result.Take(5);

        //            foreach (var stock in aFewStocks)
        //            {
        //                stocks.Add(stock);
        //            }

        //            Dispatcher.Invoke(() => {
        //                Stocks.ItemsSource = stocks.ToArray();
        //            });

        //            return aFewStocks;
        //        });
        //        loadingTask.Add(loadTask);
        //    }

        //    var timeout = Task.Delay(12000);
        //    var allStocksLoadingTask = Task.WhenAll(loadingTask);

        //    var completedTask = Task.WhenAny(timeout, allStocksLoadingTask);

        //    if (completedTask == timeout)
        //    {
        //        cancellationTokenSource.Cancel();
        //        throw new OperationCanceledException("Timeout!");
        //    }

        //    // Stocks.ItemsSource = allStocksLoadingTask.Result.SelectMany(x => x);

        //    //Task<List<string>> loadLinesTask = SearchForStocks(cancellationTokenSource.Token);

        //    //loadLinesTask.ContinueWith(t =>
        //    //{
        //    //    Dispatcher.Invoke(() =>
        //    //    {
        //    //        Notes.Text = t.Exception?.InnerException?.InnerException?.Message;
        //    //    });

        //    //}, TaskContinuationOptions.OnlyOnFaulted);

        //    //var processStockTask = loadLinesTask.ContinueWith((completedTask) =>
        //    //{
        //    //    var lines = completedTask.Result;
        //    //    var data = new List<StockPrice>();

        //    //    foreach (var line in lines.Skip(1))
        //    //    {
        //    //        var price = StockPrice.FromCSV(line);

        //    //        data.Add(price);
        //    //    }


        //    //    Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);

        //    //},
        //    //cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion,
        //    //TaskScheduler.Current);

        //    //_ = processStockTask.ContinueWith(_ =>
        //    //{
        //    //    Dispatcher.Invoke(() =>
        //    //    {
        //    //        AfterLoadingStockData();

        //    //        cancellationTokenSource?.Dispose();
        //    //        cancellationTokenSource = null;

        //    //        Search.Content = "Search";
        //    //    });
        //    //});
        //}
        //catch (Exception ex)
        //{
        //    Notes.Text = ex.Message;
        //}
        //finally
        //{
        //    AfterLoadingStockData();
        //    cancellationTokenSource?.Dispose();
        //    cancellationTokenSource = null;

        //    Search.Content = "Search";
        //}
    }


    private IEnumerable<StockPrice> Generate(string stockIdentifier)
    {
        return Enumerable.Range(1, random.Next(10, 250))
            .Select(x => new StockPrice
            {
                Identifier = stockIdentifier,
                Open = random.Next(10, 1024)
            });
    }

    private StockCalculation Calculate(IEnumerable<StockPrice> prices)
    {
        #region Start stopwatch
        var calculation = new StockCalculation();
        var watch = new Stopwatch();
        watch.Start();
        #endregion

        var end = DateTime.UtcNow.AddSeconds(4);

        // Spin a loop for a few seconds to simulate load
        while (DateTime.UtcNow < end)
        { }

        #region Return a result
        calculation.Identifier = prices.First().Identifier;
        calculation.Result = prices.Average(s => s.Open);

        watch.Stop();

        calculation.TotalSeconds = watch.Elapsed.Seconds;

        return calculation;
        #endregion
    }



    private async Task SearchForStocks(IProgress<IEnumerable<StockPrice>> progress)
    {
        var service = new StockService();
        var loadingTasks = new List<Task<IEnumerable<StockPrice>>> ();

        foreach (var identifier in StockIdentifier.Text.Split(' ', ','))
        {
            var loadTask = service.GetStockPricesFor(identifier, CancellationToken.None);

            loadTask = loadTask.ContinueWith(completedTask =>
            {
                progress?.Report(completedTask.Result);
                return completedTask.Result;
            });
        }

        var data = await Task.WhenAll(loadingTasks);

        Stocks.ItemsSource = data.SelectMany(stock => stock);
    }

    private async Task<IEnumerable<StockPrice>> GetStocksFor(string identifier)
    {
        var service = new StockService();
        var data = await service.GetStockPricesFor(identifier, CancellationToken.None)
            .ConfigureAwait(false);

        // Different Thread!


        return data.Take(5);
    }

    private Task<IEnumerable<StockPrice>> SearchForStocks()
    {
        var tcs = new TaskCompletionSource<IEnumerable<StockPrice>>();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            var lines = File.ReadAllLines("StockPrices_Small.csv");
            var prices = new List<StockPrice>();

            foreach (var line in lines.Skip(1))
            {
                prices.Add(StockPrice.FromCSV(line));
            }

            // Communicate the result of 'prices'
            tcs.SetResult(prices);
        });

        //  return a Task<IEnumerable<StockPrice>>
        return tcs.Task;
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
