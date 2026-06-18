using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyMap.Services
{
    public interface IExchangeRatesClient // ממשק לששליפת שערי מטבע חיצוני מאי פי אי
    {
        Task<Dictionary<string, decimal>> GetLatestAsync(IEnumerable<string> baseCurrencies);
    }
}
