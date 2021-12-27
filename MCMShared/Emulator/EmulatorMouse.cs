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
			MouseAction mouseAction;
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
				// CASE: open/close tape 1 lid requested
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
						: new Rectangle(483, 148, 409, 256);

					// Not really what OO is good for, sometimes conditional
					// compilation is easier.  This allows the WASM code
					// with a different code flow to work.
					if(ReturnForShiftClick
						(
							tape_s,
							tapeDevice,
							isShifted,
							out mouseAction
						))
					{
						return mouseAction;
					}

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
						if(_tapes.FinalizeTapeLoad(tapeDevice))
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
			// This is unreachable by WASM
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

		private const int keyboardTop = 427;
		private const int keyRow1Left = 58;
		private const int keyRow1Bottom = 481;
		private const int keyRow2QLeft = 80;
		private const int keyRow2RBRight = 786;
		private const int keyRow2Top = 484;
		private const int keyRow2Bottom = 538;
		private const int keyRow3ALeft = 112;
		private const int keyRow3APOSRight = 817;
		private const int keyRow3Top = 541;
		private const int keyRow3Bottom = 596;
		private const int keyRow4Top = 598;
		private const int keyRow4ZLeft = 146;
		private const int keyRowSlashRight = 786;
		private const int keyboardBottom = 655;

		private const int keyWidth = 122 - 58;
		private const int keyHeight = 482 - keyboardTop;
		private const int keyHalfHeight = keyHeight >> 1;
		public bool IsKeyboardClick(int x, int y, out byte ch)
		{
			ch = 0;
			if (y < keyboardTop || y > keyboardBottom)
			{
				return false;
			}
			if (x < 3 || x > 910)
			{
				return false;
			}

			if (y < keyRow1Bottom) // 1..Backspace
			{
				if (x < keyRow1Left || x > 912)
				{
					return false;
				}
				var column = (x - keyRow1Left) / keyWidth;
				var row = (y-keyboardTop) > keyHalfHeight;
				if (row)
				{
					ch = ByteOf("1234567890-=\b", column);
				}
				else
				{
					ch = ByteOf("!@#$%^&*()_+\b", column);
				}
			}
			else if (y < keyRow2Bottom)
			{
				// special case for START, |\
				if (x < keyRow2QLeft || x > keyRow2RBRight)
				{
					return false;
				}
				var column = (x - keyRow3ALeft) / keyWidth;
				var row = (y-keyRow2Top) > keyHalfHeight;
				if (row)
				{
					ch = ByteOf("qwertyuiop[", column);
				}
				else
				{
					ch = ByteOf("QWERTYUIOP{", column);
				}
			}
			else if (y < keyRow3Bottom)
			{
				if (x < keyRow3ALeft || x > keyRow3APOSRight)
				{
					return false;
				}
				var column = (x - keyRow3ALeft) / keyWidth;
				var row = (y-keyRow3Top) > keyHalfHeight;
				if (row)
				{
					ch = ByteOf("asdfghjkl;'", column);
				}
				else
				{
					ch = ByteOf("ASDFGHJKL:\"", column);
				}
			}
			else
			{
				if (x < keyRow4ZLeft || x > keyRowSlashRight)
				{
					return false;
				}
				var column = (x - keyRow4ZLeft) / keyWidth;
				var row = (y-keyRow4Top) > keyHalfHeight;
				if (row)
				{
					ch = ByteOf("zxcvbnm,./", column);
				}
				else
				{
					ch = ByteOf("ZXCVBNM<>?", column);
				}
			}

			return true;
		}

		private static byte ByteOf(string asciiKeys, int index)
		{
			return (byte)asciiKeys[index];
		}


		protected virtual bool ReturnForShiftClick
		(
			TP tape_s,
			int tapeDevice,
			bool isShifted,
			out MouseAction returnAction
		)
		{
			returnAction = MouseAction.None;
			return false;
		}
	}
}