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

		public void Set(byte b)
		{
			static KeyboardKeyEventArgs Key(Keys k, int scanCode)
			{
				return new KeyboardKeyEventArgs(k, scanCode, modifiers: 0, isRepeat: false);
			}
			switch (b)
			{
				case (byte)' ':
					_keyCode = Key(Keys.Space,0);
					break;
				case (byte)'\t':
					_keyCode = Key(Keys.Tab,15);
					break;
				case (byte)'\b':
					_keyCode = Key(Keys.Backspace,0);
					break;
			}
		}
		public void Set(KeyboardKeyEventArgs ke)
		{
			_keyCode = ke;
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
