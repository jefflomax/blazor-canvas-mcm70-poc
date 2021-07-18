using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCMShared.Emulator;

namespace BlazorCanvas.Emulator
{
	public class TapeEntryWasm : TapeEntry
	{
		private int[] _tapeData;
		private bool _changed;
		private bool _embedded;
		public bool HasData { get; private set; }
		public int[] GetTapeData() => _tapeData;

		public TapeEntryWasm(int id, string name, bool embedded, bool hasData, bool isEject)
			: base(id, name, isEject)
		{
			_tapeData = null;
			_changed = false;
			_embedded = embedded;
			HasData = hasData;
		}

		public TapeEntryWasm(int id, string name, bool embedded, int[] data)
			: base(id, name, false)
		{
			_tapeData = data;
			_changed = false;
			_embedded = embedded;
			HasData = true;
		}

		public void SetTapeData(int[] data)
		{
			if( IsEject)
			{
				return;
			}
			_changed = (_tapeData == null)
				? true
				: CompareData(data);
			_tapeData = data;
			HasData = true;
		}

		public override string GetName()
		{
			if (_name.StartsWith(TapeEntriesWasm.ResourceName))
			{
				return _name.Substring(TapeEntriesWasm.ResourceName.Length);
			}
			return _name;
		}

		private bool CompareData(int[] data)
		{
			return data.Length != _tapeData.Length;
		}
	}
}
