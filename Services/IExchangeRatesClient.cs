using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyMap.Services
{
    public interface IExchangeRatesClient
    {
        Task<Dictionary<string, decimal>> GetLatestAsync(IEnumerable<string> baseCurrencies);
    }
}
