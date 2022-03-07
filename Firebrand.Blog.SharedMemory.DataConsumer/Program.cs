// See https://aka.ms/new-console-template for more information

using Firebrand.Blog.SharedMemory.Shared;

Console.WriteLine("Data Consumer is started.");

SharedMemoryManager memoryManager = new SharedMemoryManager();

while (true)
{
    Console.WriteLine("Please enter currency code :");
    string? input = Console.ReadLine();

    if (input.ToLowerInvariant() == "e")
        Environment.Exit(0);

    Currency[]? currencies = memoryManager.ReadMMFFromBinary<Currency[]>();
    Currency[]? currencies2 = memoryManager.ReadMMFFromJson<Currency[]>();
    Currency[]? currencies3 = memoryManager.ReadMMFFromCustomBinary();

    Currency? currency = currencies.SingleOrDefault(c => c.Code == input);

    if (currency is null)
        Console.WriteLine("Currecy code can not be found.");
    else
        Console.WriteLine($"1 TRY = {currency.Rate} {currency.Code}");
}