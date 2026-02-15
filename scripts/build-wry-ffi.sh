#!/bin/bash
# Build wry-ffi and copy to .NET project
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
WRY_FFI_DIR="$PROJECT_ROOT/src/wry-ffi"
DOTNET_BIN="$PROJECT_ROOT/src/TauriCSharp/TauriCSharp.TestApp/bin/Debug/net8.0"

# Parse arguments
BUILD_TYPE="release"
if [[ "$1" == "--debug" ]]; then
    BUILD_TYPE="debug"
fi

echo "Building wry-ffi ($BUILD_TYPE)..."
cd "$WRY_FFI_DIR"

if [[ "$BUILD_TYPE" == "release" ]]; then
    cargo build --release
    LIB_PATH="$WRY_FFI_DIR/target/release"
else
    cargo build
    LIB_PATH="$WRY_FFI_DIR/target/debug"
fi

# Determine library name based on OS
case "$(uname -s)" in
    Linux*)
        LIB_NAME="libwry_ffi.so"
        ;;
    Darwin*)
        LIB_NAME="libwry_ffi.dylib"
        ;;
    MINGW*|CYGWIN*|MSYS*)
        LIB_NAME="wry_ffi.dll"
        ;;
    *)
        echo "Unknown OS"
        exit 1
        ;;
esac

# Copy to .NET bin directory (create if needed)
mkdir -p "$DOTNET_BIN"
cp "$LIB_PATH/$LIB_NAME" "$DOTNET_BIN/"

echo "Copied $LIB_NAME to $DOTNET_BIN/"
echo "Done!"
