using System;
using System.Runtime.Caching;
using System.Text;

namespace EasyCache;

/// <summary>
/// The MemoryCache class is a concrete implementation of the abstract ObjectCache class.
/// https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache?view=net-8.0
/// </summary>
/// <remarks>
/// Our main method will employ <see cref="CacheItemPolicy.SlidingExpiration"/> since 
/// we want the stale-timer to be reset each time the key is accessed by the user.
/// </remarks>
public static class CacheHelper
{
    static readonly System.Runtime.Caching.MemoryCache memoryCache = System.Runtime.Caching.MemoryCache.Default;

    /// <summary>
    /// An event that fires when a cache item has been updated. Format is {Key, CacheEntryRemovedReason}
    /// </summary>
    public static event Action<string, CacheEntryRemovedReason> OnCacheItemUpdated;
    
    /// <summary>
    /// An event that fires when any error occurs.
    /// </summary>
    public static event Action<Exception> OnCacheException;

    #region [Public Methods]
    public static bool AddOrUpdate(string key, object value, double expirationSeconds) => AddOrUpdate(key, value, TimeSpan.FromSeconds(expirationSeconds));
    public static bool AddOrUpdate(string key, object value, float expirationMinutes) => AddOrUpdate(key, value, TimeSpan.FromMinutes((double)expirationMinutes));
    public static bool AddOrUpdate(string key, object value, int expirationDays) => AddOrUpdate(key, value, TimeSpan.FromDays((double)expirationDays));

    /// <summary>
    /// With <see cref="CacheItemPolicy.AbsoluteExpiration"/> the cache will be expired after a particular 
    /// time irrespective of the fact whether it has been used or not in that time span. Whereas, in 
    /// <see cref="CacheItemPolicy.SlidingExpiration"/>, the cache will be expired after a particular 
    /// time only if it has not been used during that time span.
    /// </summary>
    public static bool AddOrUpdate(string key, object value, TimeSpan expiration)
    {
        // The MemoryCache class does not allow null as a value in the cache.
        // Any attempt to add or change a cache entry with a value of null will fail.
        if (value == null)
        {
            OnCacheException?.Invoke(new ArgumentNullException($"'{nameof(value)}' cannot be null."));
            return false;
        }
        if (string.IsNullOrEmpty(key))
        {
            OnCacheException?.Invoke(new ArgumentException($"'{nameof(key)}' cannot be empty."));
            return false;
        }
        if (expiration > TimeSpan.FromDays(365))
        {
            OnCacheException?.Invoke(new ArgumentOutOfRangeException($"'{nameof(expiration)}' may not exceed 1 year."));
            expiration = TimeSpan.FromDays(365);
        }

        // NOTE: A cache item policy can not be re-used, each entry holds its own policy.
        System.Runtime.Caching.CacheItemPolicy policy = new System.Runtime.Caching.CacheItemPolicy
        {
            // Only one expiration can be used, not both.
            SlidingExpiration = expiration,
            // The update callback includes removal, so there is no need to setup CacheEntryRemovedCallback.
            UpdateCallback = new System.Runtime.Caching.CacheEntryUpdateCallback(UpdateCallback),
        };

        try
        {
            memoryCache.Set(key, value, policy);
            Console.WriteLine($"■ Item added/updated in cache with key \"{key}\" for {expiration.ToReadableString()}.");
            return true;
        }
        catch (Exception ex)
        {
            OnCacheException?.Invoke(ex);
        }

        return false;
    }

