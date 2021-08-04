using System;
using MCMShared.Emulator;

namespace MCM70Client.Emulator.NotOriginal
{
	// TODO: would like to display tape motion info
	public class DisplayLights : Display
	{
		AplFont[] _aplFonts;
		private const int COLUMN_1_LEFT = 180;
		private const int COLUMN_2_LEFT = 350;
		private const int COLUMN_3_LEFT = 500;
		private const int ROW_1_TOP = 15;
		private const int ROW_2_TOP = 35;

		private const int INSTR_POINTER_X = COLUMN_1_LEFT;
		private const int INSTR_POINTER_Y = ROW_1_TOP;
		private const int STACK_POINTER_X = COLUMN_1_LEFT;
		private const int STACK_POINTER_Y = ROW_2_TOP;

		private const int ROM_BANK_X = COLUMN_2_LEFT;
		private const int ROM_BANK_Y = ROW_1_TOP;
		private const int SELECTED_DEVICE_X = COLUMN_2_LEFT;
		private const int SELECTED_DEVICE_Y = ROW_2_TOP;

		private const int MEM_READ_X = COLUMN_3_LEFT;
		private const int MEM_READ_Y = ROW_1_TOP;
		private const int MEM_WRITE_X = COLUMN_3_LEFT;
		private const int MEM_WRITE_Y = ROW_2_TOP;

		public DisplayLights(byte[] panel, byte[] memory, int x, int y, AplFont[] aplFonts)
			: base(panel, memory, x, y, aplFonts)
		{
			_aplFonts = aplFonts;
		}

		public override void RomBank(int bankNibble)
		{
			RomBank(bankNibble, _background);
		}
		public override void StackPointer(int stackPointer)
		{
			StackPointer(stackPointer, _background);
		}
		public override void InstructionPointer(ushort address)
		{
			DisplayAddress(InstructionPointerOffset, address, _background);
		}
		public override void MemoryRead(ushort address)
		{
			DisplayAddress(MemoryReadOffset, address, _background);
		}
		public override void MemoryWrite(ushort address)
		{
			DisplayAddress(MemoryWriteOffset, address, _background);
		}
		public override void SelectedDevice(byte device)
		{
			SelectedDevice(device, _background);
		}
		public override void InitAllesLookensgepeepers()
		{
			Letter(19, INSTR_POINTER_X - 24, INSTR_POINTER_Y - 2); // I
			Letter(26, INSTR_POINTER_X - 14, INSTR_POINTER_Y - 2); // P

			Letter(29, STACK_POINTER_X - 24, STACK_POINTER_Y - 2); // S
			Letter(26, STACK_POINTER_X - 14, STACK_POINTER_Y - 2); // P

			Letter(28, ROM_BANK_X - 24, ROM_BANK_Y - 2); // R
			Letter(12, ROM_BANK_X - 14, ROM_BANK_Y - 2); // B

			Letter(14, SELECTED_DEVICE_X - 24, SELECTED_DEVICE_Y - 2); // D
			Letter(32, SELECTED_DEVICE_X - 14, SELECTED_DEVICE_Y - 2); // V

			Letter(23, MEM_READ_X - 24, MEM_READ_Y - 2); // M
			Letter(28, MEM_READ_X - 14, MEM_READ_Y - 2); // R

			Letter(23, MEM_WRITE_X - 24, MEM_WRITE_Y - 2); // M
			Letter(33, MEM_WRITE_X - 14, MEM_WRITE_Y - 2); // W

		}
		public override void ClearAllesLookensgepeepers()
		{
			RomBank(0, _black);
			StackPointer(0, _black);
			DisplayAddress(InstructionPointerOffset, 0, _black);
			DisplayAddress(MemoryReadOffset, 0, _black);
			DisplayAddress(MemoryWriteOffset, 0, _black);
			SelectedDevice(0, _black);
		}
		public override void Message()
		{
			Console.WriteLine(Environment.NewLine+"BLINKING LIGHTS MODE");

			Console.WriteLine("The MCM/70 did NOT have blinking lights"+Environment.NewLine);
			Console.WriteLine("This mode shows lights in honor of an old sign popular in computer labs:"+Environment.NewLine);

			Console.WriteLine("Achtung! Alles Lookenpeepers!");
			Console.WriteLine("Dies Machine is nicht fur gefingerpoken und mittengraben.");
			Console.WriteLine("Is easy schnappen der springenwerk, blowenfusen und poppencorken mit spitzensparken.");
			Console.WriteLine("Is nicht fur gewerken by das dummkopfen. Das rubbernecken sightseeren keepen  ");
			Console.WriteLine("Cotten-pickenen hands in das pockets – relaxen und Watch Das Blinken Lights."+Environment.NewLine);
		}

		private class DisplayOffset
		{
			public readonly int X;
			public readonly int Y;
			protected DisplayOffset(int x, int y)
			{
				X = x;
				Y = y;
			}
		}

		private class DisplayOffsetGap : DisplayOffset
		{
			public readonly int G;
			public DisplayOffsetGap(int x, int y, int g)
				: base(x, y)
			{
				G = g;
			}
		}

