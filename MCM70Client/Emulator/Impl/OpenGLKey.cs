using MCMShared.Interfaces;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MCM70Client.Emulator.Impl
{
	public class OpenGLKey : IsKey
	{
		private KeyboardKeyEventArgs _keyCode;
		public OpenGLKey()
		{
			Clear();
		}

		public void Clear()
		{
			//_keyCode = null;
		}

		public void Set(KeyboardKeyEventArgs ke)
		{
			_keyCode = ke;
#if false
			_ctrl = ctrl;
			switch (code)
			{
				case "Space":
					_keyCode = JSKeyCode.Space;
					break;
				case "Backspace":
					_keyCode = JSKeyCode.BackSpace;
					break;
				case "F1":
					_keyCode = JSKeyCode.F1;
					break;
				case "F2":
					_keyCode = JSKeyCode.F2;
					break;
				case "Tab":
					_keyCode = JSKeyCode.TAB;
					break;
			}
#endif
		}

		public bool IsF1()
		{
			return _keyCode.Key == Keys.F1;
		}

		public bool IsF2()
		{
			return _keyCode.Key == Keys.F2;
		}

		public bool IsTab()
		{
			return _keyCode.Key == Keys.Tab;
		}

		public bool IsSpace()
		{
			return _keyCode.Key == Keys.Space;
		}

		public bool IsBackspace()
		{
			return _keyCode.Key == Keys.Backspace;
		}

		public bool HasCtrlModifier()
		{
			return _keyCode.Modifiers.HasFlag(KeyModifiers.Control);
		}
	}
}
