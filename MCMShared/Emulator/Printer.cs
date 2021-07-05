 using System;

namespace MCMShared.Emulator
{
	public class Printer
	{
		// =====================  printer.c prototypes/variables/definitions  ===========================

		public const int p_width = 944;				// printer window's width
		public const int p_height = 700;			// printer window's height
		protected const int  head_Y =  629;			// Y-coordinate of printer's head
		private const int left_mar = 64;			// head's leftmost x coordinate
		public const int page_start = 560;			// y-coordinate of the top left corner of page, when page is initialized
		public const int page_bottom = 626;			// y-coordinate of the bottom of visible page

		public bool InitializePrinterHead ;			// Flags Keyboard.c line 246
		public bool RenderResetHead;
		public bool RenderRunPrinterOut0A;
		public byte RenderRunPrinterOut0AData;
		public bool Redisplay;

		public readonly byte[] _printerWindow;
		public readonly AplFont[] _aplFonts;
		private readonly byte[] _pr_error_off;
		private readonly byte[] _pr_error_on;

		private bool _printerConnected ;
		// true, if printer is connected to Omniport
		public bool PrinterConnected => _printerConnected;
		public byte pr_status = 241;			// printer status initialized to 241 -- everything OK
												//  pr_status[7] - paper feed ready
												//  pr_status[6] - carriage ready
												//  pr_status[5] - character print ready 
												//  pr_status[4] - ribbon up
												//  pr_status[3] - ribbon red
												//  pr_status[2] - paper out
												//  pr_status[1] - check condition (carriage motion)
												//  pr_status[0] - printer powered and ready  

		public byte pr_data = 0;			// this variable holds 8 ls bits of a 16-bit printer command;
											// the value is assigned by OUT 0B instruction
		public byte pr_op_code;				// stores current printer operation code -- needed by page movement animation
											// when is set to 0 -- no operation
		protected int car_X = 76;			// X-coordinate of a character to be printed (in pixels, top left pixel of char)
		int car_Y = 612;					// Y-coordinate of a character to be printed (in pixels, top left pixel of char)

		//byte printer_addressed = 0;		// flag to indicate whether printer has been addressed via OMNIPORT  
		public byte ABC = 0;				// flag to indicate whether or not Answer-Back Code is requested

		private class PageNode
		{
			public int x;
			public int y;
			public int ch;
			public byte prData;
			public PageNode next;
			public bool NewLine;
		}

		private PageNode page;				// pointer to page
		private PageNode last_ch;			// pointer to the last char in line
		private int page_top;				// y-coordinate of the top of the page

		/* **********************************************************************************
		 *      Emulation of MCP-132 printer (which is Diablo HyType I)                     *
		 *      Although the MCP-132 printer can print up to 132 characters per line,       *
		 *      the max number of columns in this emulation is 68.                          *
		 ************************************************************************************/

		public Printer
		(
			byte[] printerWindow,
			AplFont[] aplFonts,
			byte[] prErrorOff,
			byte[] prErrorOn
		)
		{
			_printerWindow = printerWindow;
			_aplFonts = aplFonts;
			_pr_error_off = prErrorOff;
			_pr_error_on = prErrorOn;
			_printerConnected = false;

			Redisplay = false;
			InitializePrinterHead = false;
			RenderResetHead = false;
			RenderRunPrinterOut0A = false;
		}

		public bool RenderPending => RenderRunPrinterOut0A ||
			pr_op_code != 0 ||
			InitializePrinterHead ||
			RenderResetHead ||
			Redisplay;

		public void SetPrinterConnected(bool state)
		{
			_printerConnected = state;
			if (PrinterConnected)
			{
				Console.WriteLine("Printer is NOW CONNECTED");
				Console.WriteLine("[QUAD] is SHIFT-L");
				Console.WriteLine("[QUAD] OUT 1");
				Console.WriteLine("Before you try to [QUAD]<- anything");
			}
			else
			{
				Console.WriteLine("Printer is DISCONNECTED");
			}
		}

