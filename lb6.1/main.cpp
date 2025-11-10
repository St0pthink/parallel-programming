//quick sort

#include <iostream>
#include <array>
#include <random>
#include <algorithm>
#include <chrono>
#include <cstdint>
#include <omp.h>

using clk = std::chrono::steady_clock;




template<class T, std::size_t n>
std::array<T, n> quick_sort(std::array<T, n> ar)
    {
        auto qs = [&](auto&& self, int l, int r) 
        {
            if (l >= r) return;

            auto pivot = ar[r];
            int i = l - 1;
            for(int j = l; j < r;j++)
            {
                if (pivot > ar[j])
                {
                    i += 1;
                    std::swap(ar[i],ar[j]);
                }
            }
            i += 1;
            std::swap(ar[i],ar[r]);
            
            self(self, l, i - 1);
            self(self, i + 1, r); 
        };
        qs(qs, 0, n - 1);
        return ar;
    
    }


template<class T, std::size_t n>
std::array<T, n> quick_sort_omp(std::array<T, n> ar)
{

    const int max_depth = 3;  

    auto qs = [&](auto&& self, int l, int r, int depth) 
    {
        if (l >= r) return;

        auto pivot = ar[r];
        int i = l - 1;
        for (int j = l; j < r; ++j) 
            {                   
                if (pivot > ar[j]) 
                { 
                    ++i; 
                    std::swap(ar[i], ar[j]); 
                }
            }
        ++i; 
        std::swap(ar[i], ar[r]);                 


        if (depth < max_depth) {                     
            #pragma omp task default(shared) firstprivate(l,i,depth)
            self(self, l, i-1, depth + 1);

            #pragma omp task default(shared) firstprivate(i,r,depth)
            self(self, i+1, r, depth + 1);

            #pragma omp taskwait
        } else {
            self(self, l, i-1, depth + 1);
            self(self, i+1, r, depth + 1);
        }
    };

    #pragma omp parallel
    {
        #pragma omp single nowait  
        qs(qs, 0, n-1, 0);
    }                                               

    return ar;

}

template <std::size_t n>
void run(std::mt19937& rng) {
    static std::array<int, n> arr;  
    static std::array<int, n> arr1;
    static std::array<int, n> arr2;

    std::uniform_int_distribution<int> dist(INT_MIN, INT_MAX);
    std::generate(arr.begin(), arr.end(), [&]{ return dist(rng); });

    auto t0 = clk::now();
    arr1 = quick_sort(arr);
    auto t1 = clk::now();
    double ms_og = std::chrono::duration<double, std::milli>(t1 - t0).count();

    t0 = clk::now();
    arr2 = quick_sort_omp(arr);
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