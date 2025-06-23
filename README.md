![Icon](AppIcon.png) 
# EasyCache



## v2.0.0.0 - October 2024
**Dependencies**

| Assembly | Version |
| ---- | ---- |
| NET Core | 6.0 (LTS) |
| NET Framework | 4.8.1 |
| System.Runtime.Caching | 8.0.0.1 |

- A [memory caching](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache?view=net-6.0) library which offers timed evictions for stored objects.
- I've also created my own version of Microsoft's memory cache to show how you could roll your own.

- This solution includes a console application for testing the DLL.

## Usage

```csharp

 // Instantiate
 var cache = new CacheHelper<string>();

 // Add an item
 cache.AddOrUpdate("key1", "value1", TimeSpan.FromSeconds(3));
 // Add an item to update
 cache.AddOrUpdate("key3", "value3", DateTime.Now.AddMinutes(1));
 // Update an item
 cache.AddOrUpdate("key3", "updated value3", DateTime.Now.AddMinutes(2));

 // Check expiry
 var dt = cache.GetExpiration("key3");
 Console.WriteLine($"key3 will expire at {dt.Value.ToLongTimeString()}");

 // Check if exists
 Console.WriteLine($"The current cache does {(cache.Contains("key1") ? "" : "not")} contain key1");

 // Refresh the expiration by fetching
 var temp = cache.Get("key1");

 // Delete an item
 cache.Remove("key1");

 // Check if exists after removal
 Console.WriteLine($"The current cache does {(cache.Contains("key1") ? "" : "not")} contain key1");

 // Fetching all keys
 var keys = cache.GetAllKeys();
 foreach (var key in keys) { /* do something */ }

 // Fetching all objects
 var objs = cache.GetCacheAsEnumerable();
 foreach (var obj in objs) { /* do something */ }

 // Clean-up
 cache.Dispose();

```

## 📷 Screenshot

![Sample](./Screenshot.png)

## 🧾 License/Warranty
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish and distribute copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
* The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the author or copyright holder be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
* Copyright © 2025. All rights reserved.

## 📋 Proofing
* This application was compiled and tested using *VisualStudio* 2022 on *Windows 10/11* versions **22H2**, **21H2**, **21H1**, and **23H2**.