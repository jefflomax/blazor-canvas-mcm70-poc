using System;
using System.Diagnostics;
using MCMShared.Emulator;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using System.Threading.Tasks;
using System.Reflection;
using MCMData;
using MCM70Client.Emulator;
using MCM70Client.Emulator.NotOriginal;
using MCM70Client.OpenTk;

/*************************************************************************************************

				 MCM/70 Emulator

Copyright (c) 2019--, Zbigniew Stachniak

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
**************************************************************************************************/


/*************************************************************************************************
.NET Core / C# / OpenTK port Copyright (c) 2021 Jeff Lomax
Gratefully acknowledging original work by Zbigniew Stachniak and assistance by OpenTK
community Julius Häger (NogginBops)
All rights identical to those specified by Zbigniew Stachniak
**************************************************************************************************/

namespace MCM70Client
{
	// http://vintagecomputer.ca/the-micro-computer-machines-mcm-70-the-canadian-holy-grail-of-computing-history/

	// https://dotnet.microsoft.com/download

	// Inventing the PC, the MCM/70 story, if you like knowing about computing
	// history, BUY this book!
	// https://www.amazon.com/gp/product/B00CS5BR8O/

	// IF you want to use OpenTK 4+ without compatiblity mode then use shaders and buffers
	// https://github.com/opentk/opentk
	// https://github.com/opentk/LearnOpenTK/tree/master/Chapter1
	// https://neokabuto.blogspot.com/p/tutorials.html
	// https://opentk.net/learn/chapter1/1-creating-a-window.html
	// https://docs.microsoft.com/en-us/dotnet/api/opentk.graphics.es20.gl.texparameter?view=xamarin-ios-sdk-12
	// https://github.com/simh/simh

	/*
		TODO:
		Timing
			Animation timings
		Capture printer output to text that can be used in NARS, etc. ?
			Begun in Printer::StorePage, shift-click printer icon
		Complete live machine language monitor
		Logger
		Non-US Keyboard Layout
		OpenTK additions (menu), external GameWindow interface
	*/

	public class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("MCM/70 Emulator" + Environment.NewLine);
			Console.WriteLine("Copyright (c) 2019, Zbigniew Stachniak");
			Console.WriteLine("8008 Emulator (c) Mike Willegal" + Environment.NewLine);
			Console.WriteLine("C# / .NET Core 3.1 / OpenTK 4.5 re-platform Copyright (c) 2021 Jeff Lomax" + Environment.NewLine);
			Console.WriteLine("Press START (TAB), then RETURN when you see MCM/APL    Read as MCM Reduces (or compresses) APL");

			var config = new Config(args);

