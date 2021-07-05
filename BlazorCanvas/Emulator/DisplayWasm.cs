using System;
using MCMShared.Emulator;
using Microsoft.JSInterop;
using BlazorCanvas.JsInterop;

namespace BlazorCanvas.Emulator
{
	public class DisplayWasm : Display
	{
		private byte[] _memory;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;
		private string _canvasId;
		public DisplayWasm
		(
			byte[] panel,
			byte[] memory,
			int x, int y,
			AplFont[] aplFonts,
			string canvasId,
			IJSUnmarshalledRuntime iJSUnmarshalledRuntime
		)
			: base(panel, memory, x, y, aplFonts)
		{
			_memory = memory;
			_canvasId = canvasId;
			_iJSUnmarshalledRuntime = iJSUnmarshalledRuntime;
		}

		//------------------------------------------------------
		//      refresh SelfScan; all 222 columns are refreshed
		//------------------------------------------------------
		public override void refresh_SS()
		{
			// display every column represented by bytes stored in mem[2021]-mem[20FE]
			ReadOnlySpan<byte> displayBytes = _memory;
			var display = displayBytes.Slice(0x2021, 222);
			var bytes = display.ToArray(); // sadly this makes a copy
			// TODO:
			// + why not pass the address of the entire memory and let JS index into it?
			// - or alter all memory writes to go to a buffer the size of the display?

			var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<string, byte[], int>
			(
				JSMethod.refreshSsUnm,
				_canvasId,
				bytes
			);
		}

		//--------------------------------------------------------------------
		// clear SelfScan
		//--------------------------------------------------------------------
		public override void ClearDisplay()
		{
			var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<string, int>
			(
				JSMethod.clearSsUnm,
				_canvasId
			);
		}

		public override void SubImage(int x, int y, int w, int h, byte[] a)
		{

		}
	}
}
