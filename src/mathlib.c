#include "mathlib/mathlib.h"

double ml_sum(const double *arr, size_t size) {
    if (arr == NULL || size == 0) {
        return 0.0;
    }
    
    double sum = 0.0;
    for (size_t i = 0; i < size; i++) {
        sum += arr[i];
    }
    
    return sum;
}

double ml_average(const double *arr, size_t size) {
    if (arr == NULL || size == 0) {
        return 0.0;
    }
    
    return ml_sum(arr, size) / (double)size;
}
