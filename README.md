# blazor-canvas-mcm70-poc

This is a port of the [York University](http://www.cse.yorku.ca/museum/collections/MCM/MCM.htm) MCM/70 emulator by Zbigniew Stachniak.

The [MCM/70](https://en.wikipedia.org/wiki/MCM/70) was built using the Intel 8008, their first 8 bit microprocessor, and ran APL.

The original C source is now C# running on .NET Core 5 using OpenTK, and also running in Web Assembly via Blazor.  You can build this with [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/) completely free, or using the .NET Core 5 command line tools.

The Blazor version is at https://blazorcanvas20210704221924.azurewebsites.net/

Blazor Proof of Concept uses unmarshalled javascript calls to manipulate the bitmap of a canvas.
