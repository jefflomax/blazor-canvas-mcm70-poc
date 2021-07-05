using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCMShared.Extensions;

namespace MCMShared.Emulator
{
	public class Tapes
	{
		public TP tape0_s;
		public TP tape1_s;				// status of tape0 and tape1, and other information    
		public int[] tape0;
		public int[] tape1;
		public int[] tape;
		private Machine _machine;
		private Display _display;

		public const int delta = 11;	// distance between write and read head in cells (bytes) on tape; it has to be at least 11
		// large values will create a larger "small gap" between block's address header and data block

		private readonly byte[] _tapeLo;
		private readonly byte[] _tapeEo;
		private readonly byte[] _spinStop;
		private readonly byte[] _spinRight;
		private readonly byte[] _spinLeft;
		private readonly AplFont[] _aplFonts;
		private readonly List<TapeEntry> _tapeEntryList;


		//--------------------------------------------------------------------------------
		// InitTapes: initialize both tapes to "unloaded" when the emulator is started
		//--------------------------------------------------------------------------------
		public Tapes
		(
			byte[] tapeLo,
			byte[] tapeEo,
			byte[] spinStop,
			byte[] spinRight,
			byte[] spinLeft,
			AplFont[] aplFonts
		)
		{
			_tapeLo = tapeLo;
			_tapeEo = tapeEo;
			_spinStop = spinStop;
			_spinRight = spinRight;
			_spinLeft = spinLeft;
			_aplFonts = aplFonts;

			tape0_s = new TP();
			tape1_s = new TP();

			tape0_s.id=0xC8;
			tape1_s.id=0xC9;		// set ids

			tape0_s.status=0x05;
			tape1_s.status=0x05;	// set status to "unloaded"

			tape0_s.lid=0;
			tape1_s.lid=0;			// set lids to closed

			tape0_s.w_head=delta;
			tape1_s.w_head=delta;	// position write heads to delta
									// (the read heads are always at tape.w_head -delta position)
			tape0_s.speed=0;
			tape1_s.speed=0;		// tape not moving

			tape0_s.length=0;
			tape1_s.length=0;		// initialize tape size

			MoveClock=0;

			// TODO: These control the right-button hack, switch to class
			_tape0Loaded = false;
			_tape0ToSave = -1;
			_tape0IdSelected = -1;
			_tape0SelectedIsEject = false;
			_tape0SelectedPath = null;
			_tape0SelectedName = null;

			_tape1Loaded = false;
			_tape1ToSave = -1;
			_tape1IdSelected = -1;
			_tape1SelectedIsEject = false;
			_tape1SelectedPath = null;
			_tape1SelectedName = null;

			tape0 = new int[0];
			tape1 = new int[0];
			_tapeEntryList = new List<TapeEntry>();
			TapeEntries();
		}

		public int MoveClock { get; set; }			// needed for advancing the tape

		public void SetMachine(Machine machine)
		{
			_machine = machine;
			_display = machine.Display ?? throw new Exception("Display must be setup first");
		}

		public byte[] SpinStop => _spinStop;
		public byte[] SpinLeft => _spinLeft;
		public byte[] SpinRight => _spinRight;


		private const int MoveClockMax = 82; // 82
		public void TapeMovement(byte inst)
		{
			// Taken from the top of cpu.c
			// Tape movement; uses move_clock variable
			// if move_clock=82, then a tape is moved 1 cell in a specified direction
			MoveClock++;
			// adjusting move_clock by 3 each pass will give TAPE ERROR
			// adjusting by 2 got TAPE ERROR on [QUAD]XN 80
			//move_clock += 2;

			/* if clock is up and not a write to tape statement, adjust tape position and status; 
			   Note: writing to tape is handled separately
			   Note: the value 82 is selected experimentally to have appropriate gap sizes on tapes (10- and 70-byte long, as required
					 by tape organization); 
					 this value controls the number of instructions to be executed (e.g. 82) for a tape to advance 1 cell (byte) */
			if ((MoveClock >= MoveClockMax) && ((inst != 0x57) | ((tape0_s.speed & 0x10) == 0) | ((tape1_s.speed & 0x10) == 0)))
			{
				if (_machine.SelectedDevice == 0xC8) MoveTape(0);		// move tape 0
				if (_machine.SelectedDevice == 0xC9) MoveTape(1);		// move tape 1
			}

		}


