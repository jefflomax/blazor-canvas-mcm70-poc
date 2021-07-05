
namespace MCMShared.Emulator
{
	public class Display
	{
		protected readonly byte[] Panel;
		private readonly byte[] _memory;
		protected readonly int Width;
		private readonly int _height;

		private const int x_off = 14;   // x coordinate of SelfScan window with resp. to main window
		private const int y_off = 75;   // y coordinate of SelfScan window with resp. to main window

		public Display
		(
			byte[] panel,
			byte[] memory,
			int x,
			int y,
			AplFont[] aplFonts
		)
		{
			Panel = panel;
			_memory = memory;
			Width = x;
			_height = y;
		}

		public virtual void InstructionPointer(ushort address)
		{
		}
		public virtual void MemoryRead(ushort address)
		{
		}
		public virtual void MemoryWrite(ushort address)
		{
		}
		public virtual void RomBank(int bankNibble)
		{
		}
		public virtual void SelectedDevice(byte device)
		{
		}
		public virtual void StackPointer(int stackPointer)
		{
		}
		public virtual void InitAllesLookensgepeepers()
		{

		}
		public virtual void ClearAllesLookensgepeepers()
		{
		}
		public virtual void Message()
		{
		}

		/***************************************************************************
		 *                 Burroughs Self-Scan (SS)  display                       *
		 ***************************************************************************/

		/*----------------------------------------------------------------------------
			turn the y-th cell on (0 < y < 7) in the x-th display column (0 < x < 222)
			SelfScan cell is implemented as 3x3 array of pixels 
		----------------------------------------------------------------------------*/
		private void pixON(int x, int y)
		{
			int i, y1;
			int x1;
			byte r, g, b, r1, g1, b1;

			var panel = Panel;

			// compute number of pixels in display from the top left corner to the top pixel of the column
			// to be displayed
			x1=x_off+(x*4) + 8;			// x1 is the x-coordinate of the leftmost pixel of column x;
										// each column is 4-pixel wide; +8 is to get to the first column of SS
			y1=(y_off + (y*4)) * Width; // y1=number of pixels in panel rows from the top of panel to
										// to row y
			i= (x1+y1)*3;				// i = index in panel[] of the top pixel of a column; there are 3
										// RGB values
										// colors
			r1=174;
			g1=35;
			b1=35;		// char color -- cell corners
			r=237;
			g=79;
			b=80;		// char color -- cell center

			panel[i]=r1;
			panel[i+1]=g1;
			panel[i+2]=b1;		//  xxx  -- top 3 pixels
			panel[i+3]=r;
			panel[i+4]=g;
			panel[i+5]=b;		//  ...
			panel[i+6]=r1;
			panel[i+7]=g1;
			panel[i+8]=b1;		//  ...

			i=i+(3 * Width);
			panel[i]=r;
			panel[i+1]=g;
			panel[i+2]=b;         // xxx
			panel[i+3]=255;
			panel[i+4]=228;
			panel[i+5]=238; // xxx
			panel[i+6]=r;
			panel[i+7]=g;
			panel[i+8]=b;		// ...

			i=i+(3*Width);
			panel[i]=r1;
			panel[i+1]=g1;
			panel[i+2]=b1;		// xxx
			panel[i+3]=r;
			panel[i+4]=g;
			panel[i+5]=b;		// xxx
			panel[i+6]=r1;
			panel[i+7]=g1;
			panel[i+8]=b1;		// xxx

			if (y < 6)  // repaint the space between pixels 
			{
				i=i+(3*Width);
				panel[i]=0x3D;
				panel[i+1]=0x25;
				panel[i+2]=0x25;
				panel[i+3]=0x41;
				panel[i+4]=0x24;
				panel[i+5]=0x26;
				panel[i+6]=0x40;
				panel[i+7]=0x23;
				panel[i+8]=0x25;	//= = =
			}
		}

