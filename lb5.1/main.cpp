#include <iostream>
#include <omp.h>

int main() {
    long long n;
    std::cin >> n;

    long long total = 0;

    #pragma omp parallel num_threads(2) reduction(+:total)
    {
        int thd = omp_get_thread_num();
        long long local = 0;

        long long m = n / 2;

        if (thd == 0)
        {
            for (long long i = 1; i <= m; ++i) {
                local += i;
            }
        } 
        else 
        {
            for (long long i = m + 1; i <= n; ++i) {
                local += i;
            }
        }
        total += local;
        #pragma omp critical
        {
            std::cout << "[" << thd << "]: Sum = " << local << "\n";
        }
    }


    std::cout << "Sum = " << total << "\n";
    return 0;
}