		/*--------------------------------------------------------------------------------
		   Ascii2MCM (int x) -- required by label_cassette
		   translate ascii values into MCM encoding
		   input:  ascii value x
		   output: MCM/APL encoding of char x
		---------------------------------------------------------------------------------*/
		private static int Ascii2MCM(int x)
		{
			if ((x > 47) && (x < 58)) return (x - 48);		//  return a digit
			if ((x > 64) && (x < 91)) return (x - 54);		//  u.c. letters
			if ((x > 96) && (x < 123)) return (x - 86);		//  l.c. letters

			// neither a letter nor a digit 
			switch (x)
			{
				case 13: return -5;     // CR
				case 21: return 61;     // !
				case 22: return 91;     // "
				case 32: return 39;     // space
				case 40: return 87;     // (
				case 41: return 88;     // )
				case 42: return 56;     // *
				case 43: return 52;     // +
				case 44: return 75;     // ,
				case 45: return 53;     // -
				case 46: return 40;     // .
				case 47: return 66;     // /
				case 58: return 92;     // :
				case 59: return 81;     // ;  
				case 60: return 42;     // <
				case 61: return 44;     // =
				case 62: return 46;     // >
				case 63: return 64;     // ?
				case 91: return 85;     // [
				case 93: return 86;     // ]
				case 95: return 10;     // _
				case 124: return 60;    // |
				case 126: return 63;    // ~
				default: return 39;     // illegal char -- ignore it
			}
		}

		private const int CassetteWidth = 409;
		/*--------------------------------------------------------------------------
		   dsp_apl_cass(i,x,y) 
		   write the APL char with MCM/APL code i at coordinates x, y on a cassette;
		   used by label_cassette_N
		--------------------------------------------------------------------------*/
		private void DspAplCass(int i, int x, int y)
		{
			int p, s, s1, x1, y1;

			// compute starting pixel position of a character on panel 
			s = 3 * ((CassetteWidth * y) + x);

			// get the pixel position p of char i in APL_fonts image
			p = i * 36;				// 36=3*12; each char is 12-pixel wide and each pixel has 3 RGB values

			// write char (with APL code i) on cassette image
			for (y1 = 0; y1 < 12; y1++)		// for every row in char image
			{
				s1 = 3 * (y1 * CassetteWidth) + s;
				for (x1 = 0; x1 < 36; x1++)
				{
					_tapeLo[s1 + x1] = _aplFonts[y1].Font[x1 + p]; // 36= width of char (12pix)*3 RGB values
				}
			}
		}

		/*-------------------------------------------------------------
		   label_cassette_N(s,x,y)
		   label a cassette with string s starting at coordinates x, y
		-------------------------------------------------------------*/
		public void LabelCassetteN(string s, int x, int y)
		{
			int c = 0;
			int a, i;
			int p = x;			// p is the initial display position

			foreach(var ch in s)
			{
				a = Ascii2MCM(ch);		// get MCM/APL code a of char s[c]
				DspAplCass(a, p, y);	// display APL char s[c] with code a at p,y on a cassette
				p = p + 12;				// compute the next char x coordinate
				c++;
			}
			// pad the label with a few blanks to erase possible "leftovers" from the previous labels
			if (c < 18)
				for (i = c; i < 15; i++)
				{
					DspAplCass(39, p, y);	// display space
					p = p + 12;
				}
		}

		/*---------------------------------------------------------------------------
			load_tape: loads tape into specified drive of the emulator
			input: s  -- path name of tape to be loaded
				   id -- derive id where tape should be loaded (0 - left, 1 - right)
		---------------------------------------------------------------------------*/
		private void LoadTape(string s, int id)
		{
			int size;
			int length = 0;

			var fi = new FileInfo(s);
			if (fi.Exists)
			{
				// determine the number of tape bytes stored in s; calculate tape length    
				size = GetTapeBytes(s, null);
				Console.WriteLine($"Loading tape {s} Length {size:N0} on device {id}");

				length = size;							// sz= number of tape bytes
				if (length < 10000) length = 180000;	// if tape is short, make it longer so that it could be
				// read/written to correctly

				// allocated space for tape in tape drive; space is deallocated on closing GLUT window or ejecting tape from drive
				if (id == 0)
				{
					tape0 = new int[length]; // allocate memory for tape0 of length "length"
					tape = tape0;
				}
				else
				{
					tape1 = new int[length];
					tape = tape1;
				}

				// read tape bytes from s into tape0 or tape1
				if (size < 10000)
				{
					//  if sz < 10000, then it is an empty tape; fill it with 0x100 and return
					for (var i = 0; i < length; i++)
					{
						tape[i] = 0x100;
					}
				}
				else
				{
					GetTapeBytes(s, tape);
				}
			}

			// save status, etc of a tape
			if (id == 0)
			{
				tape0_s.status = 0xC0;			// tape mounted and ready
				tape0_s.w_head = delta;			// position write head
				tape0_s.speed = 0;				// set tape speed to stop
				tape0_s.length = length;		// record the tape length
			}
			else
			{
				tape1_s.status = 0xC0;
				tape1_s.w_head = delta;
				tape1_s.speed = 0;
				tape1_s.length = length;
			}
		}

