# blazor-canvas-mcm70-poc

Proof of Concept for using unmarshalled javascript calls to manipulate the bitmap of a canvas.

While the .NET/WASM => JS unmarshalled interop works fine, there is no way to pass the JavaScript sourced UInt8ClampedArray by reference unmarshalled to .NET:

https://github.com/dotnet/aspnetcore/issues/26287
