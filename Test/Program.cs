using EasyCache;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            #region [Using my home-brew cache]
            Console.WriteLine("\r\n  Home-Brew Cache Test  \r\n↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓\r\n");
            var cache = new CacheHelper<string>(TimeSpan.FromSeconds(2));
            cache.ItemEvicted += Cache_ItemEvicted;
            try
            {
                cache.AddOrUpdate("key1", GenerateKeyValue(), TimeSpan.FromSeconds(3));
                cache.AddOrUpdate("key2", GenerateKeyValue(), TimeSpan.FromSeconds(30));
                var keys = cache.GetAllKeys();
                Console.WriteLine("Current cache keys: " + string.Join(", ", keys));
                Thread.Sleep(6000);
                cache.Remove("key2");
                cache.GetAllKeys();
            }
            finally 
            { 
                cache.Dispose();
                Thread.Sleep(3000);
            }
            #endregion

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
                Thread.Sleep(100);
                minSeconds *= 20;
            }

            ConsoleKey _key;
            while ((_key = Console.ReadKey(true).Key) != ConsoleKey.Escape)
            {
                switch (_key)
                {
                    case ConsoleKey.C: Console.Clear();
                        break;
                    case ConsoleKey.D: Console.WriteLine($"[CONTENTS]");
                        foreach (var item in CacheHelper.GetCacheItems())
                        {
                            if (item.Value is ExampleItem ci)
                                Console.WriteLine($"■ Key '{ci.KeyName}' with value '{ci.KeyValue}' expires in {ci.KeyTime.ToReadableString()}. ");
                            else // it's not an ExampleItem
                                Console.WriteLine($"■ Key '{item.Key}' with value '{item.Value}'. ");
                        }
                        break;
                    case ConsoleKey.A: case ConsoleKey.U:
                        CacheHelper.AddOrUpdate(keyName, GenerateKeyValue(), TimeSpan.FromSeconds(10));
                        break;
                    case ConsoleKey.F: case ConsoleKey.G:
                        var fetch = CacheHelper.Get(keyName);
                        if (fetch != null)
                            Console.WriteLine($"■ Item retrieved from cache with key '{keyName}' of type '{fetch.GetType()}'");
                        else
                            Console.WriteLine($"■ Item not found in cache with key '{keyName}'");
                        break;
                    case ConsoleKey.R: CacheHelper.Remove(keyName);
                        break;
                }
            }
            CacheHelper.Shutdown();
            #endregion
        }

        static void Cache_ItemEvicted(EvictionInfo<string> evictionInfo)
        {
            Console.WriteLine($"Cache item evicted: Key='{evictionInfo.Key}'\r\n" + 
                              $"Value='{evictionInfo.Value}'\r\n" +
                              $"Reason='{evictionInfo.Reason}'\r\n" +
                              $"Expiration='{evictionInfo.ExpirationTime}'\r\n");
        }

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
        static string GenerateKeyValue(int pLength = 6, long pSeed = 0)
        {
            const string pwChars = "2346789BCDFGHJKMPQRTVWXY";
            if (pLength < 6)
                pLength = 6; // minimum of 6 characters

            char[] charArray = pwChars.Distinct().ToArray();

            if (pSeed == 0)
                pSeed = DateTime.Now.Ticks;

            var result = new char[pLength];
            var rng = new Random((int)pSeed);

            for (int x = 0; x < pLength; x++)
                result[x] = pwChars[rng.Next() % pwChars.Length];

            return (new string(result));
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"[ERROR] UnhandledException: {(e.ExceptionObject as Exception)?.Message}");
        }
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
}