		/*----------------------------------------------------------------------------------
		   HT2MCM (int x)
		   translate HyType char values into MCM encodings
		   input:  HT value x times 2 (that's the value transmitted by the MCM/70)
		   output: MCM/APL encoding of char x
		-----------------------------------------------------------------------------------*/
		private static int HT2MCM(int x)
		{
			x = (x / 2);		// x is now a "true HyTape char code value

			// a digit or a letter
			if ((x > 47) && (x < 58)) return (x - 48);		// return a digit code
			if ((x > 96) && (x < 123)) return (x - 86);		// return a letter code

			// neither a letter nor a digit 
			switch (x)
			{
				case 32: return 89;     // ??? little o
				case 33: return 41;     // ..
				case 34: return 88;     // )
				case 35: return 42;     // <
				case 36: return 43;     // <=
				case 37: return 44;     // =
				case 38: return 46;     // >
				case 39: return 86;     // ]
				case 40: return 48;     // v
				case 41: return 49;     // logical and
				case 42: return 47;     // 
				case 43: return 55;     // div
				case 44: return 75;     // ,
				case 45: return 52;     // +
				case 46: return 40;     // .
				case 47: return 66;     // /
				case 58: return 87;     // (
				case 59: return 85;     // [
				case 60: return 81;     // ;
				case 61: return 54;     // x
				case 62: return 92;     // :
				case 63: return 67;     // right slash
				case 64: return 100;    // negative number
				case 65: return 94;     // alpha
				case 66: return 71;     // false
				case 67: return 96;     // intersection
				case 68: return 58;     // left floor symbol
				case 69: return 73;     // epsilon
				case 70: return 10;     // _
				case 71: return 93;     // triangle down
				case 72: return 37;     // triangle up
				case 73: return 74;     // iota
				case 74: return 89;     // small circle
				case 75: return 91;     // '
				case 76: return 38;     // quad 
				case 77: return 60;     // |
				case 78: return 72;     // true
				case 79: return 62;     // big o
				case 80: return 56;     // *
				case 81: return 64;     // ?
				case 82: return 76;     // rho
				case 83: return 59;     // left ceiling
				case 84: return 63;     // ~
				case 85: return 70;     // arrow down
				case 86: return 97;     // union
				case 87: return 95;     // omega
				case 88: return 98;     // inverted inclusion
				case 89: return 69;     // arrow up
				case 90: return 99;     // inclusion
				case 91: return 80;     // <-
				case 93: return 82;     // ->
				case 94: return 45;     // >=
				case 95: return 53;     // -
				case 126: return 106;   // $ 
				default: return 64;     // illegal char, represented as ?
			}
		}

		/*---------------------------------------------------------------------------------------------
		   display printer error_off button
		   note: glBindTexture, glTexSubImage2D, and glutPostRedisplay have to follow for changes to
				 take effect
		---------------------------------------------------------------------------------------------*/

		private void PrinterError()
		{
#if SKIP_WASM
#else
			int m;

			var n = ((p_width * 656) + 491) * 3;
			// (x,y) coordinates of the pr_error_off image are (491,656)
			for (m = 0; m < 24; m++)		// image height=24
			{
				//memcpy(&printer_win[n], &pr_error_on[m * 156], 156);
				for (var i = 0; i < 156; i++)
				{
					_printerWindow[n + i] = _pr_error_on[m * 156 + i];
				}
				n = n + 2832;				// offset to the next pixel line of image p_width=944
			}
#endif
		}


		//---------------------------------------------------------------------------------------------
		// display printer error_on button
		//---------------------------------------------------------------------------------------------

		private void PrinterRestore()
		{
#if SKIP_WASM
#else
			int m;

			var n = ((p_width * 656) + 491) * 3;
			// (x,y) coordinates of the pr_error_off image are (491,656)
			for (m = 0; m < 24; m++)			// image height=24
			{
				//memcpy(&printer_win[n], &pr_error_off[m * 156], 156);
				for (var i = 0; i < 156; i++)
				{
					_printerWindow[n + i] = _pr_error_off[m * 156 + i];
				}

				n += 2832;						// offset to the next pixel line of image p_width=944
			}
#endif
		}

