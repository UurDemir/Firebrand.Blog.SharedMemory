using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace Firebrand.Blog.SharedMemory.Shared;

public class SharedMemoryManager
{
    private MemoryMappedFile? mmfCustomBinary;
    private MemoryMappedFile? mmfBinary;
    private MemoryMappedFile? mmfJson;

    public void CreateMMFFromBinary(Currency[] data)
    {
        BinaryFormatter binaryFormatter = new();

        MemoryStream memoryStream = new();
        binaryFormatter.Serialize(memoryStream, data);
        byte[] binaryData = memoryStream.ToArray();
        memoryStream.Close();

        Mutex mutex = new(true, "CurrencyCacheBinaryMutex", out bool createdNew);

        if (!createdNew)
            mutex.WaitOne();

        mmfBinary ??= MemoryMappedFile.CreateOrOpen("CurrencyCacheBinary", binaryData.Length);

        using MemoryMappedViewStream viewStream = mmfBinary.CreateViewStream(0, binaryData.Length);
        viewStream.Write(binaryData);
        mutex.ReleaseMutex();
    }

    public void CreateMMFFromCustomBinary(Currency[] data)
    {
        byte[] binaryData =  ConvertToArray(data);
        Mutex mutex = new(true, "CurrencyCacheCustomBinaryMutex", out bool createdNew);

        if (!createdNew)
            mutex.WaitOne();

        mmfCustomBinary ??= MemoryMappedFile.CreateOrOpen("CurrencyCacheCustomBinary", binaryData.Length);

        using MemoryMappedViewStream viewStream = mmfCustomBinary.CreateViewStream(0, binaryData.Length);
        viewStream.Write(binaryData);
        mutex.ReleaseMutex();
    }

    public void CreateMMFFromJson(Currency[] data)
    {
        byte[] binaryData = JsonSerializer.SerializeToUtf8Bytes(data);

        Mutex mutex = new(true, "CurrencyCacheJsonMutex", out bool createdNew);

        if (!createdNew)
            mutex.WaitOne();

        mmfJson ??= MemoryMappedFile.CreateOrOpen("CurrencyCacheJson", binaryData.Length);

        using MemoryMappedViewStream viewStream = mmfJson.CreateViewStream(0, sizeof(int) + binaryData.Length);
        viewStream.Write(BitConverter.GetBytes(binaryData.Length));
        viewStream.Write(binaryData);
        mutex.ReleaseMutex();
    }

    public T? ReadMMFFromJson<T>()
    {
        T? mmfObject;

        Mutex mutex = Mutex.OpenExisting("CurrencyCacheJsonMutex");
        mutex.WaitOne();

        mmfJson ??= MemoryMappedFile.OpenExisting("CurrencyCacheJson");

        using MemoryMappedViewStream sizeViewStream = mmfJson.CreateViewStream(0, sizeof(int));

        Span<byte> lengthInfo = new byte[sizeof(int)];
        sizeViewStream.Read(lengthInfo);

        int dataLength = BitConverter.ToInt32(lengthInfo);

        using MemoryMappedViewStream viewStream = mmfJson.CreateViewStream(sizeof(int), dataLength);

        mmfObject = JsonSerializer.Deserialize<T>(viewStream);
        mutex.ReleaseMutex();

        return mmfObject;
    }

    public T? ReadMMFFromBinary<T>()
    {
        T? mmfObject;
        BinaryFormatter binaryFormatter = new();
        Mutex mutex = Mutex.OpenExisting("CurrencyCacheBinaryMutex");
        mutex.WaitOne();

        mmfBinary ??= MemoryMappedFile.OpenExisting("CurrencyCacheBinary");

        using MemoryMappedViewStream viewStream = mmfBinary.CreateViewStream();
        mmfObject = (T) binaryFormatter.Deserialize(viewStream);
        mutex.ReleaseMutex();

        return mmfObject;
    }

    public Currency[] ReadMMFFromCustomBinary()
    {
        List<Currency> mmfObject = new List<Currency>(10);
        BinaryFormatter binaryFormatter = new();
        Mutex mutex = Mutex.OpenExisting("CurrencyCacheCustomBinaryMutex");
        mutex.WaitOne();

        mmfCustomBinary ??= MemoryMappedFile.OpenExisting("CurrencyCacheCustomBinary");

        using MemoryMappedViewStream sizeViewStream = mmfCustomBinary.CreateViewStream(0, sizeof(int));
        
        Span<byte> lengthInfo = new byte[sizeof(int)];
        sizeViewStream.Read(lengthInfo);
        
        int dataLength = BitConverter.ToInt32(lengthInfo);
        
        using MemoryMappedViewStream viewStream = mmfCustomBinary.CreateViewStream(sizeof(int), dataLength);

        while (viewStream.Position != viewStream.Length)
        {
            
            Span<byte> codeLengthData = new byte[sizeof(int)];
            sizeViewStream.Read(codeLengthData);
            viewStream.Position += sizeof(int);
            int codeLength = BitConverter.ToInt32(codeLengthData);

            Span<byte> codeData = new byte[codeLength];
            sizeViewStream.Read(codeData);
            viewStream.Position += codeLength;
            string code = Encoding.UTF8.GetString(codeData);
            
            byte[] rateData = new byte[sizeof(decimal)];
            sizeViewStream.Read(rateData);
            viewStream.Position += sizeof(decimal);
            decimal rate = ToDecimal(rateData);

            Currency currency = new Currency(code, rate);
            
            mmfObject.Add(currency);
        }
        
        
        mutex.ReleaseMutex();

        return mmfObject.ToArray();
    }
    private byte[] ConvertToArray(Currency[] items)
    {
        List<byte> array = new List<byte>(4096);

        foreach (var item in items)
        {
            array.AddRange(BitConverter.GetBytes(item.Code.Length));
            array.AddRange(Encoding.UTF8.GetBytes(item.Code));
            array.AddRange(GetBytes(item.Rate));
        }
        array.InsertRange(0,BitConverter.GetBytes(array.Count));
        return array.ToArray();
    }

    public static byte[] GetBytes(decimal dec)
    {
        Int32[] bits = decimal.GetBits(dec);
        List<byte> bytes = new List<byte>();
        foreach (Int32 i in bits)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }

        return bytes.ToArray();
    }

    public static decimal ToDecimal(byte[] bytes)
    {
        if (bytes.Count() != 16)
            throw new Exception("A decimal must be created from exactly 16 bytes");

        Int32[] bits = new Int32[4];
        for (int i = 0; i <= 15; i += 4)
        {
            bits[i / 4] = BitConverter.ToInt32(bytes, i);
        }

        return new decimal(bits);
    }
}