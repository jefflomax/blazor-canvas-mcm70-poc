using System;


namespace MCMShared.Emulator
{
	/*************************************************************************************
	 *                              KEYBOARD  & MOUSE                                    *
	 * ***********************************************************************************/

	/*===============================  KEYBOARD ===========================================
	  GLUT keyboard callback + other routines to handle keyboard emulation.
	  Note: MCM/70's OS expects a "bouncing" keyboard which OS then debounces.            */

	// KBD Queue
	public class Node
	{
		public int Data;
		public int Repetitions;
		public Node Next;
	};


	public class Keyboard
	{
		// Two variables to store address of front and rear of keyboard queue 
		// Could change to .NET LinkedList -- but it's doubly linked
		private Node _front;
		private Node _rear;
		private Machine _machine;

		public bool KeyRequested;	// flag to determine whether keyboard input is requested

		public Keyboard()
		{
			_machine = null;
			_front = null;
			_rear = null;
			KeyRequested = false;
		}

		public void SetMachine(Machine machine)
		{
			_machine = machine;
		}

		//------------------------------------------------------------------------------------
		// addQ: enqueue a byte in keyboard's queue
		//------------------------------------------------------------------------------------
		private void AddQ(int x, int repetitions)
		{
			var temp = new Node
			{
				Data = x,
				Repetitions = repetitions,
				Next = null
			};

			if(_front == null && _rear == null)
			{
				_front = _rear = temp;
				return;
			}
			_rear.Next = temp;
			_rear = temp;
		}

		public void ClearIfAllZero()
		{
			if(_front == null || _front.Data != 0 )
			{
				return;
			}
			while(_front.Data == 0 && _front.Next != null )
			{
				_front = _front.Next;
			}
			if( _front.Data == 0)
			{
				_rear = null;
				_front = null;
			}
		}


		//-------------------------------------------------------------------------------------
		// delQ: deque and return a byte from the keyboard queue
		//-------------------------------------------------------------------------------------
		private int DelQ()
		{
			if (_front == null)
			{
				return 0x00; // queue empty -- return 0
			}

			if (_front.Repetitions != 0)
			{
				_front.Repetitions -= 1;
				return _front.Data;
			}

			if(_front == _rear)
			{
				_front = _rear = null;
			}
			else
			{
				_front = _front.Next;
				_front.Repetitions -= 1;
				return _front.Data;
			}

			return 0x00;
		}

		//------------------------------------------------------------
		// keyboard: GLUT keyboard handler 
		//------------------------------------------------------------

		//public void keyboard(byte key, int x, int y)
		public void keyboard
		(
			//OpenTK.Windowing.GraphicsLibraryFramework.Keys openGLKey,
			byte ascii
			//KeyModifiers modifiers
		)
		{
			int key = ascii;

			if (!KeyRequested)
			{
				// IO has not requested a keyboard press
				// should the queue be emptied as well to get rid
				// of extra presses??
				// computer is on and key press has been requested then process it
				Console.WriteLine("Not ready for key yet");
				return;
			}

			KeyRequested=false;     // key will be supplied so mark request as "served"

			ClearIfAllZero();

			AddQ(key, repetitions: 100);
			// end with a few 0s to emulate key up
			AddQ(0, repetitions: 5);

#if false
			int key = ascii;

			if( ascii == 0)
			{
				return;
			}

			if (! _machine.Power)
			{
				if (openGLKey == Keys.F2)
				{
					_machine.Power = true; // simulates restouring power
					Console.WriteLine("Power restore simulated, press START (TAB)");
				}
				return;			// no power, do nothing
			}

			// if TAB is pressed for the first time, then start computer
			if ((openGLKey == Keys.Tab) && (! _machine.McmOn))
			{
				_machine.McmOn=true; // power CPU

				_machine.Display.InitAllesLookensgepeepers();

				Console.WriteLine("MCM/70 powered on, press RETURN when you see MCM/APL");
				return;
			}

			if (! _machine.McmOn)
				return;				// computer is not turned on, so do nothing

			if (! KeyRequested)
			{
				// IO has not requested a keyboard press
				// should the queue be emptied as well to get rid
				// of extra presses??
				// computer is on and key press has been requested then process it
				return;
			}

			KeyRequested=false;		// key will be supplied so mark request as "served"

			/* process CTRL key press;
				note: Glut recognizes CTRL as a key modifier and not as a "real" key; so, there is no way to
					detect CTRL key press alone withing the keyboard handler; the emulator simulates CTRL
					key press via a combination key press: CTRL + any alphanumeric key.    */

#if false
			int mod = glutGetModifiers();

			if (mod & GLUT_ACTIVE_CTRL)
				switch (key)
				{
				case 0: // CTRL + space = insert char; note CTRL+ space gives ascii code 0
				key=2;
				break;    // key code 2 is selected arbitrarily
				case 8: // CTRL + BSP = delete char; note CTRL+ space gives ascii code 8 the same as for BSP
				key=127;
				break;  // key-code 127 is selected to match DEL key on a standard keyboard  
				default: // CTRL and any alphanumeric key are pressed 
				key =3;            // key code 3 is selected an an arbitrary way
				}
#endif
			if (modifiers.HasFlag(KeyModifiers.Control))
			{
				switch (openGLKey)
				{
				case Keys.Space: // CTRL + space = insert char; note CTRL+ space gives ascii code 0
					key=2;
					break;		// key code 2 is selected arbitrarily
				case Keys.Backspace: // CTRL + BSP = delete char; note CTRL+ space gives ascii code 8 the same as for BSP
					key=127;
					break;  // key-code 127 is selected to match DEL key on a standard keyboard  
				default: // CTRL and any alphanumeric key are pressed 
					key =3;		// key code 3 is selected an an arbitrary way
					break;
				}
			}

			if (openGLKey == Keys.F1)
			{
				_machine.Power = false;	// simulates power failure
				Console.WriteLine("Power failure simulated, press F2 to restore");
				return;
			}

			/* build queue of key bounces: keyboard routine uses at most 8 keyboard reads to identify a key
			   pressed plus 90 reads for keyboard debouncing; so, pushing 100 key values into the keyboard 
			   queue (followed by some 0s) is sufficient
			*/

			ClearIfAllZero();

			AddQ(key, repetitions:100);
			// end with a few 0s to emulate key up
			AddQ(0,repetitions:5);
#endif
		}


