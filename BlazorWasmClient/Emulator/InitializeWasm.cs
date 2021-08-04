using System;
using System.IO;
using System.Reflection;
using MCMShared.Emulator;
using Microsoft.AspNetCore.Components;

namespace BlazorWasmClient.Emulator
{
	public class InitializeWasm : Initialize
	{
		public byte[] AllFonts;

		public override void InitRom6K()
		{
			Rom6k = new byte[0x1800]; //6144
			ReadResource(_sharedAssembly, @"MCMShared.ROM.ROM6k", Rom6k);
		}

		public override void InitRom()
		{
			Rom = new byte[0x8000];
			ReadResource(_sharedAssembly, @"MCMShared.ROM.ROMs0-C", Rom);
		}

		public override void InitFonts()
		{
			AllFonts = ReadFonts();
			ProcessFonts(AllFonts);
		}

		protected override void InitImages()
		{
			TapeEO = ImageAsByte(ImagesWasm.TapeEmptyOpened);
			TapeEC = ImageAsByte(ImagesWasm.TapeEmptyClosed);
			TapeLO = ImageAsByte(ImagesWasm.TapeLoadedOpened);
			TapeLC = ImageAsByte(ImagesWasm.TapeLoadedClosed);

			SpinLeft = ImageAsByte(ImagesWasm.SpinLeft);
			SpinRight = ImageAsByte(ImagesWasm.SpinRight);
			SpinStop = ImageAsByte(ImagesWasm.SpinStop);

			PrErrorOn = ImageAsByte(ImagesWasm.PrinterErrorOn);
			PrErrorOff = ImageAsByte(ImagesWasm.PrinterErrorOff);
		}

		private static byte[] ImageAsByte(ImagesWasm image)
		{
			return new byte[] { (byte)image };
		}

		// TODO: Vestige
		// Add NuGet package Microsoft.Extensions.FileProviders.Embedded
		// Set Build Action to Embedded Resource
		private byte[] LoadEmbeddedResource
		(
			Assembly assembly,
			string name
		)
		{
			using var stream = assembly.GetManifestResourceStream(name);
			if (stream == null)
			{
				throw new Exception($"Missing {name}");
			}
			using var binaryReader = new BinaryReader(stream);
			var bytes = binaryReader.ReadBytes((int)stream.Length);
			binaryReader.Close();
			return bytes;
		}
	}
}
