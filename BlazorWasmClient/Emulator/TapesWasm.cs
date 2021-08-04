using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using MCMShared.Emulator;
using System.Reflection;
using Microsoft.JSInterop;
using BlazorWasmClient.JsInterop;

namespace BlazorWasmClient.Emulator
{
	public class TapesWasm : Tapes
	{
		private readonly Assembly _mcmSharedAssembly;
		private readonly byte[] _allFonts;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;

		private TapeEntriesWasm _tapeEntries;
		private Action<int> _tapeChanged;
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
			byte[] allFonts,
			Action<int> tapeChanged
		) :base(tapeLo, tapeEo, spinStop, spinRight, spinLeft, aplFonts)
		{
			_iJSUnmarshalledRuntime = iJSUnmarshalledRuntime;
			_allFonts = allFonts;
			_mcmSharedAssembly = mcmSharedAssembly;
			_tapeChanged = tapeChanged;

			_tapeEntries = new TapeEntriesWasm();
			TapeEntries();
		}

		public override void TapeEntries()
		{
			_tapeEntries.AddSystemTapeEntries();
		}

		protected override TapeEntry GetTapeEntry(int id)
		{
			return _tapeEntries.GetTapeEntry(id);
		}

		public override int NextTapeIndex(int index)
		{
			if (++index < _tapeEntries.GetTapeEntryCount())
			{
				return index;
			}

			return 0;
		}

		public TapeEntriesWasm GetTapeEntries() => _tapeEntries;

		public (byte[] Tape, string Name) GetTapeEntryImage(int id)
		{
			var te = _tapeEntries.GetById(id);
			var tapeData = te.GetTapeData();
			if (tapeData==null)
			{
				throw new Exception("Missing tape data");
			}
			var length = GetTapeAscii(tapeData, null);
			var bytes = new byte[length];
			GetTapeAscii(tapeData, bytes);

			return (bytes,te.GetName());
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
			// TODO: Check in tapeEntries
			return true;
		}

		protected override bool IsPreloaded(int tapeEntryId)
		{
			return _tapeEntries.IsPreloaded(tapeEntryId);
		}

		protected override int[] GetTapeData(int tapeEntryId)
		{
			return _tapeEntries.GetTapeData(tapeEntryId);
		}

		protected override string GetFileName(TapeEntry te)
		{
			return te.GetName();
		}

		public override bool IsEject(int tapeDrive)
		{
			return false;
		}

		// Right now, only eject saves a tape, and only the most
		// recent
		protected override void SaveTape(string s, int id, int tapeEntryId)
		{
			int i, length;
			int[] currentTape;

			if (id == 0)
			{
				currentTape = tape0;
				length = tape0_s.length;
			}
			else
			{
				currentTape = tape1;
				length = tape1_s.length;
			}

			Console.WriteLine($"WASM Save Tape {s} {tapeEntryId}");
			_tapeEntries.SetTapeData(tapeEntryId, currentTape);

			if(!GetTapeEntry(tapeEntryId).IsEject)
			{
				// Notify UI
				_tapeChanged(tapeEntryId);
			}
		}

		private int GetTapeAscii(int[] tape, byte[] memory)
		{
			var updateBuffer = memory != null;
			var totalLength = 0;

			for (var i = 0; i < tape.Length; i++)
			{
				// TODO: This was backwards in orig
				if (tape[i] > 255)
				{
					if(updateBuffer)
					{
						WriteToBuffer(memory, tape[i], totalLength);
					}
					totalLength += 4;
				}
				else
				{
					if (updateBuffer)
					{
						WriteToBuffer(memory, tape[i], totalLength);
					}
					totalLength +=3;
				}
				if (tape[i] == 0x33 && tape[i - 1] == 0x17 && tape[i - 2] == 0x17)
				{
					// 0D 0A
					if (updateBuffer)
					{
						memory[totalLength] = 13;
						memory[totalLength+1] = 10;
					}
					totalLength += 2;
				}
			}

			return totalLength;
		}

		private int WriteToBuffer(byte[] memory, int val, int offset)
		{
			if(val > 255)
			{
				memory[offset++] = (byte)'1'; // max value is $100
				val -= 256;
			}

			memory[offset++] = Nibble(val >> 4);
			memory[offset++] = Nibble(val & 0x0F);

			memory[offset++] = 32;

			return offset;
		}

		private byte Nibble(int n)
		{
			return (n <= 9)
				? (byte) ('0' + n)
				: (byte) ('A'-10 + n);
		}

		public TapeEntryWasm GetTapeEntryWasm(int i) =>
			_tapeEntries.GetById(i);

		public void AddTapeEntry
		(
			string name,
			byte[] rawImage
		)
		{
			// Over allocate array
			var maximumTapeWords = MinimumTapeLength(rawImage.Length / 3);
			var tapeData = new int[maximumTapeWords];

			// TODO: Try passing the memory stream instead
			var memoryStream = new MemoryStream(rawImage, writable:false);
			var streamReader = new StreamReader(memoryStream);

			var actualTapeWords = ReadTapeFromStream(tapeData, streamReader);
			if (tapeData.Length != actualTapeWords)
			{
				// Creates new array & copies data
				Array.Resize(ref tapeData, actualTapeWords);
			}
			if ( actualTapeWords < 10000)
			{
				Array.Fill(tapeData, 0x100);
			}

			_tapeEntries.Add(name, embedded: false, tapeData);
		}

		// Add NuGet package Microsoft.Extensions.FileProviders.Embedded
		// Set Build Action to Embedded Resource
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
