using System;
using MCMShared.Extensions;

namespace MCMShared.Emulator
{
	public class Machine
	{
		private Cpu _cpu;
		private Display _display;
		private Keyboard _keyboard;
		private Printer _printer;
		private EmulatorMouse _mouse;
		private Tapes _tapes;
		private byte _selectedDevice;

		public bool McmOn;			// records the MCM/70's on/off  status
		public bool Power;			// MCM/70 connected to power source (external or battery)

		public Machine()
		{
			_selectedDevice = 0;
			RefreshDisplayCounter = 0;
			InstrCount = 0;

			_cpu = null;
			_display = null;
			_keyboard = null;
			_mouse = null;
			_printer = null;

			McmOn = false;

			Power = true;

			Memory = new byte[0x4000]; // 16K
		}

		public void AddCpu(Cpu cpu, bool showOpCodeListing)
		{
			_cpu = cpu;
			cpu.SetMachine(this);

			if (showOpCodeListing)
			{
				var opcodes = _cpu.Opcodes;
				for (int i = 0; i < opcodes.Length; i++)
				{
					var o = opcodes[i];
					if (o != null)
					{
						byte b = o.Instruction;
						var s = b.ToBinary();
						Console.WriteLine($"{o.Instruction:X2} {s.Substring(0, 2)} {s.Substring(2, 3)} {s.Substring(5, 3)} {o.Op}");
					}
				}
			}
		}

		public byte GetCurrentInstruction => _cpu.GetCurrentInstruction;

		public void AddDisplay(Display display)
		{
			_display = display;
		}

		public void AddPrinter(Printer printer)
		{
			_printer = printer;
		}

		public void AddKeyboard(Keyboard keyboard)
		{
			keyboard.SetMachine(this);
			_keyboard = keyboard;
		}

		public void AddEmulatorMouse(EmulatorMouse mouse)
		{
			_mouse = mouse;
			mouse.SetMachine(this);
		}
		public Keyboard Keyboard => _keyboard;

		public void AddTapes(Tapes tapes)
		{
			_tapes = tapes;
			tapes.SetMachine(this);
		}

		// current I/O device:
		// - 0x01 - printer
		// - 0xC8 - Tape 0 (left tape)
		// - 0xC9 - Tape 1 (right tape)
		public byte SelectedDevice
		{
			get
			{
				return _selectedDevice;
			}
			set
			{
				Display.SelectedDevice(value);
				_selectedDevice = value;
			}
		}

		public int RefreshDisplayCounter { get; set; }
		public int InstrCount { get; set; }

		public Display Display => _display;
		public Tapes Tapes => _tapes;
		public byte[] Memory { get; }
		public Printer Printer => _printer;
		public EmulatorMouse EmulatorMouse => _mouse;

		public void Sleep(int v)
		{
			// Stopwatch?  Other high resolution timer?
			// System.Threading.Thread.Sleep(v);
		}

		public void Sleep1(int v)
		{
			System.Threading.Thread.Sleep(v);
		}

		//------------------------------------------------------------------
		// reset the emulator
		//------------------------------------------------------------------
		// ReSharper disable once InconsistentNaming
		public void ResetMCM()
		{
			// See CPU.reset_mcm
			if (! McmOn)
			{
				Console.WriteLine("Please START (tab) before reset");
				return;
			}

			_display.ClearDisplay();		// clear display
			_display.ClearAllesLookensgepeepers();

			RefreshDisplayCounter = 0;		// refresh display flag set to "no refresh"
			InstrCount = 0;					// instruction counter set to 0; this counter is used with refresh display (program)
			_cpu.ResetCpu();				// reset CPU
			_selectedDevice = 0;			// no device selected
			McmOn = false;					// MCM is in "off" state
		}
	}
}
