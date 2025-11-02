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

        long long len = n / k;
        long long rem = n % k; 
        long long number;

        if (thd < rem) 
        {
            number = thd * (len + 1);
        } 
        else 
        {
            number = rem * (len + 1) + (thd - rem) * len;
        }

        long long L = number + 1;

        long long R;
        if (thd < rem)
        {
            R = number + len + 1;
        }
        else
        {
            R = number + len;
        }     

        for (long long x = L; x <= R; x++) 
        {
            local += x;
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
