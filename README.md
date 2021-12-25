# blazor-canvas-mcm70-poc

This is a port of the [York University](http://www.cse.yorku.ca/museum/collections/MCM/MCM.htm) MCM/70 emulator by Zbigniew Stachniak.  

The original C source is now C# running on .NET Core 5 using OpenTK, and also running in Web Assembly via Blazor.

The Blazor version is at https://blazorcanvas20210704221924.azurewebsites.net/

Blazor Proof of Concept uses unmarshalled javascript calls to manipulate the bitmap of a canvas.
