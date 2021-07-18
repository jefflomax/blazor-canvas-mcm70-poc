using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCMShared.Emulator
{
	public interface ITapeEntry
	{
		void AddSystemTapeEntries();
		TapeEntry GetTapeEntry(int id);
		int GetTapeEntryCount();
	}
}
