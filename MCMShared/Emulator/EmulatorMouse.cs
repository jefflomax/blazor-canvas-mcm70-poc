using System;

namespace MCMShared.Emulator
{
	public class EmulatorMouse
	{
		private Machine _machine;
		private Display _display;
		private Tapes _tapes;
		private readonly byte[] _tapeLc;
		private readonly byte[] _tapeEc;
		private readonly byte[] _tapeLo;
		private readonly byte[] _tapeEo;

		private int _lastMouseX;
		private int _lastMouseY;

		private int _tape0LoadIndex;
		private int _tape1LoadIndex;

		public EmulatorMouse
		(
			byte[] tapeLc,
			byte[] tapeEc,
			byte[] tapeLo,
			byte[] tapeEo
		)
		{
			_tapeLc = tapeLc;
			_tapeEc = tapeEc;
			_tapeLo = tapeLo;
			_tapeEo = tapeEo;
			_tape0LoadIndex = 0;
			_tape1LoadIndex = 0;
		}

		public void SetMachine
		(
			Machine machine
		)
		{
			_machine = machine;
			_tapes = _machine.Tapes ?? throw new NotSupportedException("Tapes must be setup first");
			_display = machine.Display ?? throw new NotSupportedException("Display must be setup first");
		}

		/*===============================  MOUSE  ============================*/

		private static bool IsTapeY(int y)
		{
			return y > 160 && y < 360;
		}

		private static bool IsLeftTapeX(int x)
		{
			return x > 40 && x < 420;
		}

		public static bool IsRightTapeX(int x)
		{
			return x > 500 && x < 880;
		}

		//------------------------------------------------------------------ 
		// mouse_clicks: handler for mouse clicks
		//------------------------------------------------------------------
		public MouseAction MouseClick
		(
			MouseButtonSel button,
			bool isPressed,
			bool isShifted,
			float fx,
			float fy
		)
		{
			var x = (int) fx;
			var y = (int) fy;
			_lastMouseX = x;
			_lastMouseY = y;

			if (isPressed && button == MouseButtonSel.Left)
			{
				// CASE: MCM reset requested
				if ((y > 18) && (y < 40) && (x > 86) && (x < 136))
				{
					_machine.ResetMCM();
					return MouseAction.None;
				}

				// CASE: connect printer
				if (y > 18 && y < 40 && x > 18 && x < 60)
				{
					var printer = _machine.Printer;

					if (! _machine.McmOn)
					{
						Console.WriteLine("Please START (TAB) before opening Printer");
						return MouseAction.None;
					}

					//printer.Redisplay = true;

					if (printer.PrinterConnected && isShifted)
					{
						printer.StorePage();
						return MouseAction.None;
					}

					printer.SetPrinterConnected(!printer.PrinterConnected);

					if (printer.PrinterConnected)	// initiate printer's window
					{
						printer.ABC = 0;			// Answer-Back code not requested
						printer.pr_status = 241;	// set printer's status to "ready"
						printer.InitializePrinterHead = true;
						return MouseAction.PrinterOn;
					}
					else
					{
						printer.ClearPage();
						return MouseAction.PrinterOff;
					}

				}

				// CASE: open/close tape 0 lid requested
				// CASE: open/close tape 1 lid  requested
				if (IsTapeY(y))
				{
					var isLeftTape = IsLeftTapeX(x);
					var isRightTape = IsRightTapeX(x);
					if (!(isLeftTape || isRightTape))
					{
						return MouseAction.None;
					}

					var tapeDevice = (isLeftTape)
						? 0
						: 1;
					var tape_s = (isLeftTape)
						? _tapes.tape0_s
						: _tapes.tape1_s;
					var r = (isLeftTape)
						? new Rectangle(40, 148, 409, 256)
						: new Rectangle(483, 148, 209, 256);

					if (_tapes.IsEject(tapeDevice))
					{
						//_tapes.SubImage(40, 148, 409, 256, _tape_eo);   // display: no tape, lid opened
						_tapes.EjectTape(tapeDevice);
					}

					tape_s.lid = 1 - tape_s.lid;  // flip lid status 

					Console.WriteLine($"TAPE {tapeDevice} LID NOW {(tape_s.lid == 0 ? "CLOSED" : "OPEN")}");

					// display the result of action on panel
					if (tape_s.lid == 0)
					{
						//if ((tape0_s.status & 0x04) == 0)
						if(_tapes.FinalizeTapeLoad(0))
						{
							_display.SubImage(r, _tapeLc); // display: tape loaded, lid closed
						}
						else
						{
							_display.SubImage(r, _tapeEc); // display: no tape, lid closed
						}
					}
					else if ((tape_s.status & 0x04) == 0)
					{
						_tapes.LabelCassetteN(tape_s.name, 154, 183);  // write label on tape
						_display.SubImage(r, _tapeLo);  // display: tape loaded, lid opened
					}
					else
					{
						_display.SubImage(r, _tapeEo);   // display: no tape, lid opened
					}
				}

			}

			else if (isPressed && button == MouseButtonSel.Right)
			{
				if (!IsTapeY(_lastMouseY))
				{
					return MouseAction.None;
				}

				if (IsLeftTapeX(_lastMouseX))
				{
					if (!_tapes.IsTapeClosed(0))
					{
						_tapes.TapeMenu(_tape0LoadIndex,0);
						_tape0LoadIndex = _tapes.NextTapeIndex(_tape0LoadIndex);
					}
				}
				else if (IsRightTapeX(_lastMouseX))
				{
					if (!_tapes.IsTapeClosed(1))
					{
						_tapes.TapeMenu(_tape1LoadIndex,1);
						_tape1LoadIndex = _tapes.NextTapeIndex(_tape1LoadIndex);
					}
				}
			}
			return MouseAction.None;
		}
	}
}