FROM gcc:latest

# Install development tools
RUN apt-get update && apt-get install -y \
    cmake \
    make \
    gdb \
    valgrind \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /workspace

# Default command (interactive bash)
CMD ["/bin/bash"]