		private readonly ushort[] _nibbleMask =
		{
			0x08, 0x04, 0x02, 0x01
		};
		private readonly Light _foreground = new Light
		(
			new Rgb(0x7F, 0x00, 0x00),
			new Rgb(0xFF, 0x00, 0x00),
			new Rgb(0x7F, 0x00, 0x00)
		);
		private readonly Light _background = new Light
		(
			new Rgb(0x40, 0x40, 0x40),
			new Rgb(0x80, 0x80, 0x80),
			new Rgb(0x40, 0x40, 0x40)
		);
		private readonly Light _black = new Light
		(
			new Rgb(0x00, 0x00, 0x00),
			new Rgb(0x00, 0x00, 0x00),
			new Rgb(0x00, 0x00, 0x00)
		);

		private readonly ushort[] _mask =
		{
			0x2000, 0x1000, 0x0800, 0x0400, 0x0200, 0x0100,
			0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01
		};

		private static readonly DisplayOffsetGap InstructionPointerOffset =
			new DisplayOffsetGap(INSTR_POINTER_X, INSTR_POINTER_Y, 20);
		private static readonly DisplayOffsetGap MemoryReadOffset =
			new DisplayOffsetGap(MEM_READ_X, MEM_READ_Y, 20);
		private static readonly DisplayOffsetGap MemoryWriteOffset =
			new DisplayOffsetGap(MEM_WRITE_X, MEM_WRITE_Y, 20);

		private void RomBank(int bankNibble, Light background)
		{
			var xOffset1 = ROM_BANK_X;
			var yOffset = ROM_BANK_Y;

			for (var y = 0; y < 5; y++)
			for (var x = 0; x < 4; x++)
			{
				var xoffset = xOffset1;

				var off = ((y + yOffset) * (Width * 3)) + (((x * 8) + xoffset) * 3);

				var light = (bankNibble & _nibbleMask[x]) != 0
					? _foreground
					: background;

				PanelLight(off, light);
			}
		}

		// Grab a vertically limited APL char from the fonts we already have
		private void Letter(int i, int x, int y)
		{
			int p, s, s1, x1, y1;

			// compute starting pixel position of a character on panel 
			s = 3 * ((Width * y) + x);

			// get the pixel position p of char i in APL_fonts image
			p = i * 36; // 36=3*12; each char is 12-pixel wide and each pixel has 3 RGB values

			// write char (with APL code i)
			var hoffsets = new int[] { 0,2,4,5,6,7,8,10,11 };
			for (y1 = 0; y1 < 9; y1++)     // for every row in char image
			{
				s1 = 3 * (y1 * Width) + s;
				for (x1 = 0; x1 < 36-3; x1++)
				{
					Panel[s1 + x1] = (byte)(255 - _aplFonts[ hoffsets[y1] ].Font[x1 + p]);
				}
			}
		}


		private void SelectedDevice(byte device, Light background)
		{
			var xOffset = SELECTED_DEVICE_X;
			var yOffset = SELECTED_DEVICE_Y;

			for (var y = 0; y < 5; y++)
			for (var x = 0; x < 8; x++)
			{
				var xoffset = xOffset;

				var off = ((y + yOffset) * (Width * 3)) + (((x * 8) + xoffset) * 3);

				var light = (device & _mask[x+6]) != 0
					? _foreground
					: background;

				PanelLight(off, light);
			}
		}

		private void StackPointer(int stackPointer, Light background)
		{
			var xOffset1 = STACK_POINTER_X;
			var yOffset = STACK_POINTER_Y;

			for (var y = 0; y < 5; y++)
			for (var x = 0; x < 3; x++)
			{
				var xoffset = xOffset1;

				var off = ((y + yOffset) * (Width * 3)) + (((x * 8) + xoffset) * 3);

				var light = (stackPointer & _nibbleMask[x]) != 0
					? _foreground
					: background;

				PanelLight(off, light);
			}
		}

		private void DisplayAddress(DisplayOffsetGap offset, ushort address, Light background)
		{
			for (var y = 0; y < 5; y++)
			for (var x = 0; x < 14; x++)
			{
				var xoffset = (x < 6)
					? offset.X
					: offset.X + offset.G;

				var off = ((y + offset.Y) * (Width * 3)) + (((x * 8) + xoffset) * 3);

				var light = (address & _mask[x]) != 0
					? _foreground
					: background;

				PanelLight(off, light);
			}
		}

		private void PanelLight(int offset, Light light)
		{
			Panel[offset] = light.Left.Red;
			Panel[offset + 1] = light.Left.Green;
			Panel[offset + 2] = light.Left.Blue;

			Panel[offset + 3] = light.Middle.Red;
			Panel[offset + 4] = light.Middle.Green;
			Panel[offset + 5] = light.Middle.Blue;

			Panel[offset + 6] = light.Middle.Red;
			Panel[offset + 7] = light.Middle.Green;
			Panel[offset + 8] = light.Middle.Blue;

			Panel[offset + 9] = light.Middle.Red;
			Panel[offset + 10] = light.Middle.Green;
			Panel[offset + 11] = light.Middle.Blue;

			Panel[offset + 12] = light.Right.Red;
			Panel[offset + 13] = light.Right.Green;
			Panel[offset + 14] = light.Right.Blue;
		}
	}
}
