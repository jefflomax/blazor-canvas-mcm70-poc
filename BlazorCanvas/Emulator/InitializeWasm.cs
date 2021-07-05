using System;
using System.IO;
using System.Reflection;
using MCMShared.Emulator;

namespace BlazorCanvas.Emulator
{
	public class InitializeWasm : Initialize
	{
		public byte[] AllFonts;

		public override void SetAssembly(Assembly assembly)
		{
			_assembly = assembly;
		}

		public override void InitRom6K()
		{
			Rom6k = new byte[0x1800]; //6144
			ReadResource(@"BlazorCanvas.Shared.ROM.ROM6k", Rom6k);
		}

		public override void InitRom()
		{
			Rom = new byte[0x8000];
			ReadResource(@"BlazorCanvas.Shared.ROM.ROMs0-C", Rom);
		}

		public override void InitFonts()
		{
			AllFonts = LoadEmbeddedResource
			(
				"BlazorCanvas.Shared.APL_F.apl_fonts.data"
			);
			ProcessFonts(AllFonts);
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
