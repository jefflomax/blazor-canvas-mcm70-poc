using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using MCMShared.Emulator;
using System.Reflection;
using Microsoft.JSInterop;
using BlazorCanvas.JsInterop;

namespace BlazorCanvas.Emulator
{
	public class TapesWasm : Tapes
	{
		private readonly Assembly _mcmSharedAssembly;
		private readonly byte[] _allFonts;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;
		private const string ResourceName = "MCMShared.tapes.";
		public TapesWasm
		(
			byte[] tapeLo,
			byte[] tapeEo,
			byte[] spinStop,
			byte[] spinRight,
			byte[] spinLeft,
			AplFont[] aplFonts,
			IJSUnmarshalledRuntime iJSUnmarshalledRuntime,
			Assembly mcmSharedAssembly,
			byte[] allFonts
		):base(tapeLo, tapeEo, spinStop, spinRight, spinLeft, aplFonts)
		{
			_iJSUnmarshalledRuntime = iJSUnmarshalledRuntime;
			_allFonts = allFonts;
			_mcmSharedAssembly = mcmSharedAssembly;
		}

		public override void TapeEntries()
		{
			var t0 = new TapeEntry(0, $"{ResourceName}demo.tp");
			var t1 = new TapeEntry(1, $"{ResourceName}utils.tp");
			var t2 = new TapeEntry(2, $"{ResourceName}empty.tp");
			var t3 = new TapeEntry(3, "eject");
			_tapeEntryList.Add(t0);
			_tapeEntryList.Add(t1);
			_tapeEntryList.Add(t2);
			_tapeEntryList.Add(t3);
		}

		protected override void DspAplCass(int i, int x, int y)
		{
			var xy = (x << 16) + y;
			var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<byte[], int, int, int>
			(
				JSMethod.dspAplCassette,
				_allFonts,
				xy,
				i
			);
		}

		protected override StreamReader GetTapeStream(string filePath)
		{
			return LoadEmbeddedResource(filePath);
		}

		protected override bool FileExists(string fileName)
		{
			//var fi = new FileInfo(s);
			//return fi.Exists;
			return true;
		}

		protected override string GetFileName(TapeEntry te)
		{
			if (te.Name.StartsWith(ResourceName))
			{
				return te.Name.Substring(ResourceName.Length);
			}
			return te.Name;
		}

		private StreamReader LoadEmbeddedResource
		(
			string name
		)
		{
			var stream = _mcmSharedAssembly.GetManifestResourceStream(name);
			if (stream == null)
			{
				throw new Exception($"Missing {name}");
			}
			return new StreamReader(stream);
		}

	}
}
