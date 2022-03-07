// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Firebrand.Blog.SharedMemory.Shared;


//BenchmarkRunner.Run<CreateBinaryVsJson>();
BenchmarkRunner.Run<ConsumeBinaryVsJson>();


[HtmlExporter]
public class CreateBinaryVsJson
{
    HttpClient client = new();
    private SharedMemoryManager manager = new ();
    private Currency[] currencies;
    public CreateBinaryVsJson()
    {
        string currencyData = client.GetStringAsync("https://freecurrencyapi.net/api/v2/latest?apikey=APIKEY&base_currency=TRY").Result;

        FreeCurrencyResponse currencyResponse = JsonSerializer.Deserialize<FreeCurrencyResponse>(currencyData);

        currencies= currencyResponse.data.Select(kp => new Currency(kp.Key, kp.Value)).ToArray();

    }
    
    [Benchmark]
    public void Binary() => manager.CreateMMFFromBinary(currencies);
    
    [Benchmark]
    public void CustomBinary() => manager.CreateMMFFromCustomBinary(currencies);
    
    [Benchmark]
    public void Json() => manager.CreateMMFFromJson(currencies);
}

[HtmlExporter]
public class ConsumeBinaryVsJson
{
    private SharedMemoryManager manager = new ();
  
    [Benchmark]
    public void Binary() => manager.ReadMMFFromBinary<Currency[]>();
    
    [Benchmark]
    public void CustomBinary() => manager.ReadMMFFromCustomBinary();
    
    [Benchmark]
    public void Json() => manager.ReadMMFFromJson<Currency[]>();
}