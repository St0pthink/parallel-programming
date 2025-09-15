using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualBasic;
class task2
{
    static int[] ns = {1000, 5000, 10000, 10000};
    static int[] thrs = {1, 2, 4, 8, 12, 16, 20};
    static  DateTime time0;
    static TimeSpan time;
    static void Exponentiation_(object o)
    {
        object[] info = (object[])o;
        double[] a_array = (double[])info[2];
        double[] b_array = (double[])info[3];
        int left = (int)info[0];
        int right = (int)info[1];
        for (int i = left; i < right; i++)
        {   
            double sum = 0;
            for (int j = 0; j < i + 1; j++)
                sum += Math.Pow(a_array[j], 1.789);
            if (i >= left)
            {
                b_array[i] = sum;
            }
        }
    }
    static double[] Fill(int n)
    {
        double[] array = new double[n];
        for (int i = 0; i < n; i++)
        {
            array[i] = i + 1;
        }
        return array;
    }
    static double[] parallels_type1(double[] array, int n, int thr)
    {
        double[] b_array = new double[n];
        int step = n / thr;
        Thread[] threads = new Thread[thr];
        for (int i = 0; i < thr; i++)
        {
            int left = i * step;
            int right;
            if (i == thr - 1)
            {
                right = n;
            }
            else
            {
                right = (i + 1) * step;
            }
            threads[i] = new Thread(Exponentiation_);
            threads[i].Start(new object[4] { left, right, array, b_array});
            
        }

        for (int i = 0; i < thr; i++)
        {
            threads[i].Join();
        }
        return b_array;
    }
    static double[] parallels_type2(double[] array, int n, int thr)
    {
        double[] b_array = new double[n];
        Thread[] threads = new Thread[thr];
        int right = 0;
        int left = 0;
        for (int i = 0; i < thr; i++)
        {
            double step = Math.Pow(2, i + 1);
            right = left+(int)Math.Floor(n / step);
            if (thr == 1 || i == thr - 1)
            {
                right = n;
            }
            threads[i] = new Thread(Exponentiation_);
            threads[i].Start(new object[4] { left, right, array, b_array });
            left = right;
            
        }

        for (int i = 0; i < thr; i++)
        {
            threads[i].Join();
        }
        return b_array;
    }
    static void Main()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                time0 = DateTime.Now;
                ///parallels_type1(Fill(ns[i]), ns[i], thrs[j]);
                parallels_type2(Fill(ns[i]), ns[i], thrs[j]);
                time = DateTime.Now - time0;
                Console.WriteLine(time);
            }
            Console.WriteLine("/////////////////////////");
        }

    }
}

