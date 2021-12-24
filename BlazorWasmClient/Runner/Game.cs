using System.Threading.Tasks;
using BlazorWasmClient.Emulator;
using BlazorWasmClient.Emulator.Impl;
using MCMShared.Emulator;
using Microsoft.JSInterop;

namespace BlazorWasmClient.Runner
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
		private DisplayWasm _display;
		private Keyboard _keyboard;
		private Cpu _cpu;
		private PrinterWasm _printer;
		private PrinterMouse _printerMouse;
		private TapesWasm _tapes;

		// See GameTime.cs
		// In order to compute 0.7 / iota 255 in 50 seconds,
		// we need to execute around 65000 instructions per second

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

		public (byte[] Tape, string Name) GetTapeEntryImage(int id) =>
			_tapes.GetTapeEntryImage(id);

		public void AddTapeEntry(string name, byte[] rawImage) =>
			_tapes.AddTapeEntry(name, rawImage);

		public TapeEntriesWasm GetTapeEntries() => _tapes.GetTapeEntries();

		public void TapeMenu(int option, int tapeDevice) =>
			_tapes.TapeMenu(option, tapeDevice);

		public void EjectTape(int tapeDrive) =>
			_tapes.EjectTape(tapeDrive);

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
		}

		public bool IsInitialized => _isInitialized;

		private void ChangedAppState(int i)
		{
			_appState.AddTapeEntryToSave(_tapes.GetTapeEntryWasm(i));
		}

		public async ValueTask Init(InitializeWasm emulatorData)
		{
			_machine = new Machine();
			_printer = new PrinterWasm
			(
				emulatorData.AplFonts,
				emulatorData.PrErrorOff,
				emulatorData.PrErrorOn,
				_iJSUnmarshalledRuntime
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

			_tapes = new TapesWasm
			(
				emulatorData.TapeLO,
				emulatorData.TapeEO,
				emulatorData.SpinStop,
				emulatorData.SpinRight,
				emulatorData.SpinLeft,
				emulatorData.AplFonts,
				_iJSUnmarshalledRuntime,
				// Assembly w/Tape image resources
				emulatorData.GetSharedAssembly(),
				emulatorData.AllFonts,
				ChangedAppState
			);
			_machine.AddTapes(_tapes);

			var emulatorMouse = new EmulatorMouseWasm
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

		private bool displayedDuringStep;
		public async ValueTask<int> Step()
		{
			await Task.CompletedTask;
			if (!_isInitialized)
			{
				GameTime.Start();

				_isInitialized = true;
			}
			displayedDuringStep = false;

			var instructionsPerFrame = GameTime.Step();

			if (_printer.pr_op_code == 0) // Printer animation, skip CPU entirely
			{
				// https://stackoverflow.com/questions/32656443/timing-in-requestanimationframe
				for(int i = 0; i< instructionsPerFrame; i++)
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

			// Return -1 to have Javascript stop running
			return 0;
		}

		public void Key(byte key, JsKey systemKey)
		{
			_keyboard.keyboard(key, systemKey);
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

		public GameTime GameTime { get; } = new GameTime();

	}
}
