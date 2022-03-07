namespace Firebrand.Blog.SharedMemory.Shared;

[Serializable]
public class Currency
{
    public Currency(string code, decimal rate)
    {
        Code = code;
        Rate = rate;
    }

    public string Code { get; set; }
    public decimal Rate { get; set; }
}