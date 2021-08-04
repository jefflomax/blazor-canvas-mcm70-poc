using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCMShared.Emulator;

namespace BlazorWasmClient.Emulator
{
	public class EmulatorMouseWasm : EmulatorMouse
	{
		public EmulatorMouseWasm
		(
			byte[] tapeLc,
			byte[] tapeEc,
			byte[] tapeLo,
			byte[] tapeEo
		) : base(tapeLc, tapeEc, tapeLo, tapeEo)
		{
		}

		protected override bool ReturnForShiftClick
		(
			TP tape_s,
			int tapeDevice,
			bool isShifted,
			out MouseAction returnAction
		)
		{
			returnAction = MouseAction.None;
			if (tape_s.lid == 1 && isShifted)
			{
				returnAction = DeviceToAction(tapeDevice);
				return true;
			}
			return false;
		}

		private MouseAction DeviceToAction(int tapeDevice)
		{
			return tapeDevice == 0
				? MouseAction.Tape0Opened
				: MouseAction.Tape1Opened;
		}

	}
}
