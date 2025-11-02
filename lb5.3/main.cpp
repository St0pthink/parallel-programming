#include <iostream>
#include <omp.h>

int main() {
    long long k, n;
    std::cin >> k >> n;

    long long total = 0;

    #pragma omp parallel num_threads(k) reduction(+:total)
    {
        long long local = 0;

        #pragma omp for 
        for (long long i = 1; i <= n; ++i) {
            local += i;       
        }

        total += local;
        
        int thd = omp_get_thread_num();
        #pragma omp critical
        {
            std::cout << "[" << thd << "]: Sum = " << local << "\n";
        }
    }


    std::cout << "Sum = " << total << "\n";
    return 0;
}