    public static object? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            OnCacheException?.Invoke(new ArgumentException($"'{nameof(key)}' cannot be empty."));
            return null;
        }
        return memoryCache.Get(key);
    }

    public static bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            OnCacheException?.Invoke(new ArgumentException($"'{nameof(key)}' cannot be empty."));
            return false;
        }

        if (memoryCache.Contains(key))
        {
            memoryCache.Remove(key);
            Console.WriteLine($"■ Item removed from cache with key \"{key}\"");
            return true;
        }
        else
        {
            Console.WriteLine($"■ Item not found in cache with key \"{key}\", nothing to remove.");
            return false;
        }
    }

    public static void Shutdown(bool flush = true)
    {
        try
        {
            if (flush)
            {
                foreach (var item in memoryCache)
                {
                    if (memoryCache.Contains(item.Key))
                    {
                        System.Diagnostics.Debug.WriteLine($"[INFO] Evicting '{item.Key}'");
                        memoryCache.Remove(item.Key);
                    }
                }
            }
            memoryCache.Dispose();
        }
        catch (Exception ex)
        {
            OnCacheException?.Invoke(ex);
        }
    }

    /// <summary>
    /// You will also see entries for the sentinel, which tracks each item in the cache.
    /// The <see cref="System.Runtime.Caching.MemoryCache.SentinelEntry"/> is a private
    /// class which employs a Collection<ChangeMonitor> to monitor each entry.
    /// </summary>
    public static void DumpCacheItems()
    {
        string format = "  Key ⇒ {0,-15} Value ⇒ {1,-30}";
        Console.WriteLine("■ Current cache items:");
        foreach (var item in memoryCache)
        {
            if (item.Value != null && item.Value.GetType().ToString().Contains("System.Runtime.Caching.MemoryCache+SentinelEntry"))
                System.Diagnostics.Debug.WriteLine($"[INFO] Ignoring MemoryCache.SentinelEntry");
            else
                Console.WriteLine(String.Format(format, item.Key, item.Value));
        }
    }

    /// <summary>
    /// You will also see entries for the sentinel, which tracks each item in the cache.
    /// The <see cref="System.Runtime.Caching.MemoryCache.SentinelEntry"/> is a private
    /// class which employs a Collection<ChangeMonitor> to monitor each entry.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, object>> GetCacheItems()
    {
        foreach (var item in memoryCache)
        {
            if (item.Value != null && item.Value.GetType().ToString().Contains("System.Runtime.Caching.MemoryCache+SentinelEntry"))
                System.Diagnostics.Debug.WriteLine($"[INFO] Ignoring MemoryCache.SentinelEntry");
            else
               yield return item;
        }
    }
    #endregion

    #region [Private Methods]
    static void UpdateCallback(System.Runtime.Caching.CacheEntryUpdateArguments arguments)
    {
        string format = "  Key: {0,-15} Reason: {1,-30}";
        Console.WriteLine($"■ [CacheUpdated]  {string.Format(format, arguments.Key, arguments.RemovedReason)}");
        OnCacheItemUpdated?.Invoke(arguments.Key, arguments.RemovedReason);

        #region [Experimental]
        if (arguments.UpdatedCacheItemPolicy != null)
            Console.WriteLine($"■ Expiration: {arguments.UpdatedCacheItemPolicy.AbsoluteExpiration.TimeOfDay.ToReadableString()}");
        if (arguments.UpdatedCacheItem != null)
            Console.WriteLine($"■ CacheObject: {arguments.UpdatedCacheItem}");
        #endregion
    }

    /// <summary>
    /// Human-friendly <see cref="TimeSpan"/> formatter.
    /// </summary>
    public static string ToReadableString(this TimeSpan span)
    {
        var parts = new StringBuilder();
        if (span.Days > 0)
            parts.Append($"{span.Days} day{(span.Days == 1 ? string.Empty : "s")} ");
        if (span.Hours > 0)
            parts.Append($"{span.Hours} hour{(span.Hours == 1 ? string.Empty : "s")} ");
        if (span.Minutes > 0)
            parts.Append($"{span.Minutes} minute{(span.Minutes == 1 ? string.Empty : "s")} ");
        if (span.Seconds > 0)
            parts.Append($"{span.Seconds} second{(span.Seconds == 1 ? string.Empty : "s")} ");
        if (span.Milliseconds > 0)
            parts.Append($"{span.Milliseconds} millisecond{(span.Milliseconds == 1 ? string.Empty : "s")} ");

        if (parts.Length == 0) // result was less than 1 millisecond
            return $"{span.TotalMilliseconds:N4} milliseconds";
        else
            return parts.ToString().Trim();
    }
    #endregion
}
