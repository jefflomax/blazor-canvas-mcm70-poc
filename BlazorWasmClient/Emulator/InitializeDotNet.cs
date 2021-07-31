using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using MCMShared.Emulator;

namespace BlazorWasmClient.Emulator
{
	public class InitializeDotNet : Initialize
	{
		public override void SetAssembly(Assembly assembly)
		{
			_assembly = assembly;
		}

		public override void InitRom6K()
		{

		}
	}
}