		/*-----------------------------------------------------------------------------------------
		   blank an area of printer's window defined by x, y, w, h with color c as specified below:
		   input:  x, y -- pixel coordinates of a block to be blanked (top left corner)
				   w -- width of a block to be blanked (in pixels)
				   h -- height of the block (in pixels)
				   c -- "uniform" color R=G=B=c
		-----------------------------------------------------------------------------------------*/
		public virtual void BlankBlock(int x, int y, int w, int h, int c)
		{
			int x1, y1, s1;
			int s = (p_width * y) + x;	// starting pixel for blanking line
			int d = w * 3;				// s=width in RGB values

			// do the blanking
			for (y1 = 0; y1 < h; y1++)
			{
				s1 = 3 * (s + (y1 * p_width));
				for (x1 = 0; x1 < d; x1++)
				{
					_printerWindow[s1 + x1] = (byte)c;	// modify printer's texture
				}
			}
		}

		/*----------------------------------------------------------------------------------------
		   dsp_apl_printer(i,x,y)
		   print the APL char with MCM/APL code i on MCP-132 printer at (x,y) (x, y in pixels)
		----------------------------------------------------------------------------------------*/
		protected virtual void DspAplPrinter(int i, int x, int y)
		{
			int p, s, s1, x1, y1;

			// compute starting pixel position of a char in printer's window 
			s = 3 * ((p_width * y) + x);

			// compute the first color value of i-th font in apl_fonts image
			p = i * 36;             // 36 = (12 pixels of font image width )* 3 RGB values

			// print char
			for (y1 = 0; y1 < 12; y1++)
			{
				s1 = 3 * (y1 * p_width) + s;
				for (x1 = 0; x1 < 36; x1++)
					if (_aplFonts[y1].Font[x1 + p] < _printerWindow[s1 + x1]) // the "if" guard is introduced to
					{
						_printerWindow[s1 + x1] = _aplFonts[y1].Font[x1 + p]; // allow overwriting characters
					}
			}
		}

		//--------------------------------------------------------------------------------------
		// display image of printer's head at car_X coordinate (Y-coordinate is fixed at head_Y)
		//--------------------------------------------------------------------------------------
		protected virtual void DisplayHead()
		{
			int x1, y1, s1;

			// compute starting pixel for rewriting printer's image with the image of printer's head 
			var s = (p_width * head_Y) + car_X;

			// display head
			for (y1 = 0; y1 < 12; y1++)
			{
				s1 = 3 * (s + (y1 * p_width));
				for (x1 = 0; x1 < 36; x1++)
					_printerWindow[s1 + x1] = _aplFonts[y1].Font[x1 + 3780];   // modify printer's window
			}	// 3780= 105 (position of head's image in APL font image)*36 values
		}

		//--------------------------------------------------------------------------------------
		// move printer's head to the left margin
		//--------------------------------------------------------------------------------------
		public void ResetHead()
		{
			Console.WriteLine("Reset Head");
			BlankBlock(car_X, head_Y, 12, 12, 124);	// blank head at its current position
			car_X = left_mar;							// set head X-coordinate to left_mar
			DisplayHead();								// display head
		}

		public void ResetPrinter()
		{
#if SKIP_WASM
#else
			pr_status = (byte)(pr_status & 0xFB);		// rest printer status to "paper in" (bit 2 set to 0)
			page_top = page_start;
			BlankBlock(car_X, head_Y, 12, 12, 124);	// blank current head position
			car_X = left_mar;
			DisplayHead();								// display head 
			PrinterRestore();							// display printer error off image

			Redisplay = true;
#endif
		}

		/*-----------------------------------------------------------------------------------------
		   display_page(dir, sp) -- move virtual page sp pixels in dir direction
					  input: dir -- direction of scrolling paper: 0 (up) 1 (down)
						 sp: amount of move (in pixels)
		-----------------------------------------------------------------------------------------*/

