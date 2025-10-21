cmake_minimum_required(VERSION 3.16)
project(mathlib VERSION 1.0.0 LANGUAGES C)

# Set C standard
set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Compiler flags
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wall -Wextra")
set(CMAKE_C_FLAGS_DEBUG "${CMAKE_C_FLAGS_DEBUG} -g -DDEBUG")
set(CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE} -O2 -DNDEBUG")

# Include directories
include_directories(include)

# Library source files
set(MATHLIB_SOURCES
    src/mathlib.c
)

# Create static library
add_library(mathlib STATIC ${MATHLIB_SOURCES})

# Set library output directory
set_target_properties(mathlib PROPERTIES
    ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
)

# Create test executable
add_executable(test_mathlib tests/test_mathlib.c)
target_link_libraries(test_mathlib mathlib m)

# Create example executable
add_executable(example examples/example.c)
target_link_libraries(example mathlib m)

# Enable testing
enable_testing()
add_test(NAME mathlib_tests COMMAND test_mathlib)

# Custom targets for convenience
add_custom_target(run_tests
    COMMAND ${CMAKE_BINARY_DIR}/test_mathlib
    DEPENDS test_mathlib
    COMMENT "Running tests..."
)

add_custom_target(run_example
    COMMAND ${CMAKE_BINARY_DIR}/example
    DEPENDS example
    COMMENT "Running example..."
)
