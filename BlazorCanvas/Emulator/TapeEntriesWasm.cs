using System;
using System.Collections.Generic;
using System.Linq;
using MCMShared.Emulator;

namespace BlazorCanvas.Emulator
{
	public class TapeEntriesWasm : ITapeEntry
	{
		public List<TapeEntryWasm> _tapeEntryList;
		public const string ResourceName = "MCMShared.tapes.";

		public int SelectedTape0Index { get; set; }
		public int SelectedTape1Index { get; set; }
		public int SelectedDrive { get; set; }

		public TapeEntriesWasm()
		{
			_tapeEntryList = new List<TapeEntryWasm>();
			SelectedTape0Index = -1;
			SelectedTape1Index = -1;
		}

		public void SetSelectedTape(int tapeId)
		{
			if(tapeId == int.MaxValue)
			{
				return;
			}

			if(SelectedDrive == 0)
			{
				SelectedTape0Index = tapeId;
			} else
			{
				SelectedTape1Index = tapeId;
			}
		}

		public bool Selected(int tapeId)
		{
			return tapeId == ((SelectedDrive == 0)
				? SelectedTape0Index
				: SelectedTape1Index);
		}

		public bool InOtherTape(int tapeId)
		{
			return tapeId == ((SelectedDrive == 0)
				? SelectedTape1Index
				: SelectedTape0Index);
		}

		public void AddSystemTapeEntries()
		{
			_tapeEntryList.Add(EmbeddedTapeEntry(0, "demo.tp"));
			_tapeEntryList.Add(EmbeddedTapeEntry(1, "utils.tp"));
			_tapeEntryList.Add(EmbeddedTapeEntry(2, "empty.tp"));
			//_tapeEntryList.Add(EmbeddedTapeEntry(3, "eject", isEject: true, hasData: false));
		}

		public int Add(string name, bool embedded, int[] tapeData)
		{
			var nextId = _tapeEntryList.Max(te => te.Id)+1;
			var te = new TapeEntryWasm(nextId, name, embedded, tapeData);
			_tapeEntryList.Add(te);
			return nextId;
		}

		public TapeEntry GetTapeEntry(int id)
		{
			var tapeEntry = _tapeEntryList.First(t => t.Id == id);
			return tapeEntry;
		}

		public void SetTapeData(int id, int[] currentTape)
		{
			GetById(id).SetTapeData(currentTape);
		}

		public int GetTapeEntryCount()
		{
			return _tapeEntryList.Count;
		}

		public bool IsPreloaded(int tapeEntryId)
		{
			var te = GetById(tapeEntryId);
			return te.HasData;
		}

		public int[] GetTapeData(int tapeEntryId)
		{
			var te = GetById(tapeEntryId);
			return te.GetTapeData();
		}

		public TapeEntryWasm GetById(int tapeEntryId)
		{
			var te = _tapeEntryList.First(t => t.Id == tapeEntryId);
			return te;
		}

		private TapeEntryWasm EmbeddedTapeEntry
		(
			int id,
			string name,
			bool isEject = false,
			bool hasData = false
		)
		{
			return new TapeEntryWasm
			(
				id,
				$"{ResourceName}{name}",
				embedded:true,
				hasData,
				isEject
			);
		}
	}
}