		public void DisplayPage(int dir, int sp)
		{
			PageNode p_tmp;
			//int h;			// height of a block to be blanked

			// no scrolling down below page_bottom (before sp adjustment)
			if (page_top >= page_bottom) return;

			// adjust page top by sp to reflect new y-coordinate of the top of the page
			if (dir == 0)
			{
				page_top = page_top - sp;
			}
			else
			{
				page_top = page_top + sp;
			}

			// no scrolling down below page_bottom (after sp adjustment), just clean-up the page
			if (page_top >= page_bottom) page_top = page_bottom;

			// paint printer's page white
			if (page_top < 0)
			{
				BlankBlock(12, 0, 920, page_bottom, 255);  // page extend above visible window -- blank everything 
			}
			else
			{
				BlankBlock(12, page_top, 920, page_bottom - page_top, 255);
			}

			// paint "tray background" when page is scrolled down
			if ((dir != 0) && (page_top > 0))
			{
				BlankBlock(12, 0, 920, page_top, 80);
			}

			// if virtual page has something on it, then adjust y-coordinates of all the characters stored in it and re-display them
			if (page != null)
			{
				p_tmp = page;
				while (p_tmp != null)
				{
					if (dir == 0)
					{
						p_tmp.y = p_tmp.y - sp;
					}
					else
					{
						p_tmp.y = p_tmp.y + sp;
					}

					// re-display all characters with adjusted y coordinates
					if ((p_tmp.y >= 0) && (p_tmp.y <= car_Y))
					{
						// character's coordinates are within virtual space
						DspAplPrinter(p_tmp.ch, p_tmp.x, p_tmp.y); // p_tmp->ch = char to be displayed
					}

					p_tmp = p_tmp.next;						// next char to adjust
				}
			}

			// if page_top >= page_bottom then printer error -- out of paper
			if (page_top >= page_bottom)
			{
				pr_status = (byte)(pr_status | 0x04);		// record "paper out" in printer's status
				PrinterError();								// display printer error image
			}
			Redisplay = true;
		}

		public void ClearPage()
		{
			page = null;
		}

