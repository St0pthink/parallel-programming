//Radix sort

#include <iostream>
#include <array>
#include <random>
#include <algorithm>
#include <chrono>
#include <cstdint>
#include <omp.h>

using clk = std::chrono::steady_clock;




template<class T, std::size_t n>
std::array<T, n> radix_sort(std::array<T, n> ar)
    {

        std::array<T, n> out;
        int nd = *std::max_element(ar.begin(), ar.end());

        for (int dv = 1; (nd/dv) > 0; dv *= 10)
        {
            int count[10] = {0};

            for (int j = 0; j < n; j++)
            {
                int div = (ar[j] / dv) % 10;
                count[div] += 1;
            }

            int base = 0;
            for (int k = 0; k < 10; ++k)
            {
                base += count[k];
                count[k] = base;
            }

            for (int j = n-1; j >= 0; --j)
            {
                int div = (ar[j] / dv) % 10;
                out[--count[div]] = ar[j];
            }
            ar = out;
        }

        return ar;
    }


template<class T, std::size_t n>
std::array<T, n> radix_sort_omp(std::array<T, n> ar)
{
    std::array<T, n> out;
    int nd = *std::max_element(ar.begin(), ar.end());

    const int thds = omp_get_num_procs();
    omp_set_num_threads(thds);

    for (int dv = 1; (nd/dv) > 0; dv *= 10)
    {
        int count[10] = {0};
        int (*prl_count)[10] = new int[thds][10];

        int (*base_pos)[10] = new int[thds][10]; 

        for (int t = 0; t < thds; ++t)
            for (int k = 0; k < 10; ++k)
                base_pos[t][k] = 0;


        #pragma omp parallel
        {
            int thd   = omp_get_thread_num();
            int l = (thd * (int)n) / thds;
            int r   = ((thd + 1) * (int)n) / thds;

            int local[10] = {0};
            for (int j = l; j < r; ++j) {
                int div = (ar[j] / dv) % 10;
                local[div] += 1;
            }
            for (int k = 0; k < 10; ++k) prl_count[thd][k] = local[k];
        }

        for (int k = 0; k < 10; ++k) {
            int s = 0;
            for (int t = 0; t < thds; ++t) s += prl_count[t][k];
            count[k] = s;
        }

        int base = 0;
        for (int k = 0; k < 10; ++k) {
            base += count[k];
            count[k] = base;
        }

        int start_count[10];
        start_count[0] = 0;
        for (int k = 1; k < 10; ++k) start_count[k] = count[k - 1];

        for (int d = 0; d < 10; ++d) {
            int off = start_count[d];
            for (int t = 0; t < thds; ++t) {
                base_pos[t][d] = off;
                off += prl_count[t][d];
            }
        }

        #pragma omp parallel
        {
            int thd   = omp_get_thread_num();
            int l = (thd * (int)n) / thds;
            int r  = ((thd + 1) * (int)n) / thds;

            int seen[10] = {0};
            for (int j = l; j < r; ++j) {
                int div = (ar[j] / dv) % 10;
                int pos = base_pos[thd][div] + seen[div]++;
                out[pos] = ar[j];
            }
        }

        ar = out;

        delete[] base_pos;
    
        delete[] prl_count;
    }

    return ar;
}

template <std::size_t n>
void run(std::mt19937& rng) {
    static std::array<int, n> arr;  
    static std::array<int, n> arr1;
    static std::array<int, n> arr2;

    std::uniform_int_distribution<int> dist(0, INT_MAX);
    std::generate(arr.begin(), arr.end(), [&]{ return dist(rng); });

    auto t0 = clk::now();
    arr1 = radix_sort(arr);
    auto t1 = clk::now();
    double ms_og = std::chrono::duration<double, std::milli>(t1 - t0).count();

    t0 = clk::now();
    arr2 = radix_sort_omp(arr);
    t1 = clk::now();
    double ms_omp = std::chrono::duration<double, std::milli>(t1 - t0).count();

    std::cout << n << "\t" << ms_og << " ms\t" << ms_omp << " ms\n";
}


int main() {
    std::mt19937 rng(std::random_device{}());

    run<10000>(rng);
    run<50000>(rng);
    run<100000>(rng);
    run<500000>(rng);
    run<1000000>(rng);  

    return 0;
}

    //for (const auto& x : quick_sort_omp(arr)) std::cout << x << ' ';
    //std::cout << '\n';