		/*-----------------------------------------------------------------------------
			turn the y-th cell off (0 < y < 7) in the x-th display column (0 < x < 222)
			SelfScan cell is implemented as 3x3 array of pixels 
		-------------------------------------------------------------------------------*/
		private void pixOFF(int x, int y)
		{
			int i, y1;
			int x1;

			var panel = Panel;

			x1=x_off+(x*4) + 8;
			y1=(y_off + (y*4)) * Width;
			i= (x1+y1)*3;					// i = index in panel[] of the top pixel of a column; there are 3
											// 1st row
			panel[i]=72;
			panel[i+1]=43;
			panel[i+2]=37;		// 1st pixel
			panel[i+3]=75;
			panel[i+4]=42;
			panel[i+5]=37;		// 2nd pixel
			panel[i+6]=81;
			panel[i+7]=46;
			panel[i+8]=42;		// 3rd pixel
								//2nd row
			i=i+(3*Width);
			panel[i]=71;
			panel[i+1]=40;
			panel[i+2]=35;
			panel[i+3]=79;
			panel[i+4]=44;
			panel[i+5]=40;
			panel[i+6]=80;
			panel[i+7]=42;
			panel[i+8]=39;

			//3rd row  
			i=i+(3*Width);
			panel[i]=73;
			panel[i+1]=43;
			panel[i+2]=41;
			panel[i+3]=82;
			panel[i+4]=48;
			panel[i+5]=47;
			panel[i+6]=83;
			panel[i+7]=47;
			panel[i+8]=47;

			if (y<6)  // repaint the space between pixels
			{
				i=i+(3*Width);
				panel[i]=0x3D;
				panel[i+1]=0x25;
				panel[i+2]=0x25;
				panel[i+3]=0x41;
				panel[i+4]=0x24;
				panel[i+5]=0x26;
				panel[i+6]=0x40;
				panel[i+7]=0x23;
				panel[i+8]=0x25;
			}
		}

		//------------------------------------------------------
		//      refresh SelfScan; all 222 columns are refreshed
		//------------------------------------------------------
		public virtual void refresh_SS()
		{
			int i, j;
			byte h;
			// display every column represented by bytes stored in mem[2021]-mem[20FE]
			for (i=0; i< 222; i++)
			{
				h=_memory[0x2021 + i];		// get a column byte from memory (it is inverted!)

				// modify 7 pixels of panel[] corresponding to the column defined by h
				byte mask = 1;
				for (j=0; j< 7; j++)
				{
					if ((h & mask) !=0)
					{
						pixON(i, j);
					}
					else
					{
						pixOFF(i, j);
					}
					mask<<=1;
				}
			}

			// flag display as refreshed
			//refresh_display=0;
#if false
			// modify texture
			glBindTexture(GL_TEXTURE_2D, textureMain);            // texture TEXTURE_TYPE_MCM will be modified
			glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGB, GL_UNSIGNED_BYTE, panel);  // modify image
			glutPostRedisplay();                   // set flag to re-display window
#endif
		}

		//--------------------------------------------------------------------
		// clear SelfScan
		//--------------------------------------------------------------------
		public virtual void ClearDisplay()
		{
			int i, j;

			for (i=0; i< 222; i++)
				// turn off all 7 pixels of i-th column
				for (j=0; j< 7; j++)
					pixOFF(i, j);       // turn j-th cell  "off"

#if false
			// modify texture
			glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGB, GL_UNSIGNED_BYTE, panel);  // modify texture
			glutPostRedisplay();                   // set flag to re-display window
#endif
		}

		/*--------------------------------------------------------------------------------------
		 SubImage(int x, int y, int w, int h, BYTE* a)
			modify panel with image of width w and height h at x, y (upper-left corner of a)
			similar to  glTexSubImage2D but works correctly with all sizes of images
		--------------------------------------------------------------------------------------*/
		public void SubImage(Rectangle r, byte[] a)
		{
			SubImage(r.X, r.Y, r.W, r.H, a);
		}

		public virtual void SubImage(int x, int y, int w, int h, byte[] a)
		{
			var n = ((932 * y) + x) * 3;
			var j = w * 3;
			for (var m = 0; m < h; m++)
			{
				for (var by = 0; by < j; by++)
				{
					Panel[n + by] = a[(m * j) + by];
				}
				n += 2796;
			}
		}

	}
}
