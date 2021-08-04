using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCMShared.Emulator;

namespace MCM70Client.Emulator
{
	public class InitializeDotNet : Initialize
	{
		public override void InitFonts()
		{
			byte[] fonts = ReadFonts();
			ProcessFonts(fonts);
		}
	}
}
