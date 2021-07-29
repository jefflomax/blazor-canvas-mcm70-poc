using MCMShared.Interfaces;

namespace BlazorCanvas.Emulator.Impl
{
	public class JsKey : IsKey
	{
		private JSKeyCode _keyCode;
		private bool _ctrl;
		public JsKey()
		{
			Clear();
		}

		public void Clear()
		{
			_keyCode = JSKeyCode.None;
		}

		public void Set(string code, bool ctrl)
		{
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
		}

		public bool IsF1()
		{
			return _keyCode == JSKeyCode.F1;
		}

		public bool IsF2()
		{
			return _keyCode == JSKeyCode.F2;
		}

		public bool IsTab()
		{
			return _keyCode == JSKeyCode.TAB;
		}

		public bool IsSpace()
		{
			return _keyCode == JSKeyCode.Space;
		}

		public bool IsBackspace()
		{
			return _keyCode == JSKeyCode.BackSpace;
		}

		public bool HasCtrlModifier()
		{
			return _ctrl;
		}
	}
}
