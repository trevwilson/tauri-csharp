using System.Runtime.CompilerServices;

// All native interop in this project goes through the Rust wry-ffi layer,
// which uses 1-byte bool (matching Rust's bool). Disabling runtime marshalling
// makes .NET's bool blittable at 1 byte, eliminating the need for
// [MarshalAs(UnmanagedType.U1)] annotations and enabling LibraryImport
// for all P/Invoke declarations.
[assembly: DisableRuntimeMarshalling]
