
namespace MCMShared.Emulator
{
	public class PrinterMouse
	{
		private readonly Printer _printer;
		public PrinterMouse(Printer printer)
		{
			_printer = printer;
		}

		//------------------------------------------------------------------ 
		// mouse_clicks in printer's window: handler for mouse clicks
		//------------------------------------------------------------------
		public void MouseClick(bool isLeftButton, bool isPressed, float fx, float fy)
		{
			int sp = 16;					// =default line spacing
			int x = (int) fx;
			int y = (int) fy;

			// ON LEFT BUTTON's PRESS
			//glutSetWindow(window2);      // make printer's window active

			if (isPressed && isLeftButton && (y > 660) && (y < 680))
			{
				// page UP
				if ((x > 385) && (x < 430))
				{
					_printer.DisplayPage(0, sp);
				}

				// page DOWN
				if ((x > 440) && (x < 485))
				{
					_printer.DisplayPage(1, sp);
				}

				// NEW page/reset
				if ((x > 485) && (x < 540))
				{
					_printer.BlankBlock(12, 0, 920, 614, 80);			// paint tray background 
					_printer.BlankBlock
					(
						12,
						Printer.page_start,
						920,
						Printer.page_bottom - Printer.page_start,
						255
					);	// paint page "white"

					_printer.ClearPage();

					// reset printer
#if false
					pr_status = (pr_status & 0xFB);          // rest printer status to "paper in" (bit 2 set to 0)
					page_top = page_start;
					blank_block(car_X, head_Y, 12, 12, 124);  // blank current head position
					car_X = left_mar;
					display_head();                 // display head 
					printer_restore();               // display printer error off image
#endif
					_printer.ResetPrinter();
				}
			}

			//glBindTexture(GL_TEXTURE_2D, texturePrinter); // work with printer's window
			//glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, p_width, p_height, GL_RGB, GL_UNSIGNED_BYTE, printer_win);  // modify texture
			//glutPostRedisplay();                          // do all the above 
			//glutSetWindow(mainWindow);                    // return control to main window
		}

	}
}
