using System;
using BlazorWasmClient.Emulator;

namespace BlazorWasmClient.Emulator
{
	public static class ExtensionsWasm
	{
		public static string Str(this ImagesWasm image)
		{
			return Enum.GetName<ImagesWasm>(image);
		}
	}
}
