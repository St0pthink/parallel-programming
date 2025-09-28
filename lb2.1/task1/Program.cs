using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualBasic;
/// <summary>
/// !!!Реализовать способ по матрицам и по столбцам!!!
/// </summary>
class task2
{
    static int[] ns = {1000, 2000, 5000, 10000};
    static int[] thrs = {1, 2, 4, 8, 12, 16, 20};
    static  DateTime time0;
    static TimeSpan time;
    static int[,] Matrix_multiplication_type1(int[,] mat_a,int[,] mat_b, int n,int t)
    {
        int[,] mat_c = new int[n, n];

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = t 
        };

        Parallel.For(0, n, options, i =>
        {
            int summ = 0;
            for (int j = 0; j < n; j++)
            {
                summ = 0;
                for (int x = 0; x < n; x++)
                {
                    summ += mat_a[i, x] * mat_b[x, j];
                }
                mat_c[i, j] = summ;
            }
        });
        return mat_c;
    }
    static int[,] Matrix_multiplication_type2(int[,] mat_a,int[,] mat_b, int n,int t)
    {
        
        int[,] mat_c = new int[n, n];

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = t 
        };

        Parallel.For(0, n, j =>
        {
            int summ = 0;
            for (int i = 0; i < n; i++)
            {
                summ = 0;
                for (int x = 0; x < n; x++)
                {
                    summ += mat_a[i, x] * mat_b[x, j];
                }
                mat_c[i, j] = summ;
            }
        });
        return mat_c;
    }
    static int[,] Fill_rng(int n)
    {
        Random rng = new Random();
        int[,] matrix = new int[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = rng.Next(int.MinValue, int.MaxValue);
            }
        }
        return matrix;
    }
    static void PrintMatrix(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1); 
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write(matrix[i, j] + "\t"); 
            }
            Console.WriteLine(); 
        }
    }
    static void Main()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                time0 = DateTime.Now;
                int[,] matrix = Matrix_multiplication_type1(Fill_rng(ns[i]), Fill_rng(ns[i]), ns[i], thrs[j]);
                //int[,] matrix = Matrix_multiplication_type2(Fill_rng(ns[i]), Fill_rng(ns[i]), ns[i], thrs[j]);
                time = DateTime.Now - time0;
                Console.WriteLine(time);
                //PrintMatrix(matrix);
            }
            Console.WriteLine("/////////////////////////");
        }

    }
}

