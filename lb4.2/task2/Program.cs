using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualBasic;
class task2
{
    private static volatile bool asked = true;
    static void Main()
    {     
        Console.WriteLine("Введите количество птенцов (int):");
        int n = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("Введите количество еды, которое приносит мать (int):");
        int F = Convert.ToInt32(Console.ReadLine());

        var food = new SemaphoreSlim(F, int.MaxValue);

        var bring = new AutoResetEvent(false);

        int need_food = 0;

        int done = 0;

        var sons = new List<Thread>();
        
        var consoleLock = new object();       

        Thread mother = new(Hunt) { Name = "Мать птица" };
        mother.Start();
        for (int i = 0; i < n; i++)
        {
            var son = new Thread(Eat) { Name = $"Птенец {i + 1}" };
            son.Start();
            sons.Add(son);
        }
        void Log(string s) { lock (consoleLock) Console.WriteLine(s); }

        mother.Join();            
        foreach (var t in sons) 
            t.Join();
        Log("Готово");

        void Hunt(){
            while (true)
            {
                bring.WaitOne();
                if (Volatile.Read(ref done) == n)
                break;
                food.Release(F);
                Interlocked.Exchange(ref need_food, 0);
                Log($"{Thread.CurrentThread.Name} принесла еды; Количество еды: {food.CurrentCount}");           
            }
        }
        void Eat()
        {
            Random rng = new Random();
            int k = rng.Next(5, 15);
            for (int i = 0; i < k; i++)
            {
                food.Wait();
                Log($"{Thread.CurrentThread.Name} поел; Количество еды: {food.CurrentCount}");
                Thread.Sleep(1000);
                Log($"{Thread.CurrentThread.Name} спит;");
                if (food.CurrentCount == 0 && Interlocked.Exchange(ref need_food, 1) == 0)
                {
                    Log($"{Thread.CurrentThread.Name} просит еды; Количество еды: {food.CurrentCount}");
                    asked = false;
                    bring.Set();
                }
            }
            Log($"{Thread.CurrentThread.Name} вырос!");
            if ( Interlocked.Increment(ref done) == n)
            {
            bring.Set();
            }

        }
    }
}