#include <stdio.h>
#include <assert.h>
#include <math.h>
#include "mathlib/mathlib.h"

#define EPSILON 1e-9
#define ASSERT_DOUBLE_EQ(expected, actual) \
    assert(fabs((expected) - (actual)) < EPSILON)

void test_ml_average() {
    printf("Testing ml_average...\n");
    
    // Test normal case
    double arr1[] = {1.0, 2.0, 3.0, 4.0, 5.0};
    ASSERT_DOUBLE_EQ(3.0, ml_average(arr1, 5));
    
    // Test single element
    double arr2[] = {42.5};
    ASSERT_DOUBLE_EQ(42.5, ml_average(arr2, 1));
    
    // Test negative numbers
    double arr3[] = {-1.0, -2.0, -3.0};
    ASSERT_DOUBLE_EQ(-2.0, ml_average(arr3, 3));
    
    // Test mixed positive/negative
    double arr4[] = {-10.0, 10.0, 0.0};
    ASSERT_DOUBLE_EQ(0.0, ml_average(arr4, 3));
    
    // Test edge cases
    assert(ml_average(NULL, 5) == 0.0);
    assert(ml_average(arr1, 0) == 0.0);
    
    printf("All ml_average tests passed!\n");
}

void test_ml_sum() {
    printf("Testing ml_sum...\n");
    
    // Test normal case
    double arr1[] = {1.0, 2.0, 3.0, 4.0, 5.0};
    ASSERT_DOUBLE_EQ(15.0, ml_sum(arr1, 5));
    
    // Test single element
    double arr2[] = {42.5};
    ASSERT_DOUBLE_EQ(42.5, ml_sum(arr2, 1));
    
    // Test edge cases
    assert(ml_sum(NULL, 5) == 0.0);
    assert(ml_sum(arr1, 0) == 0.0);
    
    printf("All ml_sum tests passed!\n");
}

int main() {
    printf("Running math library tests...\n\n");
    
    test_ml_sum();
    test_ml_average();
    
    printf("\nAll tests passed!\n");
    return 0;
}
