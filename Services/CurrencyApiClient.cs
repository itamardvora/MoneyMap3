using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MoneyMap.Services
{
    // אחראי על שליפת הנתנוים של שערי מטבע 
    public class CurrencyApiClient
    {
        private readonly HttpClient _http;

        public CurrencyApiClient(HttpClient httpClient = null)
        {
            _http = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        
        public async Task<decimal> GetUsdToIlsAsync()
        {
            return await FetchRateFromBoiAsync("USD");
        }

      
        public async Task<decimal> GetEurToIlsAsync()
        {
            return await FetchRateFromBoiAsync("EUR");
        }

      
        private async Task<decimal> FetchRateFromBoiAsync(string baseCurrency)
        {

            string url =
      "https://edge.boi.gov.il/FusionEdgeServer/sdmx/v2/data/dataflow/BOI.STATISTICS/EXR/1.0/" +
      "?format=sdmx-json" +
      "&lastNObservations=1" +
      "&c%5BDATA_TYPE%5D=OF00" +
      "&c%5BBASE_CURRENCY%5D=" + Uri.EscapeDataString(baseCurrency) +
      "&c%5BCOUNTER_CURRENCY%5D=ILS";


            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();

            using (var doc = JsonDocument.Parse(json))
            {
                decimal? rate = ExtractRateForCurrency(doc, baseCurrency);
                if (rate.HasValue)
                    return rate.Value;
            }

            throw new Exception($"לא הצלחתי לקרוא שער עבור {baseCurrency} מתוך התשובה של בנק ישראל");
        }


        private decimal? ExtractRateForCurrency(JsonDocument doc, string wantedBaseCurrency)
        {
            if (!doc.RootElement.TryGetProperty("data", out var dataElem))
                return null;

            if (!dataElem.TryGetProperty("dataSets", out var dataSetsElem))
                return null;

            if (dataSetsElem.GetArrayLength() == 0)
                return null;

            var dataSet0 = dataSetsElem[0];

            if (!dataSet0.TryGetProperty("series", out var seriesElem))
                return null;

            if (!dataElem.TryGetProperty("structure", out var structureElem))
                return null;

            if (!structureElem.TryGetProperty("dimensions", out var dimsElem))
                return null;

            if (!dimsElem.TryGetProperty("series", out var seriesDimsElem))
                return null;

            var dimValues = new List<List<string>>();

            for (int dimI = 0; dimI < seriesDimsElem.GetArrayLength(); dimI++)
            {
                var dimObj = seriesDimsElem[dimI];

                if (!dimObj.TryGetProperty("values", out var valuesArr))
                {
                    dimValues.Add(new List<string>());
                    continue;
                }

                var thisDimVals = new List<string>();

                for (int vi = 0; vi < valuesArr.GetArrayLength(); vi++)
                {
                    var valObj = valuesArr[vi];

                    if (valObj.TryGetProperty("id", out var idProp))
                        thisDimVals.Add(idProp.GetString());
                    else
                        thisDimVals.Add(null);
                }

                dimValues.Add(thisDimVals);
            }

            foreach (var seriesProperty in seriesElem.EnumerateObject())
            {
                string key = seriesProperty.Name;
                var seriesData = seriesProperty.Value;

                string[] parts = key.Split(':');

                int[] idx = new int[parts.Length];
                bool parseOk = true;

                for (int i = 0; i < parts.Length; i++)
                {
                    if (!int.TryParse(parts[i], out idx[i]))
                    {
                        parseOk = false;
                        break;
                    }
                }

                if (!parseOk)
                    continue;

                string baseCurrencyId = SafeGetDimVal(dimValues, 2, idx, 2);
                string counterCurrencyId = SafeGetDimVal(dimValues, 3, idx, 3);
                string dataTypeId = SafeGetDimVal(dimValues, 5, idx, 5);

                if (!string.Equals(baseCurrencyId, wantedBaseCurrency, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(counterCurrencyId, "ILS", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(dataTypeId, "OF00", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!seriesData.TryGetProperty("observations", out var obsElem))
                    continue;

                foreach (var obsKvp in obsElem.EnumerateObject())
                {
                    var arr = obsKvp.Value;

                    if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                    {
                        if (TryReadDecimal(arr[0], out decimal rate))
                            return rate;
                    }
                }
            }

            return null;
        }

        private bool TryReadDecimal(JsonElement element, out decimal value)
        {
            value = 0m;

            if (element.ValueKind == JsonValueKind.Number)
                return element.TryGetDecimal(out value);

            if (element.ValueKind == JsonValueKind.String)
            {
                return decimal.TryParse(
                    element.GetString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value
                );
            }

            return false;
        }

        private string SafeGetDimVal(
            System.Collections.Generic.List<System.Collections.Generic.List<string>> dimValues,
            int dimPosition,
            int[] idx,
            int idxPosition)
        {
            if (dimPosition >= dimValues.Count) return null;
            var dimList = dimValues[dimPosition];
            if (idxPosition >= idx.Length) return null;
            int wantedIndex = idx[idxPosition];
            if (wantedIndex < 0 || wantedIndex >= dimList.Count) return null;
            return dimList[wantedIndex];
        }
    }
}
