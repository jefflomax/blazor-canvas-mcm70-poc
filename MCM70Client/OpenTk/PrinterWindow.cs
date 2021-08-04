using System;
using System.ComponentModel;
using System.Diagnostics;
using MCMShared.Emulator;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MCM70Client.OpenTk
{
	public class PrinterWindow : GameWindow
	{
		private readonly Printer _printer;
		private readonly PrinterMouse _printerMouse;

		private readonly int _width;
		private readonly int _height;
		private const float Zero = 0f;
		private const int IntZero = 0;
		private const int IntOne = 1;

		private bool _resized;

		private int _texturePrinter; // was GLUint

		private float _mouseX;
		private float _mouseY;

		public System.Timers.Timer Timer;

		public PrinterWindow
		(
			GameWindowSettings printerWindowSettings,
			NativeWindowSettings printerNativeWindowSetting,
			Printer printer,
			PrinterMouse printerMouse
		) : base
			(
				printerWindowSettings,
				printerNativeWindowSetting
			)
		{
			_resized = false;
			Timer = null;
			_printer = printer;
			_printerMouse = printerMouse;
			_width = printerNativeWindowSetting.Size.X;
			_height = printerNativeWindowSetting.Size.Y;
		}

		public void ToggleVisibleAndTimer()
		{
			SetVisibleAndTimer(! IsVisible);
		}

		public void SetVisibleAndTimer(bool newState)
		{
			IsVisible = newState;
			Timer.Enabled = newState;
		}

		public void Event_OnLoad()
		{
			Timer = new System.Timers.Timer
			(
				TimeSpan.FromSeconds(1).TotalMilliseconds / 12.0
			)
			{
				AutoReset = true,
				Enabled = false
			};

			OnLoad();
		}
		public void Event_Resize(ResizeEventArgs e)
		{
			OnResize(e);
		}
		public void Event_OnUpdateFrame(FrameEventArgs args)
		{
			OnUpdateFrame(args);
		}
		public void Event_OnRenderFrame(FrameEventArgs args)
		{
			OnRenderFrame(args);
		}
		public void Event_OnUnload()
		{
			if (Timer!=null)
			{
				Timer.Dispose();
			}
			OnUnload();
		}

		protected override void OnLoad()
		{
			Title = "MCM/70 Printer";
			GL.Viewport(IntZero, IntZero, _width, _height);
			GL.Enable(EnableCap.Texture2D);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0.0, _width, _height, 0.0, 0.0, 100.0);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.ClearColor(Zero, Zero, Zero, Zero);
			GL.ClearDepth(Zero);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.GenTextures(1, out _texturePrinter);

			base.OnLoad();
		}

		protected override void OnMouseMove(MouseMoveEventArgs e)
		{
			_mouseX = e.X;
			_mouseY = e.Y;

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			var scaledX = _mouseX * ((float)_width / (float)Size.X);
			var scaledY = _mouseY * ((float)_height / (float)Size.Y);

			_printerMouse.MouseClick
			(
				e.Button == MouseButton.Button1 || e.Button == MouseButton.Left,
				e.Action == InputAction.Release,
				scaledX,
				scaledY
			);
			base.OnMouseUp(e);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			if (_printer.pr_op_code != 0)
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
				_printer.RunPrinter(_printer.RenderRunPrinterOut0AData, isAnimation:false);
			}

			base.OnUpdateFrame(args);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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

			GL.BindTexture(TextureTarget.Texture2D, _texturePrinter);
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
				_printer._printerWindow
			);

			SwapBuffers();
			base.OnRenderFrame(args);
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			// Because OpenTK is handling 2 windows, doing this
			// in Resize affects both windows
			//  GL.Viewport(0, 0, e.Width, e.Height);
			_resized = true;

			base.OnResize(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// User click X (Close) on printer, cancel the request
			// hide the window, and disconnect the printer
			_printer.SetPrinterConnected(false);
			SetVisibleAndTimer(false);
			e.Cancel = true;
		}

		protected override void OnClosed()
		{
			SetVisibleAndTimer(false);
		}

		protected override void OnUnload()
		{
			GL.DeleteTextures(1, ref _texturePrinter);
			base.OnUnload();
		}

	}
}