		private static int GetTapeBytes(string filePath, int[] buffer)
		{
			var i = 0;
			using (var streamReader = File.OpenText(filePath))
			{
				while (!streamReader.EndOfStream)
				{
					var line = streamReader.ReadLine();
					if (line == null)
					{
						break;
					}
					foreach (var token in line.Split(' '))
					{
						if (!string.IsNullOrWhiteSpace(token))
						{
							var value = Convert.ToInt32(token, 16);
							if (buffer != null)
							{
								buffer[i] = value;
							}
							i++ ;
						}
					}
				}
				// TODO: Watch for tape short logic
			}

			return i;
		}

		/*---------------------------------------------------------------------------------
		   save_tape: saves tape in id's drive (id=0 for left drive and =1 for right drive)
					  in tapes' directory under the name s
		---------------------------------------------------------------------------------*/
		private void SaveTape(string s, int id)
		{
			int i, length;

			string tapePath = $@"Tapes/{s}";    // make path name for selected tape

			if (id == 0)
			{
				tape = tape0;
				length = tape0_s.length;
			}
			else
			{
				tape = tape1;
				length = tape1_s.length;
			}

			using (var streamWriter = File.CreateText(tapePath))
			{
				for (i = 0; i < length; i++)
				{
					if (tape[i] < 256)
					{
						streamWriter.Write($"{tape[i]:X3} ");
					}
					else
					{
						streamWriter.Write($"{tape[i]:X2} ");
					}
					if (tape[i] == 0x33 && tape[i - 1] == 0x17 && tape[i - 2] == 0x17)
					{
						streamWriter.Write("\n");
					}
				}
			}
		}

		private bool _tape0Loaded;
		private int _tape0ToSave;
		private int _tape0IdSelected;
		private bool _tape0SelectedIsEject;
		private string _tape0SelectedPath;
		private string _tape0SelectedName;

		private bool _tape1Loaded;
		private int _tape1ToSave;
		private int _tape1IdSelected;
		private bool _tape1SelectedIsEject;
		private string _tape1SelectedPath;
		private string _tape1SelectedName;

		public bool FinalizeTapeLoad(int tapeDrive)
		{
			switch (tapeDrive)
			{
				case 0:
					if (_tape0SelectedPath == null)
					{
						return false;
					}

					LoadTape(_tape0SelectedPath, 0);
					_tape0ToSave = _tape0IdSelected;
					tape0_s.name = _tape0SelectedName;
					_machine.SelectedDevice = 0xC8;    // load tape
					_tape0Loaded = true;
					_tape0SelectedPath = null;
					_tape0SelectedName = null;
					return true;
				case 1:
					if (_tape1SelectedPath == null)
					{
						return false;
					}

					LoadTape(_tape1SelectedPath, 1);
					_tape1ToSave = _tape1IdSelected;
					tape1_s.name = _tape1SelectedName;
					_machine.SelectedDevice = 0xC9;		// load tape
					_tape1Loaded = true;
					_tape1SelectedPath = null;
					_tape1SelectedName = null;
					return true;
			}

			return false;
		}

		public bool IsTapeLoaded(int tapeDrive)
		{
			switch (tapeDrive)
			{
				case 0:
					return _tape0Loaded && (tape0_s.status & 0x04) == 0;
				case 1:
					return _tape1Loaded && (tape1_s.status & 0x04) == 0;
			}

			return false;
		}

		public bool IsEject(int tapeDrive)
		{
			switch (tapeDrive)
			{
				case 0:
					return _tape0SelectedIsEject;
				case 1:
					return _tape1SelectedIsEject;
			}
			return false;
		}

		public void EjectTape(int tapeDrive)
		{
			if (IsTapeLoaded(tapeDrive))
			{
				switch (tapeDrive)
				{
					case 0:
						SaveTape(tape0_s.name, 0);					// save tape0 to file
						tape0 = null;
						tape0_s.status = 13;						// change tape status to no tape
						_display.SubImage(40, 148, 409, 256, _tapeEo);		// display empty drive on emulator's panel
						_tape0Loaded = false;
						_tape0SelectedPath = null;
						_tape0SelectedIsEject = false;
						break;
					case 1:
						SaveTape(tape1_s.name, 0);					// save tape1 to file
						tape1 = null;
						tape1_s.status = 13;						// change tape status to no tape
						_display.SubImage(483, 148, 409, 256, _tapeEo);		// display empty drive on emulator's panel
						_tape1Loaded = false;
						_tape1SelectedPath = null;
						_tape1SelectedIsEject = false;
						break;
				}
			}
		}