			var runner = new Runner(config);
			runner.Run();
		}
	}

	public delegate void TogglePrinterWindow();

	public class Runner
	{
		private static bool _timerElapsed;
		private static bool _printerWindowRunLoop;
		private PrinterWindow _printerWindow;
		private System.Threading.Mutex _mutex;
		private readonly Config _config;

		private const int EmulatorWidth = 932; // emulator window's width
		private const int EmulatorHeight = 722;

		private readonly Stopwatch _watchRender;
		private readonly Stopwatch _watchUpdate;

		public Runner(Config config)
		{
			_config = config;
			_printerWindow = null;
			_printerWindowRunLoop = false;
			_mutex = null;
			_watchRender = new Stopwatch();
			_watchUpdate = new Stopwatch();
		}

		public void Run()
		{
			_timerElapsed = false;
			var emulatorData = new InitializeDotNet();
			emulatorData.SetAssembly
			(
				this.GetType().Assembly,
				new AplFont().GetType().Assembly,
				new McmData().GetType().Assembly
			);
			emulatorData.InitAll();

			var machine = new Machine(); // Machine & CPU both "need" ROMs

			var printer = new Printer
			(
				emulatorData.PrinterWin,
				emulatorData.AplFonts,
				emulatorData.PrErrorOff,
				emulatorData.PrErrorOn
			);
			machine.AddPrinter(printer);
			var printerMouse = new PrinterMouse(printer);

			var display = _config.AllesLookensgepeepers
				? new DisplayLights
				(
					emulatorData.Panel,
					machine.Memory,
					EmulatorWidth,
					EmulatorHeight,
					emulatorData.AplFonts
				)
				: new Display
				(
					emulatorData.Panel,
					machine.Memory,
					EmulatorWidth,
					EmulatorHeight,
					emulatorData.AplFonts
				);
			machine.AddDisplay(display);
			display.Message();

			var keyboard = new Keyboard();
			machine.AddKeyboard(keyboard);

			var tapes = new TapesDotNet
			(
				emulatorData.TapeLO,
				emulatorData.TapeEO,
				emulatorData.SpinStop,
				emulatorData.SpinRight,
				emulatorData.SpinLeft,
				emulatorData.AplFonts
			);
			machine.AddTapes(tapes);

			var emulatorMouse = new EmulatorMouse
			(
				emulatorData.TapeLC,
				emulatorData.TapeEC,
				emulatorData.TapeLO,
				emulatorData.TapeEO
			);
			machine.AddEmulatorMouse(emulatorMouse);

			var cpu = new Cpu(_config.ShowDisassembly);
			machine.AddCpu(cpu,_config.ShowOpCodeListing);

			cpu.ResetCpu();

			cpu.InitMemory
			(
				emulatorData.Rom6k,
				emulatorData.Rom
			);

			var nwsPrinter = new NativeWindowSettings
			{
				// CRITICAL
				// Setting a compatibility context will re-enable the GL.Begin/End APIs
				Profile = ContextProfile.Compatability,
				Size = new Vector2i(Printer.p_width, Printer.p_height),
				StartVisible = false
			};
			var gwsPrinter = new GameWindowSettings
			{
				UpdateFrequency = 1,
				RenderFrequency = 1
			};

			var nwsEmulator = new NativeWindowSettings
			{
				// CRITICAL
				// Setting a compatibility context will re-enable the GL.Begin/End APIs
				Profile = ContextProfile.Compatability,
				Size = new Vector2i(EmulatorWidth, EmulatorHeight),
				StartVisible = true
			};
			var gwsEmulator = new GameWindowSettings
			{
				UpdateFrequency = _config.UpdateFrequency, // We pass a multiplier of 132 so this is 66,000
				RenderFrequency = 10
			};


			// Typically, OpenTK would just call GameWindow.Run(), but if you
			// do that you won't be able to manage a 2nd window.  Further, all
			// windows must be created on the main thread.
			// To get around this, create your own Run loop and assure you
			// call MakeCurrent on the window before interacting with it.

			// In this case, OpenTK is handling the timing, but since doing that
			// across 2 windows (emulator, printer) where one isn't important is 
			// difficult, we'll put the printer window in it's own thread

			_mutex = new System.Threading.Mutex();

			_printerWindow = new PrinterWindow
			(
				gwsPrinter,
				nwsPrinter,
				printer,
				printerMouse
			);
			_printerWindowRunLoop = true;

			var printerWindowTask = new System.Threading.Thread(() => {

				var frameEventArgs = new FrameEventArgs();

				while (_printerWindowRunLoop)
				{
					if(_printerWindow.IsExiting)
					{
						_printerWindow.ProcessEvents();
					}
					else if (_printerWindow.IsVisible)
					{
						_mutex.WaitOne();
						if (!_printerWindow.Context.IsCurrent && !_printerWindow.IsExiting )
						{
							_printerWindow.MakeCurrent();
						}
						_printerWindow.ProcessEvents();

						bool animation = false;
						if (printer.RenderPending)
						{
							animation = printer.pr_status !=0;
							_printerWindow.Event_OnUpdateFrame(frameEventArgs);
						}

						if (_timerElapsed || animation)
						{
							_printerWindow.Event_OnRenderFrame(frameEventArgs);
							if(_timerElapsed)
							{
								_timerElapsed = false;
							}
						}

						if (printer.Redisplay)
						{
							_printerWindow.Timer.Enabled = false; // "animation" requested, suspend timer
						}

						_mutex.ReleaseMutex();

						if (printer.Redisplay)
						{
							machine.Sleep1(20); // Original emulator waited 2/10th of a second
							printer.Redisplay = false;
							_printerWindow.Timer.Enabled = true;
						}
					}
				}

				// Probably move these to main window loop
				if(! _printerWindow.IsExiting)
				{
					_mutex.WaitOne();
					_printerWindow.MakeCurrent();
					_printerWindow.Event_OnUnload();
					_mutex.ReleaseMutex();
				}
				else
				{
					_printerWindow.ProcessEvents();
					_printerWindow.Event_OnUnload();
					_printerWindow.ProcessEvents();
				}
			});

			using (var game = new Game
				(
					gwsEmulator,
					nwsEmulator,
					machine,
					display,
					cpu,
					keyboard,
					printer,
					emulatorData,
					TogglePrinter,
					_watchUpdate,
					_watchRender,
					132
				)
			)
			{
				game.Context.MakeCurrent();
				game.Event_OnLoad();

				game.Event_Resize(new ResizeEventArgs(game.Size));

				_printerWindow.Context.MakeCurrent();
				_printerWindow.Event_OnLoad();
				_printerWindow.Event_Resize(new ResizeEventArgs(_printerWindow.Size));

				// Timer for printer window limits frame rate
				_printerWindow.Timer.Elapsed += (sender, e) =>
					PrinterWindowTimer_Elapsed(sender, e);

				printerWindowTask.Start();

				// Would watching FocusChanged help with tracking the current context ?

				_watchRender.Start();
				_watchUpdate.Start();

				_timerElapsed = false;

				while (! game.IsExiting) // Exists and IsExiting are protected
				{
					if (_printerWindow.IsVisible && printer.RenderPending)
					{
						continue;  // Give Printer precedence
					}

					_mutex.WaitOne();

					// Context.MakeCurrent() is a very expensive, and will drop the
					// emulator speed dramatically if called without checking if needed
					if (! game.Context.IsCurrent && ! game.IsExiting)
					{
						game.Context.MakeCurrent();
					}

					game.ProcessEvents();
					if (!game.IsExiting)
					{
						game.DispatchUpdateFrame();

						game.DispatchRenderFrame();
					}
					else
					{
						_timerElapsed = true;
					}

					_mutex.ReleaseMutex();

					// watch out for swapbuffers
				}

			
				_printerWindowRunLoop = false;
				_printerWindow.SetVisibleAndTimer(false);
				_timerElapsed = false;

				_mutex.WaitOne();
				game.MakeCurrent();
				game.Event_OnUnload();
				_mutex.ReleaseMutex();
			}
		}

		public void TogglePrinter()
		{
			_printerWindow.ToggleVisibleAndTimer();
		}

		private static void PrinterWindowTimer_Elapsed
		(
			object sender,
			System.Timers.ElapsedEventArgs e
		)
		{
			_timerElapsed = true;
		}
	}

}
