using System;
using System.ComponentModel;
using System.Diagnostics;
using MCMShared.Emulator;
using MCM70Client.Emulator.Impl;
using MCM70Client.Extensions;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace MCM70Client.OpenTk
{
	public class Game : GameWindowPrivate
	{
		private readonly int _width;
		private readonly int _height;
		private int _textureMain; // was GLUint
		private const float Zero = 0f;
		private const int IntZero = 0;
		private const int IntOne = 1;

		private readonly byte[] _panel;
		private readonly Machine _machine;
		private readonly Display _display;
		private readonly Printer _printer;
		private readonly Cpu _cpu;
		private readonly Keyboard _keyboard;
		private readonly TogglePrinterWindow _togglePrinterWindow;

		private static readonly OpenGLKey _openGLKey = new OpenGLKey();

		private float _mouseX;
		private float _mouseY;

		private bool _resized;

		public struct KeyMapEntry
		{
			public KeyMapEntry(Keys k, char c)
			{
				Key = k;
				Char = (byte)c;
			}
			public Keys Key;
			public byte Char;
		}

		public KeyMapEntry[] KeyMap =
		{
			new KeyMapEntry(Keys.A, 'A'),
			new KeyMapEntry(Keys.B, 'B'),
			new KeyMapEntry(Keys.C, 'C'),
			new KeyMapEntry(Keys.D, 'D'),
			new KeyMapEntry(Keys.E, 'E'),
			new KeyMapEntry(Keys.F, 'F'),
			new KeyMapEntry(Keys.G, 'G'),
			new KeyMapEntry(Keys.H, 'H'),
			new KeyMapEntry(Keys.I, 'I'),
			new KeyMapEntry(Keys.J, 'J'),
			new KeyMapEntry(Keys.K, 'K'), // '
			new KeyMapEntry(Keys.L, 'L'), // QUAD
			new KeyMapEntry(Keys.M, 'M'),
			new KeyMapEntry(Keys.N, 'N'),
			new KeyMapEntry(Keys.O, 'O'),
			new KeyMapEntry(Keys.P, 'P'),
			new KeyMapEntry(Keys.Q, 'Q'),
			new KeyMapEntry(Keys.R, 'R'),
			new KeyMapEntry(Keys.S, 'S'),
			new KeyMapEntry(Keys.T, 'T'),
			new KeyMapEntry(Keys.U, 'U'),
			new KeyMapEntry(Keys.V, 'V'),
			new KeyMapEntry(Keys.W, 'W'),
			new KeyMapEntry(Keys.X, 'X'),
			new KeyMapEntry(Keys.Y, 'Y'),
			new KeyMapEntry(Keys.Z, 'Z')
		};

		public Game
		(
			GameWindowSettings gameWindowSettings,
			NativeWindowSettings nativeWindowSettings,
			Machine machine,
			Display display,
			Cpu cpu,
			Keyboard keyboard,
			Printer printer,
			Initialize emulatorData,
			TogglePrinterWindow toggleWindow,
			Stopwatch watchUpdate,
			Stopwatch watchRender,
			int updateFrequencyMultiplier
		) : base
			(
				gameWindowSettings,
				nativeWindowSettings,
				watchUpdate,
				watchRender,
				updateFrequencyMultiplier
			)
		{
			_togglePrinterWindow = toggleWindow;
			_resized = false;
			_width = nativeWindowSettings.Size.X;
			_height = nativeWindowSettings.Size.Y;

			_panel = emulatorData.Panel;

			_machine = machine;
			_display = display;
			_cpu = cpu;
			_printer = printer;
			_keyboard = keyboard;
		}

		public void Event_OnUnload()
		{
			OnUnload();
		}

		protected override void OnMouseMove(MouseMoveEventArgs e)
		{
			_mouseX = e.X;
			_mouseY = e.Y;

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			var scaledX = (int)(_mouseX * ((float)_width / (float)Size.X));
			var scaledY = (int)(_mouseY * ((float)_height / (float)Size.Y));

			if( _machine.EmulatorMouse.IsKeyboardClick(scaledX, scaledY, out var ch))
			{
				_keyboard.keyboard(ch, _openGLKey);
			}
			else
			{
				var action = _machine.EmulatorMouse.MouseClick
				(
					ButtonType(e.Button),
					e.Action == InputAction.Release,
					(e.Modifiers & KeyModifiers.Shift) != 0,
					scaledX,
					scaledY
				);

				if( action == MouseAction.PrinterOn || action == MouseAction.PrinterOff )
				{
					_togglePrinterWindow();
				}
			}

			base.OnMouseUp(e);
		}

		private static MouseButtonSel ButtonType(MouseButton mb)
		{
			if (mb == MouseButton.Button1 || mb == MouseButton.Left)
				return MouseButtonSel.Left;

			if (mb == MouseButton.Button2 || mb == MouseButton.Right)
			{
				return MouseButtonSel.Right;
			}

			return MouseButtonSel.Unknown;
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			// If UpdateFrequency is zero, GameWindowPrivate has a copy of GameWindow's private
			// DispatchUpdate, and will run as fast as it can maintaining the frame rate
			// The UpdateFrequency is capped at 500, but we need it much higher, so we pass
			// a multiplier of 132 so this is 66,000

			if (_printer.pr_op_code == 0) // Printer animation, skip CPU entirely
			{
				if (_machine.RefreshDisplayCounter >= 56 || _machine.InstrCount >= 1000)
				{
					_machine.RefreshDisplayCounter = 0;
					_machine.InstrCount = 0;
					_display.refresh_SS();
				}

				// When we encounter a printer operation, we stop running the 
				// CPU, this resets it
				RunCpu = true;

				_cpu.RunCpu(); // Process one instruction
				if (_machine.RefreshDisplayCounter != 0)
				{
					_machine.InstrCount++;
				}

				// pr_op_code is only modified in printer.cs, it doesn't seem
				// likely this can happen.
				if (_printer.pr_op_code != 0)
				{
					RunCpu = false;
				}
			}

			//base.OnUpdateFrame(args);
		}

		// OnTextInput has Unicode char, but didn't come reliably before OnKeyDown

		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			//http://archive.gamedev.net/archive/reference/articles/article842.html
			if (e.Key == Keys.Escape)
			{
				OnClosing(new CancelEventArgs());
				Close();
			}
			else
			{
				_openGLKey.Set(e);
				// This could try to map ASCII to APL
				KeyboardState keyboardState = KeyboardState.GetSnapshot();
				var ascii = KeyToAscii(keyboardState, e.Key, e.Modifiers);
				if (!e.IsRepeat && ascii != 0xFF)
				{
					_keyboard.keyboard(ascii, _openGLKey);
				}
			}

			base.OnKeyDown(e);
		}

		private byte KeyToAscii(KeyboardState keyboardState, Keys key, KeyModifiers m)
		{
			// Map an array once
			// UN-shifted keys
			if (!m.HasFlag(KeyModifiers.Shift))
			{
				if (key >= Keys.A && key <= Keys.Z)
				{
					return (byte)((int)key | 0x20); // Lower-case
				}

				if (key >= Keys.D0 && key <= Keys.D9)
				{
					return (byte)key;
				}

				switch (key)
				{
					case Keys.Space:
						return 0x20;
					case Keys.Enter:
						return 0x0D;
					case Keys.Minus:
						return (byte)key;
					case Keys.Equal:
						return (byte)key;
					case Keys.Comma:
						return (byte)key;
					case Keys.Period:
						return (byte)key;
					case Keys.Slash:
						return (byte)key;
					case Keys.Apostrophe:
						return (byte)0x27; // ' is ]
					case Keys.LeftBracket:
						return 0x5B;
					case Keys.Semicolon:
						return 0x3B; // ; is [
					case Keys.Backslash:
						return (byte)'?'; // Map \ not on APL Keyboard
					case Keys.Tab:
						return 0x09; //

					case Keys.Backspace:
						return 0x08;

					case Keys.F1:
					case Keys.F2:
						return 0xFE;
				}
			}

			else // HasFlag(KeyModifiers.Shift)
			{
				// This is stupid
				foreach (var km in KeyMap)
				{
					if (keyboardState.IsKeyDown(km.Key))
					{
						return km.Char;
					}
				}

				// This isn't going to work either
				// Just masking D0..D9 with $EF only
				// works for a few
				switch (key)
				{
					case Keys.D0:
						return (byte)')';
					case Keys.D1:
						return (byte)'!';
					case Keys.D2:
						return (byte)'@';
					case Keys.D3:
						return (byte)'#';
					case Keys.D4:
						return (byte)'$';
					case Keys.D5:
						return (byte)'%';
					case Keys.D6:
						return (byte)'^';
					case Keys.D7:
						return (byte)'&';
					case Keys.D8:
						return (byte)'*';
					case Keys.D9:
						return (byte)'(';
					case Keys.Minus:
						return (byte)'_';
					case Keys.Equal:
						return (byte)'+';
					case Keys.Comma:
						return (byte)'<';
					case Keys.Period:
						return (byte)'>';
					case Keys.Slash:
						return (byte)'?';
					case Keys.Apostrophe:
						return (byte)'"'; // 
					case Keys.LeftBracket:
						return (byte)0x7B; // SHIFT [ '{'
					case Keys.Semicolon:
						return 0x3A; // : is (
					case Keys.Backslash:
						return (byte)'M'; // Map | not on APL Keyboard
										  // TODO: What should we do with shift TAB?
				}
			}

			if (!key.IsIn(LeftShift, RightShift, LeftControl, RightControl))
			{
				Console.WriteLine($"Unmapped Key {key.ToString()}");
			}
			return 0xFF;
		}

		// OnTextInput(TextInputEventArgs e) gives us UNICODE but not modifiers

		protected override void OnLoad()
		{
			Title = "MCM/70 Emulator";

#if false
			// This worked on the 4.0 API
			_vertexBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
			GL.BufferData
			(
				BufferTarget.ArrayBuffer,
				_vertices.Length * sizeof(float),
				_vertices,
				BufferUsageHint.StaticDraw
			);

			_vertexArrayObject = GL.GenVertexArray();
			GL.BindVertexArray(_vertexArrayObject);
			GL.VertexAttribPointer
			(
				0,
				3,
				VertexAttribPointerType.Float,
				false,
				3 * sizeof(float),
				0
			);
			GL.EnableVertexAttribArray(0);
			/*
			_shader = new OpenGL.Shaders.Shader
			(
				@"Shaders\shader.vert",
				@"Shaders\shader.frag"
			);

			_shader.Use();
			*/
#endif

			// init_all.c
			GL.Viewport(IntZero, IntZero, _width, _height);
			GL.Enable(EnableCap.Texture2D);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0.0, _width, _height, 0.0, 0.0, 100.0);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.ClearColor(Zero, Zero, Zero, Zero);
			GL.ClearDepth(Zero);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// mcm.c 
			// Main window's texture
			GL.GenTextures(1, out _textureMain);

			base.OnLoad();
		}

