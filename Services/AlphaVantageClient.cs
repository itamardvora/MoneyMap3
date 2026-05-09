//// Services/AlphaVantageClient.cs
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading.Tasks;
//using System;
//using System.Globalization;


//public static class AlphaVantageClient
//{
//    // פעם אחת HttpClient סטטי – יעיל ובטוח
//    static readonly HttpClient http = new HttpClient();

//    // מחזיר מחיר כמספר (decimal) או null אם לא הצליח
//    public static async Task<decimal?> GetPriceAsync(string symbol, string apiKey)
//    {
//        // URL של Alpha Vantage: GLOBAL_QUOTE מחזיר מחיר נוכחי
//        var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";

//        using var resp = await http.GetAsync(url);
//        if (!resp.IsSuccessStatusCode) return null;

//        var json = await resp.Content.ReadAsStringAsync();

//        using var doc = JsonDocument.Parse(json);
//        if (!doc.RootElement.TryGetProperty("Global Quote", out var quote)) return null;
//        if (!quote.TryGetProperty("05. price", out var priceProp)) return null;

//        var priceStr = priceProp.GetString();
//        if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
//            return price;

//        return null;
//    }
//}
