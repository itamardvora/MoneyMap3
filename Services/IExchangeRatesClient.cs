using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyMap.Services
{
    /// <summary>
    /// לקוח שמחזיר שערים יחסית ל-ILS: מיפוי CODE -> RateToILS.
    /// </summary>
    public interface IExchangeRatesClient
    {
        Task<Dictionary<string, decimal>> GetLatestAsync(IEnumerable<string> baseCurrencies);
    }
}
