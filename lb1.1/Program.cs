using System;
using System.Security.Cryptography.X509Certificates;

namespace project
{
    class Program1
    {
        static void Main()
        {
            int N = 10000;
            int M = 200;

            for (int nb = 1; nb < 4; nb++)                                                                                //для задания 1.1 необходимо в цикле nb начинать не с 2, а с 1                               
            {
                if (nb == 1)
                {
                    N = 1000; Console.WriteLine("Эксперимент со значением N = 10000");                           //для задания 1.1
                    if (nb == 2) { N = 1020; Console.WriteLine("Эксперимент со значением N = 1020, сопоставимо к кэшу L2"); }
                    if (nb == 3) { N = 1250; Console.WriteLine("Эксперимент со значением N = 1250, сопоставимо к кэшу L3"); }
                    for (int number = 1; number < 5; number++)
                    {
                        double time_f = 0, time_sum = 0, time_min = 99999, time_max = 0, time_mean = 0;
                        double[,] a = new double[N, N];
                        DateTime t1, t2;
                        TimeSpan dt;

                        if (number == 1)
                        {
                            for (int y = 0; y < M; y++)
                            {
                                a = new double[N, N];
                                t1 = DateTime.Now;
                                for (int i = 0; i < N; i++)
                                    for (int j = 0; j < N; j++)
                                        a[i, j] = i / (j + 1);
                                t2 = DateTime.Now;
                                dt = t2 - t1;
                                time_f = dt.TotalMilliseconds / 1000.0;
                                time_sum += time_f;
                                if (time_max < time_f) { time_max = time_f; }
                                if (time_min > time_f) { time_min = time_f; }
                            }
                            time_mean = time_sum / M;

                            Console.WriteLine("Для цикла номер " + number);
                            Console.WriteLine("Среднее время: " + Math.Round(time_mean, 4));
                            Console.WriteLine("Минимальное время: " + time_min);
                            Console.WriteLine("Максимальное время: " + time_max + "\n");
                        }


                        if (number == 2)
                        {
                            for (int y = 0; y < M; y++)
                            {
                                a = new double[N, N];
                                t1 = DateTime.Now;
                                for (int j = 0; j < N; j++)
                                    for (int i = 0; i < N; i++)
                                        a[i, j] = i / (j + 1);
                                t2 = DateTime.Now;
                                dt = t2 - t1;
                                time_f = dt.TotalMilliseconds / 1000.0;
                                time_sum += time_f;
                                if (time_max < time_f) { time_max = time_f; }
                                if (time_min > time_f) { time_min = time_f; }
                            }
                            time_mean = time_sum / M;

                            Console.WriteLine("Для цикла номер " + number);
                            Console.WriteLine("Среднее время: " + Math.Round(time_mean, 4));
                            Console.WriteLine("Минимальное время: " + time_min);
                            Console.WriteLine("Максимальное время: " + time_max + "\n");
                        }

                        if (number == 3)
                        {
                            for (int y = 0; y < M; y++)
                            {
                                a = new double[N, N];
                                t1 = DateTime.Now;
                                for (int i = N - 1; i >= 0; i--)
                                    for (int j = N - 1; j >= 0; j--)
                                        a[i, j] = i / (j + 1);
                                t2 = DateTime.Now;
                                dt = t2 - t1;
                                time_f = dt.TotalMilliseconds / 1000.0;
                                time_sum += time_f;
                                if (time_max < time_f) { time_max = time_f; }
                                if (time_min > time_f) { time_min = time_f; }
                            }
                            time_mean = time_sum / M;

                            Console.WriteLine("Для цикла номер " + number);
                            Console.WriteLine("Среднее время: " + Math.Round(time_mean, 4));
                            Console.WriteLine("Минимальное время: " + time_min);
                            Console.WriteLine("Максимальное время: " + time_max + "\n");
                        }

                        if (number == 4)
                        {

                            for (int y = 0; y < M; y++)
                            {
                                a = new double[N, N];
                                t1 = DateTime.Now;
                                for (int j = N - 1; j >= 0; j--)
                                    for (int i = N - 1; i >= 0; i--)
                                        a[i, j] = i / (j + 1);
                                t2 = DateTime.Now;
                                dt = t2 - t1;
                                time_f = dt.TotalMilliseconds / 1000.0;
                                time_sum += time_f;
                                if (time_max < time_f) { time_max = time_f; }
                                if (time_min > time_f) { time_min = time_f; }
                            }
                            time_mean = time_sum / M;

                            Console.WriteLine("Для цикла номер " + number);
                            Console.WriteLine("Среднее время: " + Math.Round(time_mean, 4));
                            Console.WriteLine("Минимальное время: " + time_min);
                            Console.WriteLine("Максимальное время: " + time_max + "\n");
                        }
                    }
                }

            }
        }
    }
}