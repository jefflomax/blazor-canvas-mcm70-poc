using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorCanvas.Emulator;
using MCMShared.Emulator;
using Microsoft.JSInterop;

namespace BlazorCanvas.Runner
{
	public class Game
	{
		private readonly AppState _appState;
		private readonly InitializeWasm _emulatorData;
		private readonly IJSUnmarshalledRuntime _iJSUnmarshalledRuntime;
		private readonly string _canvasId;

		private const int EmulatorWidth = 932; // emulator window's width
		private const int EmulatorHeight = 722;

		private bool _isInitialized;
		private Machine _machine;
		private Display _display;
		private Keyboard _keyboard;
		private Cpu _cpu;
		private PrinterWasm _printer;
		private PrinterMouse _printerMouse;
		
		// In order to compute 0.7 / iota 255 in 50 seconds,
		// we need to execute around 65000 instructions per second
		private static readonly int InstructionsPerFrame = 1090; // Until we have timing

		private int _instructionCounter = 0;
		private int _lastInstructionCounter = 0;

		public bool PrinterRedisplay
		{
			get { return _printer.Redisplay; }
			set { _printer.Redisplay = value; }
		}
		public byte[] PrinterWindow => _printer._printerWindow;

		public void ClearPrinterOperationList() => _printer.ClearPrinterOperationList();

		public int PrinterOperationOffset => _printer.PrinterOperationOffset;
		public uint[] PrinterOperations => _printer.PrinterOperations;

		public byte[] AllFonts => _emulatorData.AllFonts;

		public Game
		(
			InitializeWasm initialize,
			string canvasId,
			AppState appState,
			IJSUnmarshalledRuntime iJSUnmarshalledRuntime
		)
		{
			_iJSUnmarshalledRuntime = iJSUnmarshalledRuntime;
			_canvasId = canvasId;
			_isInitialized = false;
			_emulatorData  = initialize;
			_appState = appState;
			_machine = null;
			_cpu = null;
			_keyboard = null;
			_display = null;
			_printer = null;
			_instructionCounter = 0;
		}

		public bool IsInitialized => _isInitialized;

		public async ValueTask Init(InitializeWasm emulatorData)
		{
			_machine = new Machine();
			_printer = new PrinterWasm
			(
				emulatorData.AplFonts,
				emulatorData.PrErrorOff,
				emulatorData.PrErrorOn
			);
			_machine.AddPrinter(_printer);

			_printerMouse = new PrinterMouse(_printer);

			_display = new DisplayWasm
			(
				emulatorData.Panel,
				_machine.Memory,
				EmulatorWidth,
				EmulatorHeight,
				emulatorData.AplFonts,
				_canvasId,
				_iJSUnmarshalledRuntime
			);
			_machine.AddDisplay(_display);

			_keyboard = new Keyboard();
			_machine.AddKeyboard(_keyboard);

			var tapes = new Tapes
			(
				emulatorData.TapeLO,
				emulatorData.TapeEO,
				emulatorData.SpinStop,
				emulatorData.SpinRight,
				emulatorData.SpinLeft,
				emulatorData.AplFonts
			);
			_machine.AddTapes(tapes);

			var emulatorMouse = new EmulatorMouse
			(
				emulatorData.TapeLC,
				emulatorData.TapeEC,
				emulatorData.TapeLO,
				emulatorData.TapeEO
			);
			_machine.AddEmulatorMouse(emulatorMouse);

			_cpu = new Cpu(showDisassembly:false);
			_machine.AddCpu(_cpu, showOpCodeListing:false);

			_cpu.ResetCpu();

			_cpu.InitMemory
			(
				emulatorData.Rom6k,
				emulatorData.Rom
			);

			await Task.CompletedTask;
		}

		//private long minMilliseconds = long.MaxValue;
		//private long maxMilliseconds = long.MinValue;
		//private long minTicks = long.MaxValue;
		//private long maxTicks = long.MinValue;

