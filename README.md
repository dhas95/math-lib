# Math Library

A minimal C mathematics library with Docker development environment.

## Features

- Calculate average and sum of double arrays
- Clean C99 code with proper error handling
- Comprehensive test suite
- Docker development environment

## Quick Start

### Development Environment

Start the Docker development environment:

```bash
./dev.sh
```

### Building

Inside the development environment:

```bash
# Create build directory
mkdir build && cd build

# Configure with CMake
cmake ..

# Build everything
make

# Run tests
make test
# or
./test_mathlib

# Run example
./example
```

### Quick Development Workflow

```bash
# Enter development environment
./dev.sh

# Inside container:
mkdir build && cd build
cmake ..
make && ./test_mathlib

# Edit code (files are shared with host)
vim ../src/mathlib.c

# Rebuild and test
make && ./test_mathlib
```

## API Documentation

### Functions

#### `ml_average`
```c
double ml_average(const double *arr, size_t size);
```
Calculate the average of an array of doubles.

**Parameters:**
- `arr`: Pointer to array of double values
- `size`: Number of elements in the array

**Returns:** Average value, or 0.0 if size is 0 or arr is NULL

#### `ml_sum`
```c
double ml_sum(const double *arr, size_t size);
```
Calculate the sum of an array of doubles.

**Parameters:**
- `arr`: Pointer to array of double values  
- `size`: Number of elements in the array

**Returns:** Sum of all values, or 0.0 if size is 0 or arr is NULL

## Project Structure

```
math-lib/
├── include/mathlib/    # Public headers
├── src/               # Implementation files
├── tests/             # Test files
├── examples/          # Example usage
├── build/             # Build directory (created by CMake)
├── CMakeLists.txt     # CMake configuration
├── Dockerfile         # Development environment
└── dev.sh            # Development environment launcher
```

## Requirements

- Docker (for development environment)
- OR: GCC + CMake (for local development)
