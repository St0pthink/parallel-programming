using System;
using System.IO;                 
using System.Collections.Concurrent;    
using System.Collections.Generic;       
using System.Linq;                      
using System.Threading;    
using Order = System.Object[,];


class task2
{
    private static readonly object ConsoleLock = new();

    static void Log(string message, ConsoleColor color)
    {
        lock (ConsoleLock)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{Thread.CurrentThread.Name}] {message}");
            Console.ForegroundColor = prev;
            Console.Out.Flush();
        }
    }

    static void Log(string message) => Log(message, ConsoleColor.Gray);

    private static void print_catalog(ConcurrentDictionary<string, Item> catalog)
{
    var snap = catalog.ToArray()
                          .OrderBy(kv => kv.Key)
                          .Select(kv => $"{kv.Key}: {kv.Value.Amount}");

    Log("Каталог:", ConsoleColor.Blue);

    bool any = false;
    foreach (var line in snap)
    {
        any = true;
        Log(line, ConsoleColor.Blue);
    }

    if (!any)
        Log("Каталог пуст", ConsoleColor.Magenta);
}
    public sealed class Item
    {
        private readonly object _sync = new object();
        private int _amount;

        public Item(int amount)
        {  
            Amount = amount; 
        }

        public int Amount
        {
            get { lock (_sync) { return _amount; } } 
            private set { _amount = value; }
        }

        public bool Full_Give(int need)
        {
            lock (_sync)
            {
                if (_amount >= need)
                {
                    _amount -= need;
                    return true;
                }
                return false;
            }
        }

        public int Part_Give(int need)
        {
            if (need <= 0) return 0;
            lock (_sync)
            {
                int taken = Math.Min(_amount, need);
                _amount -= taken;
                return taken;
            }
        }
    }

    private static volatile bool stop = false;
    private static long orderSeq = 0;
    private static readonly ConcurrentDictionary<string, byte> lowStockWarned = new();
    private static readonly ConcurrentDictionary<string, byte> outOfStockWarned = new();
    private static int stopSignaled = 0;
    static void Main()
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.OutputEncoding = System.Text.Encoding.UTF8;


        const int client_n = 15;
        const int checker_n = 3;
        const int que_n = 50;

        var catalog = Catalog_Class.сreate_сatalog();
        var productKeys = catalog.Keys.ToArray();

        var orderQueue = new ConcurrentQueue<(long Id, DateTime EnqueuedAt, Order Bag)>();
        var items = new SemaphoreSlim(0);
        var slots = new SemaphoreSlim(que_n); /// Ограничение на макс количество, чтобы не было перегрузки заказами
        print_catalog(catalog);


        var clients = Enumerable.Range(1, client_n)
            .Select(i => new Thread(make_order) { Name = $"Клиент {i}" })
            .ToList();

        var checkers = Enumerable.Range(1, checker_n)
            .Select(i => new Thread(check) { Name = $"Обработчик {i}" })
            .ToList();

        foreach (var t in clients) t.Start();
        foreach (var t in checkers) t.Start();

        foreach (var t in clients) t.Join();
        foreach (var t in checkers) t.Join();
        Log("Готово");


        void make_order()
        {
            Random time_rng = new Random();
            while (true)
            {
                if (stop) break;

                slots.Wait();

                if (stop)
                {
                    slots.Release();
                    break;
                }

                var client_bag = bag_generator(productKeys);
                var id = Interlocked.Increment(ref orderSeq);

                Log($"Заказал #{id}: {order_string(client_bag)}");

                if (stop)
                {
                    slots.Release();
                    break;
                }
                var enqAt = DateTime.UtcNow;
                orderQueue.Enqueue((id, enqAt, client_bag));
                items.Release();
                Thread.Sleep(time_rng.Next(1000, 3000));
            }
        }

        void check()
        {
            while (true)
            {
                if (stop) break;
                items.Wait();
                if (stop)
                {
                    items.Release();
                    break;
                }

                if (!orderQueue.TryDequeue(out var qi))
                {
                    Log($"{Thread.CurrentThread.Name} Очередь пуста после сигналa items.", ConsoleColor.Yellow);
                    items.Release();
                    continue;
                }


                var id = qi.Id;
                var enqAt = qi.EnqueuedAt;
                var order = qi.Bag;


                var age = DateTime.UtcNow - enqAt;
                if (age > TimeSpan.FromSeconds(10))
                {
                    Log($"Отменён по таймауту заказ #{id} (простоял {age.TotalSeconds:F1}s в очереди).", ConsoleColor.DarkYellow);
                    slots.Release();
                    continue;
                }   

                Log($"Начал обработку заказа #{id}", ConsoleColor.DarkCyan);

                int rows = order.GetLength(0);
                for (int i = 0; i < rows; i++)
                {
                    string Name = (string)order[i, 0];
                    int need = (int)order[i, 1];
                    var item = catalog[Name];

                    if (item.Full_Give(need))
                    {
                        Log($"[#{id}] {Name} - {need} подтверждено. Остаток: {item.Amount}", ConsoleColor.Green);
                    }
                    else
                    {
                        // Частичное выполнение
                        int taken = item.Part_Give(need);
                        if (taken > 0)
                            Log($"[#{id}] {Name} - частично {taken}/{need}.", ConsoleColor.Yellow);
                        else
                            Log($"[#{id}] {Name} - нет в наличии.", ConsoleColor.Red);
                    }
                    if (item.Amount <= 5 && lowStockWarned.TryAdd(Name, 1))
                        Log($"Низкий остаток по {Name}: {item.Amount}", ConsoleColor.Cyan);

                    if (item.Amount == 0 && outOfStockWarned.TryAdd(Name, 1))
                        Log($"Товар {Name} закончился.", ConsoleColor.Magenta);
                }
                Log($"Завершил обработку заказа #{id}", ConsoleColor.DarkCyan);
                slots.Release();

                if (IsCatalogEmpty(catalog))
                {
                    Log("Склад пуст — останавливаем покупателей и обработчиков.", ConsoleColor.Magenta);
                    stop = true;

                    if (Interlocked.Exchange(ref stopSignaled, 1) == 0)
                    {
                        Log("Склад пуст — останавливаем покупателей и обработчиков.", ConsoleColor.Magenta);
                        items.Release(checker_n); // будим всех обработчиков
                        slots.Release(client_n);  // будим всех клиентов
                    }
                    break;
                }
            }
        }
    }
    
    public static Order bag_generator(IReadOnlyCollection<string> productKeys)
    {
        Random bag_rng = new Random();
        int lines = bag_rng.Next(1, 4);

        // Выбираем уникальные без повторов
        var Names = productKeys.ToArray();
        Shuffle(Names, bag_rng);                   // перемешивание
        var chosen = Names.Take(lines).ToArray();

        // Собираем двумерный массив
        var order = new object[lines, 2];
        for (int i = 0; i < lines; i++)
        {
            int quantity = bag_rng.Next(1, 6);
            order[i, 0] = chosen[i];
            order[i, 1] = quantity;
        }

        return order;
    }
    private static void Shuffle<T>(T[] array, Random rnd)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private static string order_string(Order order)
    {
        int rows = order.GetLength(0);
        var parts = new string[rows];
        for (int i = 0; i < rows; i++)
            parts[i] = $"{order[i, 0]} x{order[i, 1]}";
        return string.Join(", ", parts);
    }
    
    private static bool IsCatalogEmpty(ConcurrentDictionary<string, Item> catalog)
        => catalog.All(kv => kv.Value.Amount == 0);

    public static class Catalog_Class
    {
        private static readonly string[] ProductKeys =
        {
        "Смартфон 5G",
        "Ноутбук",
        "Планшет",
        "Монитор",
        "Клавиатура механическая",
        "Мышь беспроводная",
        "Наушники накладные",
        "Умные часы",
        "Экшн-камера",
        "Игровая консоль",
        "Маршрутизатор Wi-Fi 6",
        "Внешний SSD 1 ТБ"
    };
        public static ConcurrentDictionary<string, Item> сreate_сatalog()
        {
            Random rng = new Random();
            var catalog = new ConcurrentDictionary<string, Item>();

            foreach (var key in ProductKeys)
            {
                int quantity = rng.Next(5, 21);
                catalog[key] = new Item(quantity);
            }

            return catalog;
        }

    }

}
