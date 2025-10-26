using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualBasic;
class task2
{
    private static volatile string? Buff;
    static void Main()
    {
        Console.WriteLine("Введите количество потребителей (int):");
        int n = Convert.ToInt32(Console.ReadLine());;
        Console.WriteLine("Введите список сообщений(разделитель ;):");
        string input_text = Console.ReadLine()!;
        string[] text = input_text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int len = text.Length;     
        var show = new Semaphore[n];

        for (int i = 0; i < n; i++)
        {
            show[i] = new Semaphore(0,1);
        }

        var confirm = new Semaphore(0, n);
        
        var consumers = new List<Thread>();

        Thread developer = new(Give) { Name = "Производитель" };
        developer.Start();
        for (int i = 0; i < n; i++)
        {
            var consumer = new Thread(Take) { Name = $"Потребитель {i + 1}" };
            consumer.Start(i);
            consumers.Add(consumer);
        }


        developer.Join();            
        foreach (var t in consumers) 
            t.Join();
        Console.WriteLine("Готово");

        void Give(){
            for (int i  = 0; i < len; i++)
            {
                Buff = text[i];
                for (int j = 0; j < n; j++)
                {
                    show[j].Release();
                }
                 Console.WriteLine($"{Thread.CurrentThread.Name} передал сообщение: {Buff}");
                for (int j = 0; j < n; j++)
                {
                    confirm.WaitOne();
                }
            }
        }
        void Take(object? state)
        {
            int num = (int)state!;
            for (int y = 0; y < len; y++)
            {
                show[num].WaitOne();
                Console.WriteLine($"{Thread.CurrentThread.Name} получило сообщение: {Buff}");
                confirm.Release();
            }
        }
    }
}
