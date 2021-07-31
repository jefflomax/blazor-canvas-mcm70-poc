using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MCMShared.Emulator;

namespace BlazorWasmClient.Emulator
{
	public class TapesDotNet : Tapes
	{
		public List<TapeEntry> _tapeEntryList;
		public TapesDotNet
		(
			byte[] tapeLo,
			byte[] tapeEo,
			byte[] spinStop,
			byte[] spinRight,
			byte[] spinLeft,
			AplFont[] aplFonts
		) : base(tapeLo, tapeEo, spinStop, spinRight, spinLeft, aplFonts)
		{
			_tapeEntryList = new List<TapeEntry>();
			TapeEntries();
		}

		public override void TapeEntries()
		{
			// Do I need to port freeGLUT freeglut_menu.c
			// Read tapes from CONFIG, display on Printer?
			// Temporary fix show tape name on cassette, cycle thru with right click

			_tapeEntryList.AddRange
			(
				Directory
					.EnumerateFiles("tapes", "*.*", SearchOption.AllDirectories)
					.Select((f, ind) => new TapeEntry(ind, f))
			);
		}

		protected override TapeEntry GetTapeEntry(int id)
		{
			var tapeEntry = _tapeEntryList.First(t => t.Id == id);
			return tapeEntry;
		}

		public override int NextTapeIndex(int index)
		{
			if (++index < _tapeEntryList.Count)
			{
				return index;
			}

			return 0;
		}


	}
}
