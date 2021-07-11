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
		private bool _hasData;
		public TapeEntryWasm(int id, string name, bool embedded, bool hasData)
			:base(id, name)
		{
			_tapeData = null;
			_changed = false;
			_embedded = embedded;
			_hasData = hasData;
		}
	}
}
