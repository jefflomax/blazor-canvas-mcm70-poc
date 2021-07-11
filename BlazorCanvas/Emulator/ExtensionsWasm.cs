using System;
using BlazorCanvas.Emulator;

namespace BlazorCanvas.Emulator
{
	public static class ExtensionsWasm
	{
		public static string Str(this ImagesWasm image)
		{
			return Enum.GetName<ImagesWasm>(image);
		}
	}
}