		public bool IsTapeClosed(int tapeDrive)
		{
			if ((tapeDrive == 0) && (tape0_s.lid == 0))
			{
				return true;
			}

			return false;
		}

		//--------------------------------------------------------------------------------
		// TapeMenu: tape selection menu
		//--------------------------------------------------------------------------------
		public void TapeMenu(int option, int tapeDevice)
		{
			int i = 0;
			int t;

			// determine which drive is selected
			t = tapeDevice;

			var tapeEntry = _tapeEntryList.First(t => t.Id == option);
			var fileName = tapeEntry.Name;
			fileName = Path.GetFileName(tapeEntry.Name);
			i = option;

			// if the selected drive has closed lid -- do nothing, tape cannot be loaded
			if ((t == 0) && (tape0_s.lid == 0)) return;
			if ((t == 1) && (tape1_s.lid == 0)) return;

			// tape can be loaded: make path name for the selected tape
			var tapePath = tapeEntry.Name;		// make path name for selected tape

			// load and display tape in the selected drive
			if (i == option)
			{
				if (t == 0)
				{
					LabelCassetteN(fileName, 154, 183);		// write tape's name as a label on a cassette
					_tape0SelectedIsEject = fileName.Equals("eject", StringComparison.CurrentCultureIgnoreCase);
					_display.SubImage(40, 148, 409, 256, _tapeLo);	// display tape in drive

					_tape0IdSelected = tapeEntry.Id;
					_tape0SelectedName = fileName;
					_tape0SelectedPath = tapePath;
				}
				if (t == 1)
				{
					LabelCassetteN(fileName, 154, 183);
					_tape1SelectedIsEject = fileName.Equals("eject", StringComparison.CurrentCultureIgnoreCase);
					_display.SubImage(483, 148, 409, 256, _tapeLo);

					_tape1IdSelected = tapeEntry.Id;
					_tape1SelectedName = fileName;
					_tape1SelectedPath = tapePath;
				}
			}
		}

		private class TapeEntry
		{
			public TapeEntry(int id, string name)
			{
				Id = id;
				Name = name;
			}
			public int Id { get; }
			public string Name { get; }
		}
		//--------------------------------------------------
		//  TapeEntries: Create menu entries
		//--------------------------------------------------
		public void TapeEntries()
		{
			// Do I need to port freeGLUT freeglut_menu.c
			// Read tapes from CONFIG, display on Printer?
			// Temporary fix show tape name on cassette, cycle thru with right click
#if SKIP_WASM
#else
			_tapeEntryList.AddRange
			(
				Directory
					.EnumerateFiles("tapes", "*.*", SearchOption.AllDirectories)
					.Select((f, ind) => new TapeEntry(ind, f))
			);
#endif
		}

		public int NextTapeIndex(int index)
		{
			if (++index < _tapeEntryList.Count)
			{
				return index;
			}

			return 0;
		}

		private bool _halted = false;

