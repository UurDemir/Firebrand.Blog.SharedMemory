namespace Firebrand.Blog.SharedMemory.Shared;

public class FreeCurrencyResponse
{
    public Query query { get; set; }
    public Dictionary<string,decimal> data { get; set; }
}