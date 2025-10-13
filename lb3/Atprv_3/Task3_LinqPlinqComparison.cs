using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace lb3.Task3
{
    //Структуры данных

    public record Order(int Id, DateTime Date, decimal Amount);

    public record Employee
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public List<Order> Orders { get; init; } = new List<Order>();
    }

    // Результат сравнения LINQ и PLINQ по одному запросу.
    public record ComparisonResult
    {
        public string Title { get; set; } 
        public double LinqTimeMs { get; set; }
        public double PlinqTimeMs { get; set; }
        public int LinqResultCount { get; set; }
        public int PlinqResultCount { get; set; } 
    }

    // класс сравнения

    public class LinqPlinqComparer
    {
        private List<Employee> Employees { get; set; }
        private readonly Random _random = new Random();
        public int TotalOrders { get; private set; } 

        // Генератор тестовых данных: сотрудников и их заказов.
        public void GenerateData(int employeeCount)
        {
            Employees = new List<Employee>();
            TotalOrders = 0;
            var firstNames = new[] { "Александр", "Елена", "Дмитрий", "Ольга", "Сергей", "Татьяна", "Николай", "Анна" };
            var lastNames = new[] { "Иванов", "Петрова", "Сидоров", "Смирнова", "Кузнецов", "Попова", "Васильев", "Соколова" };

            for (int i = 1; i <= employeeCount; i++)
            {
                string firstName = firstNames[_random.Next(firstNames.Length)];
                string lastName = lastNames[_random.Next(lastNames.Length)];

                int numOrders = _random.Next(50, 101);
                var orders = new List<Order>();

                for (int j = 1; j <= numOrders; j++)
                {
                    DateTime date = DateTime.Now.AddDays(-_random.Next(1, 365));
                    decimal amount = (decimal)_random.Next(100, 10000) + (decimal)_random.NextDouble();
                    orders.Add(new Order(j, date, Math.Round(amount, 2)));
                }

                Employees.Add(new Employee
                {
                    Id = i,
                    FullName = $"{firstName} {lastName}",
                    Orders = orders
                });

                TotalOrders += numOrders;
            }
        }

        // Запрос 1
        public ComparisonResult Query1(int idThreshold)
        {
            var result = new ComparisonResult { Title = $"Запрос 1: Найти все заказы сотрудников (ID > {idThreshold})" };

            // LINQ 
            var stopwatch = Stopwatch.StartNew();
            var linqResult = Employees
                .Where(e => e.Id > idThreshold)
                .SelectMany(e => e.Orders)      
                .ToList();
            stopwatch.Stop();
            result.LinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.LinqResultCount = linqResult.Count;

            // PLINQ 
            stopwatch.Restart();
            var plinqResult = Employees.AsParallel()
                .Where(e => e.Id > idThreshold)
                .SelectMany(e => e.Orders)
                .ToList();
            stopwatch.Stop();
            result.PlinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.PlinqResultCount = plinqResult.Count;

            return result;
        }

        // Запрос 2
        public ComparisonResult Query2(decimal amountThreshold)
        {
            var result = new ComparisonResult { Title = $"Запрос 2: Найти все заказы (Amount > {amountThreshold:F2})" };

            // LINQ 
            var stopwatch = Stopwatch.StartNew();
            var linqResult = Employees
                .SelectMany(e => e.Orders)      
                .Where(o => o.Amount > amountThreshold) 
                .ToList();
            stopwatch.Stop();
            result.LinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.LinqResultCount = linqResult.Count;

            // PLINQ 
            stopwatch.Restart();
            var plinqResult = Employees.AsParallel()
                .SelectMany(e => e.Orders)
                .Where(o => o.Amount > amountThreshold)
                .ToList();
            stopwatch.Stop();
            result.PlinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.PlinqResultCount = plinqResult.Count;

            return result;
        }

        // Запрос 3
        public ComparisonResult Query3()
        {
            var result = new ComparisonResult { Title = "Запрос 3: Сортировка сотрудников по средней сумме заказов (Avg Amount)" };

            // LINQ 
            var stopwatch = Stopwatch.StartNew();
            var linqResult = Employees
                .Select(e => new { Employee = e, AvgAmount = e.Orders.Average(o => o.Amount) })
                .OrderByDescending(x => x.AvgAmount) 
                .ToList();
            stopwatch.Stop();
            result.LinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.LinqResultCount = linqResult.Count;

            // PLINQ
            stopwatch.Restart();
            var plinqResult = Employees.AsParallel()
                .Select(e => new { Employee = e, AvgAmount = e.Orders.Average(o => o.Amount) })
                .OrderByDescending(x => x.AvgAmount)
                .ToList();
            stopwatch.Stop();
            result.PlinqTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.PlinqResultCount = plinqResult.Count;

            return result;
        }

        // Выполняет все три запроса и возвращает результаты сравнения
        public List<ComparisonResult> ExecuteAllComparisons(int idThreshold, decimal amountThreshold)
        {
            if (Employees == null || !Employees.Any())
            {
                throw new InvalidOperationException("Данные не сгенерированы. Вызовите GenerateData() первым.");
            }

            var results = new List<ComparisonResult>
            {
                Query1(idThreshold),
                Query2(amountThreshold),
                Query3()
            };

            return results;
        }
    }
}