		/*------------------------------------------------------------------------
		   moveTape: tape movement, writing to tape; setting status after the move
		   input:  id -- tape id
		   output: modified tape, tape position, and status
		------------------------------------------------------------------------*/
		public void MoveTape(int id)
		{
			int i, h, s, length, speed;	// variables for temporal assignments
			int[] tape;					// variable to be either tape0 or tape1
			// clear clock
			MoveClock = 0;

			// if no tape or non-tape device, then return
			if ((_machine.SelectedDevice != 0xC8) && (_machine.SelectedDevice != 0xC9))
			{
				return;		// not a tape
			}

			if ((_machine.SelectedDevice == 0xC8) && ((tape0_s.status & 0x04) != 0)) return; // tape0 not mounted
			if ((_machine.SelectedDevice == 0xC9) && ((tape1_s.status & 0x04) != 0)) return; // tape1 not mounted


			// make these temporal assignments
			if (_machine.SelectedDevice == 0xC8)
			{
				tape = tape0;
				speed = tape0_s.speed;
				h = tape0_s.w_head;
				s = tape0_s.status;
				length = tape0_s.length;
			}
			else
			{
				tape = tape1;
				speed = tape1_s.speed;
				h = tape1_s.w_head;
				s = tape1_s.status;
				length = tape1_s.length;
			}


			// move write head depending on the tape speed and direction
			var inst = _machine.GetCurrentInstruction;
			switch (speed)
			{
				case 0x00: // tape halted 
				case 0x10: // tape halted, write head activated -- nothing to do so return
					if (_halted == false)
					{
						_halted = true;
						Console.WriteLine($"Tape halted Speed:{speed} Op:{inst:X2}");
					}
					return;			// for both cases
				case 0x13: // slow forward with writing enabled
				case 0x03: // slow forward without writing
					if (speed == 0x13)
					{
						if (inst != 0x57) tape[h] = 256;      // if not "write" instruction, then write "noise" (256)
					}
					h++;
					break;			// for both cases
				case 0x05: // slow reverse without writing
					h = h - 1;
					break;
				case 0x1B: //  fast forward + write head enable
				case 0x0B: // fast forward -- no writing
					if (speed == 0x1B)
					{
						if (inst == 0x57) break;			// "write" instruction is taken care separately 
						for (i = 0; i < 4; i++)
							if ((h + i) < length) tape[h + i] = 256;  // write "noise" (256)

					}
					h = h + 4;
					break;			// for both cases
				case 0x1D: //  fast reverse + write head enabled
				case 0x0D:
					if (speed == 0x1D)
					{
						for (i = 0; i < 4; i++)
							if ((h - i) >= delta) tape[h - i] = 256;  // write "noise" (256)
					}
					h = h - 4;		// fast reverse
					break;    // for both cases
				default: 
					//i = i;          // do nothing as tape is not moving
					break;
			}
			_halted = false;


			// adjust tape position and status, if out of bounds:
			if ((h < delta) || (h >= length))					// if out of bounds, then:
			{
				if (h < delta) h = delta; else h = length - 1;	// re-position the head
				s = (s | 0x49);									// record: out of bounds, no data, ready to receive a byte
																 //  TapeStatus = TapeStatus Or &H40  'Set TBMT
			}
			else				// the head is within bounds, so:
			{
				if (tape[h - delta] == 0x100) s = s | 0x01;		// set status to "not data"
				else s = ((s & 0xFE) | 0xC0);					// set status to "it's data" and byte ready
			}

			// make changes permanent
			if (_machine.SelectedDevice == 0xC8)
			{
				tape0_s.status = (byte)s;
				tape0_s.w_head = h;
			}	// if tape0 (id=0xC8), then...
			else
			{
				tape1_s.status = (byte)s;
				tape1_s.w_head = h;
			}

		}

		//-------------------------------------------------------------------------------
		// wait140: delay sampling a tape by 140uSec while tape is moving;
		//-------------------------------------------------------------------------------
		void wait140()
		{
			_machine.Sleep(1);
		}

		/***************************************************************************************
		 *                TAPE CALLBACKS, MENU and other tape UTILITIES                        *
		 * *************************************************************************************/
	}

	public class TP
	{
		public byte id;		// tape0: 0xC8, tape1: 0xC9
							//char name[20];// tape name
		public string path;
		public string name;
		public byte status; // status of tape 0:
							//   Bit0:  1=Data Pause. IRG signal.  Set to 1 after 3.5mS of no data.
							//          Will be 1 when there is no tape or no tape running. Used at
							//          high speeds to count blocks.
							//   Bit1:  1=Write Disabled on Tape.  Tape cannot be written to.
							//   Bit2:  1=Cassette Unloaded.  No tape in drive.
							//   Bit3:  1=Cassette Not Ready. Status of flip-flop in the drive. 
							//          Is set when no tape or tape run without clock
							//   Bit4:  1=Receiver Over Run (USRT).  Last Data byte from the USRT was
							//          not read before a new byte was received.
							//   Bit5:  1=Receive Parity Error (USRT).  Received byte did not have the 
							//          right parity.
							//   Bit6:  1=Transmit Buffer Empty (USRT). Transmitter ready to accept next
							//          byte.
							//   Bit7:  1=Receive Data Available (USRT).  Received a byte.
		public int lid;		// 1=lid opened, 0= lid closed
		public int w_head;	// position of write head
		public byte speed;	// current speed and movement direction of tape, write permission;
							// the value is set by OUT 0A instruction
							//   0x00: tape is not moving
							//   0x10: tape is not moving, writing enabled
							//   0x03: tape moving slowly forward, no writing
							//   0x13: tape moving slowly forward while writing
							//   0x0B: fast forward, no writing
							//   0x1B: fast forward while writing
							//   0x05: slow reverse, no writing
							//   0x15: slow reverse while writing
							//   0x0D: fast reverse,  no writing
							//   0x1D: fast reverse,  no writing
							//long int length;       //   actual length of a tape in tape bytes
		public int length;	//   actual length of a tape in tape bytes
	}

}
