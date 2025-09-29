using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    // Параллельный и последовательный методы (последовательный метод - параллельный метод только с 1 потоком)
    static double LeftThread(Func<double, double> f, double a, double b, long n, int ThN)
    {
        
        Thread[] threads = new Thread[ThN];
        double[] localSums = new double[ThN];
        double h = (b - a) / n;

        long chunkSize = n / ThN;
        long remainder = n % ThN;
        long currentStartIndex = 0;

        for (int i = 0; i < ThN; i++)
        {
            long startIndex = currentStartIndex;
            long endIndex = startIndex + chunkSize + (i < remainder ? 1 : 0);
            int threadIndex = i;

            threads[i] = new Thread(() =>
            {
                double sum = 0.0;
                for (long j = startIndex; j < endIndex; j++)
                {
                    double x = a + j * h;
                    sum += f(x);
                }
                localSums[threadIndex] = sum;
            });
            
            threads[i].Start();
            currentStartIndex = endIndex;
        }

        for (int i = 0; i < ThN; i++)
        {
            threads[i].Join();
        }

        double totalSum = 0.0;
        for (int i = 0; i < localSums.Length; i++)
        {
            totalSum += localSums[i];
        }
        return totalSum * h;
    }

    // Функция: f(x) = 2 * log2(3 + x^2)
    static double Func(double x)
    {
        return 2 * Math.Log(2, 3 + x * x); 
    }

   static double Adaptive(Func<double, double> f, double a, double b, double eps, long initialN, int ThN)
    {
        long n = initialN; 
        double currentResult;

        double previousResult = double.MaxValue; 
        
        currentResult = LeftThread(f, a, b, n, ThN);

        while (Math.Abs(currentResult - previousResult) >= eps)
        {
            previousResult = currentResult;
            
            n *= 2; 

            currentResult = LeftThread(f, a, b, n, ThN);
        }
        return currentResult;
    }

    static void Main()
    {
        long N;
        double a, b;
        
        Console.Write("Начальное число разбиений: ");
        N = long.Parse(Console.ReadLine()); 

        Console.Write("Левая граница: ");
        a = double.Parse(Console.ReadLine()); 
        
        Console.Write("Правая граница: ");
        b = double.Parse(Console.ReadLine()); 
        
        double[] epss = { 0.001, 0.0001, 0.00001, 0.000001, 0.0000001, 0.00000001, 0.000000001 };
        int[] ThN = { 1, 2, 4, 8, 12, 16, 20 };

        Stopwatch stopwatch = new Stopwatch();

        foreach (double eps in epss)
        {
            Console.WriteLine($"Точность: {eps:F9}"); 

            foreach (int ThCount in ThN)
            {
                stopwatch.Reset();
                stopwatch.Start();

                double result = Adaptive(Func, a, b, eps, N, ThCount);

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"  Потоки: {ThCount}\n  Время (мс): {elapsedMs}\n  Интеграл ≈ {result:F10}\n");
            }
            Console.WriteLine("/////////////////////////");
        }
    }
}
