#ifndef MATHLIB_H
#define MATHLIB_H

#include <stddef.h>

/**
 * @file mathlib.h
 * @brief A simple mathematics library
 */

/**
 * @brief Calculate the sum of an array of doubles
 * @param arr Pointer to array of double values
 * @param size Number of elements in the array
 * @return Sum of all values, or 0.0 if size is 0 or arr is NULL
 */
 double ml_sum(const double *arr, size_t size);

/**
 * @brief Calculate the average of an array of doubles
 * @param arr Pointer to array of double values
 * @param size Number of elements in the array
 * @return Average value, or 0.0 if size is 0 or arr is NULL
 */
double ml_average(const double *arr, size_t size);



#endif // MATHLIB_H