#if false
		private Shader _shader;
		private readonly float[] _vertices =
		{
			-0.5f, -0.5f, 0.0f, // Bottom-left vertex
			0.5f, -0.5f, 0.0f, // Bottom-right vertex
			0.0f,  0.5f, 0.0f  // Top vertex
		};

		private readonly float[] _quads =
		{
			0.0f, 0.0f, 0.0f, // Bottom-left vertex
			0.5f, -0.5f, 0.0f, // Bottom-right vertex
			0.0f,  0.5f, 0.0f  // Top vertex
		};

		private int _vertexBufferObject;
		private int _vertexArrayObject;
#endif

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			// OpenGL calls this per the requested frame rate

			// Clears using color set in OnLoad
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

#if false
			// Worked on the 4.0 API
			GL.BindVertexArray(_vertexArrayObject);
			//GL.Color3(1.0, 0.0, 0.0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			//GL.DrawArrays(PrimitiveType.Quads,0,3);
			//GL.DrawElements(BeginMode.Quads,4,DrawElementsType.)
			//GL.Flush();
#endif

			if (_resized)
			{
				_resized = false;
				GL.Viewport(0, 0, Size.X, Size.Y);
			}

			GL.MatrixMode(MatrixMode.Modelview);
			GL.Begin(PrimitiveType.Quads);

			GL.TexCoord2(IntZero, IntZero);
			GL.Vertex2(IntZero, IntZero);

			GL.TexCoord2(IntZero, IntOne);
			GL.Vertex2(IntZero, _height);

			GL.TexCoord2(IntOne, IntOne);
			GL.Vertex2(_width, _height);

			GL.TexCoord2(IntOne, IntZero);
			GL.Vertex2(_width, IntZero);

			GL.End();


			GL.BindTexture(TextureTarget.Texture2D, _textureMain);
			// https://github.com/opentk/LearnOpenTK/blob/master/Common/Texture.cs
			GL.TexParameter
			(
				TextureTarget.Texture2D,
				TextureParameterName.TextureMagFilter,
				(int)TextureMagFilter.Linear
			);
			GL.TexParameter
			(
				TextureTarget.Texture2D,
				TextureParameterName.TextureMinFilter,
				(int)TextureMinFilter.Linear
			);


			GL.TexImage2D
			(
				TextureTarget.Texture2D,
				0,
				PixelInternalFormat.Rgb,
				_width,
				_height,
				0,
				PixelFormat.Rgb,
				PixelType.UnsignedByte,
				_panel
			);

			SwapBuffers();
			base.OnRenderFrame(args);
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			// Because OpenTK is handling 2 windows, doing this
			// in Resize affects both windows
			_resized = true;
			//GL.Viewport(0, 0, e.Width, e.Height);

			base.OnResize(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
		}

		protected override void OnUnload()
		{
			GL.DeleteTextures(1, ref _textureMain);
			base.OnUnload();
		}

	}
}