		/*---------------------------------------------------------------------------------------------------------------
		   runPrinter(code) -- execute printer's instruction given by code 

		   Note: printer's commands are 2-byte long instructions: the ms byte is passed to runPrinter as variable "code";
				 the 3 ms bits of code determine printer's operation to be executed;
				 the remaining 5 bits of code constitute msb of 13-bit data (if needed), where the remaining 8 bits are
				 stored in the global variable pr_data by instruction OUT B
		---------------------------------------------------------------------------------------------------------------*/
		public void RunPrinter(int code, bool isAnimation)
		{
			int incr;
			byte bits3;						// three most significant bits determine printer's action
			byte h;							// h = MCM code of a character to be printed
			PageNode tmp;

			bits3 = (byte)(code >> 5);		// get the decimal value of the ms 3 bits of code

			// check printer status, if not OK, then do nothing; the only exception is the operation
			// "reset" with code value 7
			if ((pr_status != 241) && (bits3 != 7))
			{
				return;
			}
			bool requestRedisplay = true;

			//glutSetWindow(window2);                       // make printer's window active
			//glBindTexture(GL_TEXTURE_2D, texturePrinter); // work with printer's window

			switch (bits3)
			{
				case 1: // print character (without advancing the head)
					h = (byte)HT2MCM((int)pr_data);			// h=MCM char code of MCP-132 char code
					DspAplPrinter(h, car_X, car_Y);			// print h at (car_X,car_Y)

					// save the char on printer's virtual page
					//tmp = (p_node*)malloc(sizeof(p_node));
					tmp = new PageNode();
					tmp.x = (car_X);						// x-coordinate of a char (in pixels)
					tmp.y = (car_Y);						// x-coordinate of a char (in pixels)
					tmp.ch = h;								// store char h (MCM encoding) in a node
					tmp.prData = (byte)(pr_data/2);
					tmp.next = null;
					if (page == null)
					{
						page = tmp;
						last_ch = tmp;
					} // first char stored
					else
					{
						last_ch.next = tmp;
						last_ch = tmp;
					} // add the node of a new char to the end of virtual page
					break;

				case 2: // head move (1 position = 1/60th of an inch)
					BlankBlock(car_X, head_Y, 12, 12, 124);	// blank head at its current position (head image is 12 by 12 pixels)

					// compute the amount of head increment (in pixels); this is given by 
					// 11-bit word: 3 least significant bits of code and 8 bits of pr_data 
					incr = pr_data + ((code & 0x07) * 256);
					if (incr > 12) h = 12; else h = (byte)incr;		// h = number of pixels of head move in a single frame of animation ( =< 12)

					// head movement is animated: up to 12 pixels at a time; if incr > 12, then compute the next incr
					// and store it in pr_op_code and pr_data for the next frame of the animation
					if (incr > 12)
					{
						incr = incr - 12;
						// compute new values of pr_op_code and pr_data for animation of head movement 
						// note: pr_op_code is an adjusted pr operation code for the next frame of animation
						pr_data = (byte)(incr & 0x0FF);						// 8 lsb's form the value of pr_data 
						pr_op_code = (byte)((incr >> 8) & 0x07);			// bits 9, 10, and 11 form the top bits of printer data
						pr_op_code = (byte)(pr_op_code | (code & 0xF8));	// transfer the msb's of code to pr_op_code because
																	// they encode printer's operation and direction
					}
					else pr_op_code = 0;							// set flag to "head movement animation finished"

					// bit 3 of code (code & 0x08) contains the info about the direction of move
					if ((code & 0x08) == 0)			// move carriage forward by incr pixels
					{
						car_X = car_X + h;
						if (car_X > 900)			// if attempt to move beyond right margin then:
						{
							DisplayPage(0, 16);		// move page UP by 16px
							car_X = left_mar;		// move the head left to the initial position
						}
					}
					else   // move carriage backwards by incr pixels
					{
						if(last_ch != null)
						{
							last_ch.NewLine = true;
						}
						car_X = car_X - h;
						if (car_X < left_mar)
						{
							car_X = left_mar;		// prevent move beyond left margin
						}
					}
					DisplayHead();					// display head's image at car_X and car_Y coordinates 
					break;

				case 4: // paper feed by N x 1/48th of an inch (represented in this emulator by N pixels)
						//   compute the scrolling amount
					incr = pr_data + ((code & 0x07) * 256);
					if (incr > 8) h = 8; else h = (byte)incr;	// h = number of pixels to move in a single frame of
																// page move emulation ( =< 8)

					// calculate the remaining amount to scroll and store the result in pr_op_code and pr_data
					if (incr > 8)
					{
						incr = incr - h;
						// compute new values of pr_op_code and pr_data for animation of paper scrolling 
						// note: pr_op_code is an adjusted pr operation code for the next frame of animation
						pr_data = (byte)(incr & 0x0FF);					// 8 lsb's form the value of pr_data 
						pr_op_code = (byte)((incr >> 8) & 0x07);		// bits 9, 10, and 11 form the top bits of printer data
						pr_op_code = (byte)(pr_op_code | (code & 0xF8));	// transfer the msb's of code to pr_op_code because
																		// they encode printer's operation and direction
					}
					else pr_op_code = 0;						// set flag to "page scrolling done"

					// scroll page up/down h pixels
					if ((code & 0x08) == 0) DisplayPage(0, h);	// page UP
					else DisplayPage(1, h);						// page DOWN
					break;

				case 7: // restore/reset printer
						// move printer's head to the left margin
					ResetHead();
					pr_status = 241;							// printer connected and ready
																// the rest is done when, e.g., the rest button is pressed
					break;

				default:
					requestRedisplay = false;
					//h = h;
					/* Ignored (unimplemented) cases: 
					   case 0: turn on/off printer
					   case 3: ribbon toggle (between black and red)
					   case 5: enable platens
					   case 6: top-of-form feed
					*/
					break;
			}

			Redisplay = requestRedisplay;
			// modify printer's texture, if required
			//glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, p_width, p_height, GL_RGB, GL_UNSIGNED_BYTE, printer_win);  // modify image
			//glutPostRedisplay();                            // set flag to re-display window
			//glutSetWindow(mainWindow);                      // return control to main window
		}

		public void RenderInitializePrinterHead()
		{
			// initialize printer's head, etc.
			// Called once when we enable the printer
			car_X = left_mar;
			DisplayHead();
			page_top = 560;		// set top of the page to default y=560
			pr_op_code = 0;		// no printer operation requested (needed for page
			// movement animation)
		}

		public void StorePage()
		{
			// When printer connected, click the printer icon while holding SHIFT

			// The idea of this is to write the contents of the printer
			// out to a file that could be read by other APL or unicode files

			// NOTE: The page data does not have the spaces
			// And the line continuation char will have to be handled
			if (page == null)
			{
				return;
			}

			var p = page;
			var lastY = page.y;
			var lastX = page.x;
			while (p != null)
			{
				var xOffset = p.x - lastX;
				if (xOffset > 12)
				{
					var spaces = xOffset / 12;
					for (var i = 0; i < spaces; i++)
					{
						Console.Write(' ');
					}
				}
				Console.Write(Convert.ToChar(p.prData));

				if( p.NewLine)
				{
					Console.WriteLine();
				}

				lastX = p.x;
				p = p.next;
			}
		}
	}
}
