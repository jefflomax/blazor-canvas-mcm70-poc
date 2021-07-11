using System;
using MCMShared.Emulator;
using Microsoft.JSInterop;
using BlazorCanvas.JsInterop;

namespace BlazorCanvas.Emulator
{
	public class DisplayWasm : Display
	{
		private readonly byte[] _memory;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;
		private readonly string _canvasId;

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
			// found no way to extract the 222 bytes for the display without copying
			// so we pass a reference to all of memory

			var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<string, byte[], int>
			(
				JSMethod.refreshSsUnm,
				_canvasId,
				_memory
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
			// To ease compatibility with the earlier emulator code, each
			// image coming from an IMG element has a single byte enumeration
			// which is also the Image IDs
			if(a == null || a.Length != 1)
			{
				return;
			}

			byte imageWasmValue = a[0];
			if ( imageWasmValue >= (byte)ImagesWasm.SpinLeft && imageWasmValue <= (byte)ImagesWasm.TapeLoadedOpened)
			{
				ImagesWasm img = (ImagesWasm)imageWasmValue;
				string imgId = img.Str();

				var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<string,int,int,int>
				(
					JSMethod.drawImageUnm,
					imgId,
					x,
					y
				);
			}
		}
	}
}