		/*-------------------------------------------------------------------------------------
		   get_row: returns row of a currently pressed key.
		   input:  column number col
		   output: row number of a key, if such a key has been pressed in specified column;
		--------------------------------------------------------------------------------------*/
		public byte GetRow(byte col)
		{
			var key = DelQ();

			byte row = 0x00;
			switch (key)
			{
			case 2:
				if (col == 0x08)
					row=0x60;
				break;   // insert char
			case 3:
				row=0x40;
				break;   // CONTROL is pressed
			case 8:
				if (col == 0x01)
					row=0x02;
				break;   // char BSP
			case 10:
				if (col == 0x02)
					row=0x01;
				break;   // char RET (or EXEC)
			case 13:
				if (col == 0x02)
					row=0x01;
				break;   // char RET (or EXEC)
			case 32:
				if (col == 0x08)
					row=0x20;
				break;   // char space 
			case 33:
				if (col == 0x40)
					row=0xA0;
				break;   // APL char .. 
			case 34:
				if (col == 0x04)
					row=0x81;
				break;   // APL char )
			case 35:
				if (col == 0x01)
					row=0xA0;
				break;   // APL char <
			case 36:
				if (col == 0x80)
					row=0x90;
				break;   // APL char =<
			case 37:
				if (col == 0x01)
					row=0x90;
				break;   // APL char =
			case 38:
				if (col == 0x01)
					row=0x88;
				break;   // APL char >
			case 39:
				if (col == 0x04)
					row=0x01;
				break;   // APL char ]
			case 40:
				if (col == 0x01)
					row=0x84;
				break;   // APL char v
			case 41:
				if (col == 0x80)
					row=0x82;
				break;   // APL char v
			case 42:
				if (col == 0x80)
					row=0x84;
				break;   // APL char logical &
			case 43:
				if (col == 0x80)
					row=0x81;
				break;   // APL char div
			case 44:
				if (col == 0x08)
					row=0x02;
				break;   // ,
			case 45:
				if (col == 0x01)
					row=0x01;
				break;   // +
			case 46:
				if (col == 0x10)
					row=0x01;
				break;   // .
			case 47:
				if (col == 0x08)
					row=0x01;
				break;   // /
			case 48:
				if (col == 0x80)
					row=0x02;
				break;   // char 0
			case 49:
				if (col == 0x40)
					row=0x20;
				break;    // char 1
			case 50:
				if (col == 0x20)
					row=0x20;
				break;    // char 2
			case 51:
				if (col == 0x01)
					row=0x20;
				break;    // char 3
			case 52:
				if (col == 0x80)
					row=0x10;
				break;    // char 4
			case 53:
				if (col == 0x01)
					row=0x10;
				break;    // char 5
			case 54:
				if (col == 0x80)
					row=0x08;
				break;    // char 6
			case 55:
				if (col == 0x01)
					row=0x08;
				break;    // char 7
			case 56:
				if (col == 0x80)
					row=0x04;
				break;    // char 8
			case 57:
				if (col == 0x01)
					row=0x04;
				break;    // char 9
			case 58:
				if (col == 0x20)
					row=0x81;
				break;    // APL char (
			case 59:
				if (col == 0x20)
					row=0x01;
				break;    // APL char [
			case 60:
				if (col == 0x08)
					row=0x82;
				break;    // ;
			case 61:
				if (col == 0x80)
					row=0x01;
				break;    // APL char x (times)
			case 62:
				if (col == 0x10)
					row=0x81;
				break;    // :
			case 63:
				if (col == 0x08)
					row=0x81;
				break;    // slash right
			case 64:
				if (col == 0x20)
					row=0xA0;
				break;    // APL char -
			case 65:
				if (col == 0x04)
					row=0xA0;
				break;    // APL char alpha
			case 66:
				if (col == 0x10)
					row=0x84;
				break;    // char reversed T
			case 67:
				if (col == 0x10)
					row=0x88;
				break;    // char intersection
			case 68:
				if (col == 0x04)
					row=0x90;
				break;   // char L
			case 69:
				if (col == 0x40)
					row=0x90;
				break;   // char membership
			case 70:
				if (col == 0x20)
					row=0x88;
				break;   // char _
			case 71:
				if (col == 0x04)
					row=0x88;
				break;   // char triangle down \/
			case 72:
				if (col == 0x20)
					row=0x84;
				break;   // char triangle up 
			case 73:
				if (col == 0x02)
					row=0x84;
				break;   // char iota
			case 74:
				if (col == 0x04)
					row=0x84;
				break;   // char o
			case 75:
				if (col == 0x20)
					row=0x82;
				break;   // char quote
			case 76:
				if (col == 0x04)
					row=0x82;
				break;   // char quad []
			case 77:
				if (col == 0x10)
					row=0x82;
				break;   // char |
			case 78:
				if (col == 0x08)
					row=0x84;
				break;   // char true small T
			case 79:
				if (col == 0x40)
					row=0x82;
				break;   // char big circ
			case 80:
				if (col == 0x02)
					row=0x82;
				break;   // char *
			case 81:
				if (col == 0x10)
					row=0xA0;
				break;   // char ?
			case 82:
				if (col == 0x02)
					row=0x90;
				break;   // char rho
			case 83:
				if (col == 0x20)
					row=0x90;
				break;   // char top left corner
			case 84:
				if (col == 0x40)
					row=0x88;
				break;   // char ~
			case 85:
				if (col == 0x40)
					row=0x84;
				break;   // char down arrow
			case 86:
				if (col == 0x08)
					row=0x88;
				break;   // char union
			case 87:
				if (col == 0x02)
					row=0xA0;
				break;   // char omega
			case 88:
				if (col == 0x08)
					row=0x90;
				break;   // char back inclusion
			case 89:
				if (col == 0x02)
					row=0x88;
				break;   // char up arrow
			case 90:
				if (col == 0x10)
					row=0x90;
				break;   // char inclusion
			case 91:
				if (col == 0x40)
					row=0x01;
				break;   // char <-

			case 94:
				if (col == 0x80)
					row=0x88;
				break;   // APL char >=
			case 95:
				if (col == 0x01)
					row=0x81;
				break;   // APL char -
			case 97:
				if (col == 0x04)
					row=0x20;
				break;    // char A
			case 98:
				if (col == 0x10)
					row=0x04;
				break;    // char B
			case 99:
				if (col == 0x10)
					row=0x08;
				break;    // char C
			case 100:
				if (col == 0x04)
					row=0x10;
				break;   // char D
			case 101:
				if (col == 0x40)
					row=0x10;
				break;   // char E
			case 102:
				if (col == 0x20)
					row=0x08;
				break;   // char F
			case 103:
				if (col == 0x04)
					row=0x08;
				break;   // char G
			case 104:
				if (col == 0x20)
					row=0x04;
				break;   // char H
			case 105:
				if (col == 0x02)
					row=0x04;
				break;   // char I
			case 106:
				if (col == 0x04)
					row=0x04;
				break;   // char J
			case 107:
				if (col == 0x20)
					row=0x02;
				break;   // char K
			case 108:
				if (col == 0x04)
					row=0x02;
				break;   // char L
			case 109:
				if (col == 0x10)
					row=0x02;
				break;   // char M
			case 110:
				if (col == 0x08)
					row=0x04;
				break;   // char N
			case 111:
				if (col == 0x40)
					row=0x02;
				break;   // char O
			case 112:
				if (col == 0x02)
					row=0x02;
				break;   // char P
			case 113:
				if (col == 0x10)
					row=0x20;
				break;   // char Q
			case 114:
				if (col == 0x02)
					row=0x10;
				break;   // char R
			case 115:
				if (col == 0x20)
					row=0x10;
				break;   // char S
			case 116:
				if (col == 0x40)
					row=0x08;
				break;   // char T
			case 117:
				if (col == 0x40)
					row=0x04;
				break;   // char U
			case 118:
				if (col == 0x08)
					row=0x08;
				break;   // char V
			case 119:
				if (col == 0x02)
					row=0x20;
				break;   // char W
			case 120:
				if (col == 0x08)
					row=0x10;
				break;   // char X
			case 121:
				if (col == 0x02)
					row=0x08;
				break;   // char Y
			case 122:
				if (col == 0x10)
					row=0x10;
				break;   // char Z
			case 123:
				if (col == 0x40)
					row=0x81;
				break;   // char ->
			case 127:
				if (col == 0x01)
					row=0x42;
				break;   // DEL char
			default:
				row=0x00;
				break;
			}

			return row;
		}
	}
}
