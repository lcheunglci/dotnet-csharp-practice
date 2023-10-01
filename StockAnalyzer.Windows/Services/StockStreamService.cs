using StockAnalyzer.Core.Domain;
using StockAnalyzer.Windows.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockAnalyzer.Windows.Services
{
    public interface IStockStreamService
    {
        IAsyncEnumerable<StockPrice> GetAllStockPrices(CancellationToken cancellationToken = default);
    }
}


public class MockStockStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> GetAllStockPrices([EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.5m };

        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.2m };

        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "GOOG", Change = 0.3m };

        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "GOOG", Change = 0.8m };
    }
}
