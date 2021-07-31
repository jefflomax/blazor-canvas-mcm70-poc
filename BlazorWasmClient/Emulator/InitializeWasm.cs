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
		// TODO: This was supposed to be the MCMShared assembly
		public Assembly GetAssembly => _assembly;

		public override void SetAssembly(Assembly assembly)
		{
			_assembly = assembly;
		}

		public override void InitRom6K()
		{
			Rom6k = new byte[0x1800]; //6144
			ReadResource(@"BlazorWasmClient.Shared.ROM.ROM6k", Rom6k);
		}

		public override void InitRom()
		{
			Rom = new byte[0x8000];
			ReadResource(@"BlazorWasmClient.Shared.ROM.ROMs0-C", Rom);
		}

		public override void InitFonts()
		{
			AllFonts = LoadEmbeddedResource
			(
				"BlazorWasmClient.Shared.APL_F.apl_fonts.data"
			);
			ProcessFonts(AllFonts);
		}

		protected override void InitImages(Assembly assembly)
		{
			TapeEO = ImageAsByte(ImagesWasm.TapeEmptyOpened);
			TapeEC = ImageAsByte(ImagesWasm.TapeEmptyClosed);
			TapeLO = ImageAsByte(ImagesWasm.TapeLoadedOpened);
			TapeLC = ImageAsByte(ImagesWasm.TapeLoadedClosed);

			SpinLeft = ImageAsByte(ImagesWasm.SpinLeft);
			SpinRight = ImageAsByte(ImagesWasm.SpinRight);
			SpinStop = ImageAsByte(ImagesWasm.SpinStop);
		}

		private static byte[] ImageAsByte(ImagesWasm image)
		{
			return new byte[] { (byte)image };
		}

		// Add NuGet package Microsoft.Extensions.FileProviders.Embedded
		// Set Build Action to Embedded Resource
		private byte[] LoadEmbeddedResource
		(
			string name
		)
		{
			using var stream = _assembly.GetManifestResourceStream(name);
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