		//private static int fps = 30;
		//private long now;
		private long then;
		//private float interval = 1000/fps;
		//private long delta;
		private bool displayedDuringStep;
		public async ValueTask<int> Step()
		{
			await Task.CompletedTask;
			if (!_isInitialized)
			{
				//await Init(_emulatorData);

				GameTime.Start();
				then = GameTime.TotalMilliseconds;

				_isInitialized = true;
			}
			displayedDuringStep = false;

			GameTime.Step();

#if false
			if (GameTime.ElapsedTicks < minTicks)
				minTicks = GameTime.ElapsedTicks;
			if (GameTime.ElapsedTicks > maxTicks)
				maxTicks = GameTime.ElapsedTicks;

			if (GameTime.ElapsedMilliseconds < minMilliseconds)
				minMilliseconds = GameTime.ElapsedMilliseconds;
			if (GameTime.ElapsedMilliseconds > maxMilliseconds)
				maxMilliseconds = GameTime.ElapsedMilliseconds;
#endif

			if (_printer.pr_op_code == 0) // Printer animation, skip CPU entirely
			{
				// https://stackoverflow.com/questions/32656443/timing-in-requestanimationframe
				// Intel 8008 clock speed 500Mhz
				// 500,000,000 cycles per second
				// 1ms = 1 / 1000 of a second
				//var instructions = Math.Min(1, GameTime.ElapsedMilliseconds);
				for(int i = 0; i< InstructionsPerFrame; i++)
				{
					// RefreshDisplayCounter incremented on every write to the display
					if (_machine.RefreshDisplayCounter >= 56 || _machine.InstrCount >= 1000)
					{
						if (displayedDuringStep)
						{
							//Console.WriteLine($"CPU interrputed for DISPLAY {i} {GameTime.ElapsedTicks}");
							break; // Try to limit to 1 display refresh per Step
						}

						_machine.RefreshDisplayCounter = 0;
						_machine.InstrCount = 0;
						_display.refresh_SS();
						displayedDuringStep = true;
					}

					_cpu.RunCpu(); // Process one instruction
					_instructionCounter++;
					if (_machine.RefreshDisplayCounter != 0)
					{
						_machine.InstrCount++;
					}

					if(_printer.CPUInterruptedByPrinter)
					{
						// The only CPU action that shifts us to the printer
						break;
					}
				}

			}


			if(_machine.Printer.RenderPending )
			{
				if(_printer.pr_op_code != 0)
				{
					_printer.RunPrinter(_printer.pr_op_code, isAnimation: true);
				}

				if (_printer.InitializePrinterHead)
				{
					_printer.InitializePrinterHead = false;
					_printer.RenderInitializePrinterHead();
				}

				if (_printer.RenderResetHead)
				{
					_printer.RenderResetHead = false;
					_printer.ResetHead();
				}

				if (_printer.RenderRunPrinterOut0A)
				{
					_printer.RenderRunPrinterOut0A = false;
					_printer.RunPrinter(_printer.RenderRunPrinterOut0AData, isAnimation: false);
				}

			}

#if false
			if(_instructionCounter - _lastInstructionCounter > 3000000)
			{
				_lastInstructionCounter = _instructionCounter;
				float totalSeconds = ((GameTime.TotalMilliseconds - then)/1000);
				float ips = _instructionCounter / totalSeconds;
				_appState.SetInstructionsPerSecond((int)ips);
			}
#endif

			return (_instructionCounter > int.MaxValue-(InstructionsPerFrame*10))
				? -1 // TODO: Signal emulator stop
				: _instructionCounter;
		}

		public void Key(byte key)
		{
			_keyboard.keyboard(key);
		}

		public MouseAction EmulatorClick
		(
			MouseButtonSel mb,
			bool isShift,
			double x,
			double y
		)
		{
			var fx = (float)x;
			var fy = (float)y;
			return _machine.EmulatorMouse.MouseClick
			(
				mb,
				isPressed:true,
				isShift,
				fx,
				fy
			);
		}

		public void PrinterClick
		(
			bool isLeftButton,
			double x,
			double y
		)
		{
			var fx = (float)x;
			var fy = (float)y;
			_printerMouse.MouseClick
			(
				isLeftButton,
				isPressed: true,
				fx,
				fy
			);
		}

		public GameTime GameTime { get; } = new();

	}
}
