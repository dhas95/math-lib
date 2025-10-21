#!/bin/bash

# Math Library Development Environment
# This script launches a Docker container for C development

echo "Starting Math Library Development Environment..."

# Build the development image if it doesn't exist
if [[ "$(docker images -q mathlib-dev 2> /dev/null)" == "" ]]; then
    echo "Building development image..."
    docker build -t mathlib-dev .
fi

# Start interactive development container
echo "Entering development environment..."
docker run --rm -it \
    -v $(pwd):/workspace \
    -w /workspace \
    mathlib-dev

echo "Exited development environment."
