using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace lb3.Task2
{
    // Структуры данных
    public class Goods
    {
        public int Id { get; set; }
        public override bool Equals(object obj) => obj is Goods goods && Id == goods.Id;
        public override int GetHashCode() => Id;
    }

    // Информация о товаре на складе
    public class Info
    {
        public decimal Price { get; set; }
        public int Count { get; set; }
    }

    // Статистика обработки заказов
    public class Statistics
    {
        public int TotalOrders { get; set; }
        public int FullyProcessedOrders { get; set; }
        public int InventoryFailureOrders { get; set; }
    }

    // Склад
    public class Storage
    {
        public ConcurrentDictionary<Goods, Info> Inventory { get; }

        public Storage(ConcurrentDictionary<Goods, Info> inventory)
        {
            Inventory = inventory;
        }

        // Генерирация товаров на складе
        public static Storage GenerateInitialData()
        {
            var inventory = new ConcurrentDictionary<Goods, Info>();
            var random = new Random();
            int numGoods = 20;

            for (int i = 1; i <= numGoods; i++)
            {
                decimal price = (decimal)(random.Next(100, 501) + random.NextDouble());
                int count = Math.Max(1, (int)(random.NextDouble() * 25 + random.Next(1, 75)));

                inventory.TryAdd(new Goods { Id = i },
                    new Info { Price = Math.Round(price, 2), Count = count });
            }
            return new Storage(inventory);
        }
    }

    // Заказ и его статус
    public class Order
    {
        public int Id { get; set; }
        public List<Goods> Goods { get; set; } = new List<Goods>();
        public decimal PersonalDiscount { get; set; }
        public string Address { get; set; }

        public bool IsInventoryChecked { get; set; } = false;
        public bool IsAvailable { get; set; } = true;
        public bool IsPriceCalculated { get; set; } = false;
        public bool IsShippingCalculated { get; set; } = false;
        public bool IsProcessed => IsInventoryChecked && IsAvailable && IsPriceCalculated && IsShippingCalculated;

        public decimal TotalPrice { get; set; }
        public TimeSpan ShippingTime { get; set; }
        public decimal ShippingCost { get; set; }
    }

    // Сервис проверки наличия товаров на складе с искуственной задержкой
    public class InventoryService
    {
        private readonly Storage _storage;
        public InventoryService(Storage storage) { _storage = storage; }

        public async Task<(bool available, string logMessage)> CheckInventoryAsync(Order order)
        {
            // Задержка
            await Task.Delay(100);

            bool isAvailable = true;
            var missingGoods = new List<int>();

            foreach (var item in order.Goods)
            {
                // Проверка в хранилище
                if (!_storage.Inventory.TryGetValue(item, out Info info) || info.Count < 1)
                {
                    isAvailable = false;
                    missingGoods.Add(item.Id);
                }
            }
            string logMessage = isAvailable ?
                "OK. Все товары в наличии." :
                $"FAIL. Товары отсутствуют: {string.Join(", ", missingGoods)}";

            return (isAvailable, logMessage);
        }
    }

    // Расчёт итоговой цены заказа с искуственной задержкой
    public class PricingService
    {
        private readonly Storage _storage;
        public PricingService(Storage storage) { _storage = storage; }

        public async Task<(decimal totalPrice, string logMessage)> CalculatePriceAsync(Order order)
        {
            await Task.Delay(50);

            decimal basePrice = 0;
            foreach (var item in order.Goods)
            {
                if (_storage.Inventory.TryGetValue(item, out Info info))
                {
                    basePrice += info.Price;
                }
            }

            decimal discountFactor = 1 - order.PersonalDiscount;
            decimal totalPrice = basePrice * discountFactor;

            string logMessage = $"OK. Итоговая: {totalPrice:F2} (Скидка: {order.PersonalDiscount:P0})";

            return (totalPrice, logMessage);
        }
    }

    // Расчет цены и срока доставки с искуственной задержкой
    public class ShippingService
    {
        public async Task<(TimeSpan shippingTime, decimal shippingCost, string logMessage)> CalculateShippingAsync(Order order)
        {
            await Task.Delay(20);

            int addressLength = order.Address.Length;
            TimeSpan shippingTime = TimeSpan.FromHours(addressLength * 4);

            decimal shippingCost = 100M + (decimal)shippingTime.TotalHours * 5M;
            shippingCost = Math.Round(shippingCost, 2);

            double totalHours = shippingTime.TotalHours;

            int days = (int)Math.Floor(totalHours / 24);
            int remainingHours = (int)Math.Floor(totalHours % 24);

            string timeString;

            if (days > 0)
            {
                timeString = $"{days} дн.";
                if (remainingHours > 0)
                {
                    timeString += $" {remainingHours} ч.";
                }
            }
            else
            {
                timeString = $"{Math.Ceiling(totalHours)} ч.";
            }

            string logMessage = $"OK. Срок: {timeString}, Стоимость: {shippingCost:F2}";

            return (shippingTime, shippingCost, logMessage);
        }
    }

    // Обработка
    public class OrderProcessor
    {
        private readonly Storage _storage = Storage.GenerateInitialData();
        private static readonly object _logLock = new object();
        private readonly InventoryService _inventoryService;
        private readonly PricingService _pricingService;
        private readonly ShippingService _shippingService;

        private const string LogFileName = "task2log.txt";

        public OrderProcessor()
        {
            _inventoryService = new InventoryService(_storage);
            _pricingService = new PricingService(_storage);
            _shippingService = new ShippingService();

            // Удаление старого лога
            if (File.Exists(LogFileName))
            {
                File.Delete(LogFileName);
            }
        }

        // Запись в лог
        private void LogToFile(string message)
        {
            lock (_logLock)
            {
                File.AppendAllText(LogFileName, message + Environment.NewLine);
            }
        }

        // Проверка наличия
        private async Task<bool> CheckInventory(Order order)
        {
            var (available, logMessage) = await _inventoryService.CheckInventoryAsync(order);
            order.IsInventoryChecked = true;
            order.IsAvailable = available;
            LogToFile($"[O{order.Id:D3}] Наличие: {logMessage}");
            return available;
        }

        // Расчет цены
        private async Task CalculatePricing(Order order)
        {
            var (totalPrice, logMessage) = await _pricingService.CalculatePriceAsync(order);
            order.TotalPrice = totalPrice;
            order.IsPriceCalculated = true;
            LogToFile($"[O{order.Id:D3}] Цена: {logMessage}");
        }

        // Расчет доставки
        private async Task CalculateShipping(Order order)
        {
            var (shippingTime, shippingCost, logMessage) = await _shippingService.CalculateShippingAsync(order);

            order.ShippingTime = shippingTime;
            order.ShippingCost = shippingCost;
            order.TotalPrice += shippingCost;

            order.IsShippingCalculated = true;
            LogToFile($"[O{order.Id:D3}] Доставка: {logMessage}. Итого с доставкой: {order.TotalPrice:F2}");
        }

        // Обработка заказа
        public async Task ProcessOrderAsync(Order order)
        {
            LogToFile($"[O{order.Id:D3}] ---> НАЧАЛО");
            if (await CheckInventory(order))
            {
                await Task.WhenAll(
                    CalculatePricing(order),
                    CalculateShipping(order)
                );

                LogToFile($"[O{order.Id:D3}] --- КОНЕЦ ---");
            }
            else
            {
                LogToFile($"[O{order.Id:D3}] --- ОТМЕНА (Нет товара) ---");
            }
        }

        // Параллельная обработка заказов 
        public async Task<Statistics> ProcessAllOrdersAsync(int count = 30)
        {
            var orders = GenerateSampleOrders(count);

            var tasks = orders.Select(ProcessOrderAsync).ToList();
            await Task.WhenAll(tasks);

            int fullyProcessed = 0;
            int inventoryFailed = 0;

            foreach (var o in orders)
            {
                if (o.IsProcessed)
                {
                    fullyProcessed++;
                }
                if (o.IsInventoryChecked && !o.IsAvailable)
                {
                    inventoryFailed++;
                }
            }

            var statistics = new Statistics
            {
                TotalOrders = orders.Count,
                FullyProcessedOrders = fullyProcessed,
                InventoryFailureOrders = inventoryFailed
            };

            LogToFile($"--- СТАТИСТИКА --- Всего: {statistics.TotalOrders}, Завершено: {statistics.FullyProcessedOrders}");
            return statistics;
        }

        // Генерация заказов
        private List<Order> GenerateSampleOrders(int count)
        {
            var orders = new List<Order>();
            var random = new Random();

            const double FailureChance = 0.30;
            const int MaxExistingGoodsId = 20;
            const int MissingGoodsId = 999;

            for (int i = 1; i <= count; i++)
            {
                var goodsList = new List<Goods>();
                int numGoods = random.Next(1, 4);

                for (int j = 0; j < numGoods; j++)
                {
                    goodsList.Add(new Goods { Id = random.Next(1, MaxExistingGoodsId + 1) });
                }

                // Несуществующий товар
                if (random.NextDouble() < FailureChance)
                {
                    goodsList.Add(new Goods { Id = MissingGoodsId });
                }

                // Генерация адреса
                int numSegments = random.Next(2, 6);
                string address = "";

                for (int k = 0; k < numSegments; k++)
                {
                    char randomChar = (char)random.Next('A', 'Z' + 1);
                    int randomDigits = random.Next(1, 1000);
                    address += $"{randomChar}{randomDigits}";
                }

                orders.Add(new Order
                {
                    Id = i,
                    Goods = goodsList,
                    PersonalDiscount = (decimal)random.Next(0, 51) / 100,
                    Address = address
                });
            }
            return orders;
        }
    }
}