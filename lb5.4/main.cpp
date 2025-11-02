#include <iostream>
#include <omp.h>

int main() {
    long long k, n;
    std::cin >> k >> n;

    long long total = 0;

    #pragma omp parallel num_threads(k) reduction(+:total)
    {
        int thd = omp_get_thread_num();
        long long local = 0;
        

        /*/ 
        schedule(static)
        schedule(static, 1)
        schedule(static, 2)
        schedule(dynamic)
        schedule(dynamic, 2)
        schedule(guided)
        schedule(guided, 2)
        /*/
        #pragma omp for schedule(guided,2)
        for (long long i = 1; i <= n; ++i) {

            #pragma omp critical
            {
                std::cout << "[" << thd << "]: calculation of the iteration number " << i << "\n";
            }

            local += i;       
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
