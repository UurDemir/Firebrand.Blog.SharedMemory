// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Firebrand.Blog.SharedMemory.Shared;

Console.WriteLine("Data Producer Started.");

SharedMemoryManager memoryManager = new SharedMemoryManager();

HttpClient client = new();
Timer timer = new Timer(RefreshData,null,new TimeSpan(0,0,0),new TimeSpan(0,0,10));

while (true)
{
    string input = Console.ReadLine();

    if (input.ToLowerInvariant() == "exit")
    {
        Environment.Exit(0);
    }
}




async void RefreshData(object? state)
{

    Console.WriteLine("Refreshing is started.");
    string currencyData = await client.GetStringAsync("https://freecurrencyapi.net/api/v2/latest?apikey=APIKEY&base_currency=TRY");

    FreeCurrencyResponse currencyResponse = JsonSerializer.Deserialize<FreeCurrencyResponse>(currencyData);

    Currency[] currencies = currencyResponse.data.Select(kp => new Currency(kp.Key, kp.Value)).ToArray();
    
    memoryManager.CreateMMFFromCustomBinary(currencies);
    memoryManager.CreateMMFFromBinary(currencies);
    memoryManager.CreateMMFFromJson(currencies);
    
    Console.WriteLine("Refreshing is done.");
}

