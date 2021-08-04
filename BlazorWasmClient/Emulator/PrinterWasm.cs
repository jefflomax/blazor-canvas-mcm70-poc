using MCMShared.Emulator;
using Microsoft.JSInterop;
using BlazorWasmClient.JsInterop;

namespace BlazorWasmClient.Emulator
{
	public class PrinterWasm : Printer
	{
		private uint[] PackedArray;
		private int PackedOffset;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;

		public PrinterWasm
		(
			AplFont[] aplFonts,
			byte[] prErrorOff,
			byte[] prErrorOn,
			IJSUnmarshalledRuntime iJSUnmarshalledRuntime

		) : base(new byte[Printer.p_width * Printer.p_height *3],
			aplFonts,
			prErrorOff,
			prErrorOn)
		{
			// TODO: check if we over-run operations array
			PackedArray = new uint[132*66];
			PackedOffset = 0;
			_iJSUnmarshalledRuntime = iJSUnmarshalledRuntime;
		}

		public bool CPUInterruptedByPrinter => RenderRunPrinterOut0A;
		public int PrinterOperationOffset => PackedOffset;
		public uint[] PrinterOperations => PackedArray;

		public void ClearPrinterOperationList()
		{
			PackedOffset = 0;
		}

		protected override void DspAplPrinter(int i, int x, int y)
		{
			Encode(x, y, i);
		}

		public override void BlankBlock
		(
			int x, int y,
			int w, int h,
			int c
		)
		{
			Encode(x, y, c, true);
			Encode(w, h, c, true);
		}

		private (int lo, int hi) ElevenBit(int i)
		{
			return ( i & 0xFF, (i >> 8) & 0x07);
		}

		private void Encode
		(
			int x, int y,
			int ch,
			bool bit31=false
		)
		{
			var xlh = ElevenBit(x);
			var ylh = ElevenBit(y);
			var xyh = ((xlh.hi << 3) | ylh.hi);
			var packed = (uint)( xyh << 24 |
				(xlh.lo << 16) |
				(ylh.lo << 8) |
				(ch & 0xFF));
			if( bit31 )
			{
				packed |= 0x40000000;
			}
			PackedArray[PackedOffset++] = packed;
		}

		protected override void DisplayHead()
		{
			Encode(car_X, head_Y, 105);
		}

		protected override void PrinterRestore()
		{
			DrawImageToPrinter
			(
				ImagesWasm.PrinterErrorOff.Str(),
				491,
				656
			);
		}

		protected override void PrinterError()
		{
			DrawImageToPrinter
			(
				ImagesWasm.PrinterErrorOn.Str(),
				491,
				656
			);
		}

		private void DrawImageToPrinter
		(
			string imgId,
			int x,
			int y
		)
		{
			var ret = _iJSUnmarshalledRuntime.InvokeUnmarshalled<string, int, int, int>
			(
				JSMethod.drawPrinterImageUnm,
				imgId,
				x,
				y
			);
		}
	}
}
