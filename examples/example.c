#include <stdio.h>
#include "mathlib/mathlib.h"

int main() {
    printf("Math Library Example\n");
    printf("===================\n\n");
    
    // Example data: test scores
    double test_scores[] = {85.5, 92.0, 78.5, 95.0, 88.5, 91.0, 83.0};
    size_t num_scores = sizeof(test_scores) / sizeof(test_scores[0]);
    
    printf("Test scores: ");
    for (size_t i = 0; i < num_scores; i++) {
        printf("%.1f ", test_scores[i]);
    }
    printf("\n");
    
    double sum = ml_sum(test_scores, num_scores);
    double average = ml_average(test_scores, num_scores);
    
    printf("Sum: %.2f\n", sum);
    printf("Average: %.2f\n", average);
    
    return 0;
}
