using System;
using EasyCache;

namespace Test;

internal class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        Console.WriteLine("\r\n Press [1] for home-brew cache, [2] for System.Runtime.Caching, any other key to exit. ");

        var selection = Console.ReadKey(true).Key;
        if (selection == ConsoleKey.D1)
        {
            RunHomeBrewTest();
            _ = Console.ReadKey(true);
        }
        else if (selection == ConsoleKey.D2)
        {
            RunMSLibTest();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    static void RunHomeBrewTest()
    {
        _ =Task.Run(async () =>
        {
            #region [Using my home-brew cache]
            Console.WriteLine("\r\n  Home-Brew Cache Test  \r\n↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓\r\n");
            var cache = new CacheHelper<string>();
            cache.ItemEvicted += OnItemEvicted;
            cache.ItemUpdated += OnItemUpdated;
            try
            {
                cache.AddOrUpdate("key1", GenerateKeyValue(), TimeSpan.FromSeconds(3));
                cache.AddOrUpdate("key2", GenerateKeyValue(), TimeSpan.FromHours(24));
                cache.AddOrUpdate("key3", GenerateKeyValue(), TimeSpan.FromDays(30));
                cache.AddOrUpdate("key4", GenerateKeyValue(), DateTime.Now.AddMinutes(1));
                cache.AddOrUpdate("key5", GenerateKeyValue(), TimeSpan.FromSeconds(7));
                var keys = cache.GetAllKeys();
                Console.WriteLine($"Current cache keys: {string.Join(", ", keys)}");
                await Task.Delay(6000);

                var key5 = cache.Get("key5"); // refresh the expire by fetching
                if (string.IsNullOrEmpty(cache.Get("unknown")))
                    Console.WriteLine($"Key \"unknown\" does not exist.");

                var dt = cache.GetExpiration("key5");
                if (dt != null)
                    Console.WriteLine($"\r\n\"key5\" will expire at {dt.Value.ToLongTimeString()} on {dt.Value.ToLongDateString()}");

                await Task.Delay(6000);

                Console.WriteLine($"The current cache does {(cache.Contains("key1") ? "" : "not ")}contain the key \"key1\"");
                Console.WriteLine($"The current cache does {(cache.Contains("key2") ? "" : "not ")}contain the key \"key2\"");
                Console.WriteLine($"The current cache does {(cache.Contains("key3") ? "" : "not ")}contain the key \"key3\"");
                Console.WriteLine($"The current cache does {(cache.Contains("key5") ? "" : "not ")}contain the key \"key5\"");
                Console.WriteLine();

                cache.Remove("key2");

                foreach (var ci in cache.GetCacheAsEnumerable())
                {
                    Console.WriteLine($"Cache item {ci.Key} value: {ci.Value.Value}");
                }
            }
            finally
            {
                await Task.Delay(4000);
                cache.Dispose();
            }
            #endregion
        });
    }

    static void RunMSLibTest()
    {
        #region [Using Microsoft's System.Runtime.Caching]
        int minSeconds = 10;
        string keyName = "password";
        Console.WriteLine("\r\n  System.Runtime.Caching Test  \r\n↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓\r\n");
        CacheHelper.OnCacheItemUpdated += (k, r) =>
        {
            Console.WriteLine($"[CacheUpdate] ⇒ [{k}][{r}]");
        };
        CacheHelper.OnCacheException += (e) =>
        {
            Console.WriteLine($"[CacheError] ⇒ [{e}]");
        };

        Console.WriteLine("• Press any key to access the cache or [Esc] to exit.\r\n");
        Console.WriteLine("• Press [A] to add an item to the MemoryCache.");
        Console.WriteLine("• Press [D] to dump the contents of the MemoryCache.");
        Console.WriteLine("• Press [F] to fetch a single item from the MemoryCache.");
        Console.WriteLine("• Press [R] to remove a single item from the MemoryCache.");
        Console.WriteLine("• If no key is pressed after the time limit then the item will be evicted from the MemoryCache.\r\n");
        Console.WriteLine("• This will test the SlidingExpiration for the MemoryCache.\r\n");

        for (int i = 1; i < 7; i++)
        {
            var ci = new ExampleItem { KeyName = $"{keyName}{i}", KeyValue = GenerateKeyValue(), KeyTime = TimeSpan.FromSeconds(minSeconds) };
            CacheHelper.AddOrUpdate($"{keyName}{i}", ci, TimeSpan.FromSeconds(minSeconds));
            Thread.Sleep(50);
            minSeconds *= 20;
        }

        ConsoleKey _key;
        while ((_key = Console.ReadKey(true).Key) != ConsoleKey.Escape)
        {
            switch (_key)
            {
                case ConsoleKey.C:
                    Console.Clear();
                    break;
                case ConsoleKey.D:
                    Console.WriteLine($"[CONTENTS]");
                    foreach (var item in CacheHelper.GetCacheItems())
                    {
                        if (item.Value is ExampleItem ci)
                            Console.WriteLine($"■ Key '{ci.KeyName}' with value '{ci.KeyValue}' expires in {ci.KeyTime.ToReadableString()}. ");
                        else // it's not an ExampleItem
                            Console.WriteLine($"■ Key '{item.Key}' with value '{item.Value}'. ");
                    }
                    break;
                case ConsoleKey.A:
                case ConsoleKey.U:
                    CacheHelper.AddOrUpdate(keyName, GenerateKeyValue(), TimeSpan.FromSeconds(10));
                    break;
                case ConsoleKey.F:
                case ConsoleKey.G:
                    var fetch = CacheHelper.Get(keyName);
                    if (fetch != null)
                        Console.WriteLine($"■ Item retrieved from cache with key '{keyName}' of type '{fetch.GetType()}'");
                    else
                        Console.WriteLine($"■ Item not found in cache with key '{keyName}'");
                    break;
                case ConsoleKey.R:
                    CacheHelper.Remove(keyName);
                    break;
            }
        }
        CacheHelper.Shutdown();
        #endregion
    }

    static void OnItemUpdated(ObjectInfo<string> info) => Console.WriteLine(
        $"Cache item updated: Key='{info.Key}'\r\n" +
        $"Value='{info.Value}'\r\n" +
        $"Expiration='{info.ExpirationTime}'\r\n");

    static void OnItemEvicted(EvictionInfo<string> info) => Console.WriteLine(
        $"Cache item evicted: Key='{info.Key}'\r\n" + 
        $"Value='{info.Value}'\r\n" +
        $"Reason='{info.Reason}'\r\n" +
        $"Expiration='{info.ExpirationTime}'\r\n");

    /// <summary><para>
    ///   Basic key/pswd generator for unique IDs. This employs the standard 
    ///   MS key table which accounts for the 36 Latin letters and Arabic 
    ///   numerals used in most Western European languages.
    /// </para><para>
    ///   24 chars are favored: 2346789 BCDFGHJKMPQRTVWXY
    /// </para><para>
    ///   12 chars are avoided: 015 AEIOU LNSZ
    /// </para><para>
    ///   Only 2 chars are occasionally mistaken: 8 & B (depends on the font).
    /// </para><para>
    ///   The base of possible codes is large (about 3.2 * 10^34).
    /// </para></summary>
    static string GenerateKeyValue(int pLength = 8, long pSeed = 0)
    {
        const string pwChars = "2346789BCDFGHJKMPQRTVWXY";
        if (pLength < 1)
            pLength = 1;

        char[] charArray = pwChars.Distinct().ToArray();

        if (pSeed == 0)
            pSeed = DateTime.Now.Ticks;

        Thread.Sleep(1); // force one tick for RNG
        var result = new char[pLength];
        var rng = new Random((int)pSeed);

        for (int x = 0; x < pLength; x++)
            result[x] = pwChars[rng.Next() % pwChars.Length];

        return (new string(result));
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) => Console.WriteLine($"[ERROR] UnhandledException: {(e.ExceptionObject as Exception)?.Message}");
}

/// <summary>
/// An example object for the <see cref="System.Runtime.Caching.MemoryCache"/>.
/// </summary>
class ExampleItem
{
    public string? KeyName { get; set; }
    public string? KeyValue { get; set; }
    public TimeSpan KeyTime { get; set; }
}
