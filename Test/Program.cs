using EasyCache;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int minSeconds = 10;
            string keyName = "password";
            
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("\r\n▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲\r\n  CACHE TEST  \r\n▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲▼▲\r\n");

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
                var ci = new ExampleItem { KeyName = $"{keyName}{i}", KeyValue = GenerateKey(), KeyTime = TimeSpan.FromSeconds(minSeconds) };
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
                        CacheHelper.AddOrUpdate(keyName, GenerateKey(), TimeSpan.FromSeconds(10));
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
        static string GenerateKey(int pLength = 6, long pSeed = 0)
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
    }

    class ExampleItem
    {
        public string? KeyName { get; set; }
        public string? KeyValue { get; set; }
        public TimeSpan KeyTime { get; set; }
    }
}
