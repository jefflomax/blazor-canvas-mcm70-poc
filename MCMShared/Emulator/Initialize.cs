using System;
using System.IO;
using System.Reflection;

namespace MCMShared.Emulator
{
	public class Initialize
	{
		// ROMS
		public byte[] Rom6k { get; set; }
		public byte[] Rom { get; set; }

		// Main Window
		public byte[] Panel { get; set; }
		public byte[] TapeEO { get; set; }
		public byte[] TapeEC { get; set; }
		public byte[] TapeLO { get; set; }
		public byte[] TapeLC { get; set; }
		public byte[] SpinLeft { get; set; }
		public byte[] SpinRight { get; set; }
		public byte[] SpinStop { get; set; }
		// Printer Window
		public byte[] PrinterWin { get; set; }
		public byte[] PrErrorOn { get; set; }
		public byte[] PrErrorOff { get; set; }


		// Fonts (Printer)
		public AplFont[] AplFonts { get; set; }

		protected Assembly _assembly;

		public Initialize()
		{
			_assembly = null;
		}

		public virtual void SetAssembly(Assembly assembly)
		{
			_assembly = assembly;
		}

		public void InitAll()
		{
			InitRoms();
			InitFonts();
		}

		public void InitRoms()
		{
			InitRom6K();
			InitRom();
		}

		public virtual void InitFonts()
		{
		}

		public virtual void InitRom6K()
		{
		}

		public virtual void InitRom()
		{
		}
#if false
		public void InitAll()
		{
			var assembly = typeof(Program).GetTypeInfo().Assembly;

			InitRoms(assembly);
			InitImages(assembly);
		}

		private void InitRoms(Assembly assembly)
		{
			Rom6k = new byte[0x1800]; //6144
			ReadResource(assembly, @"MCM70.ROM.ROM6k", Rom6k);

			Rom = new byte[0x8000];
			ReadResource(assembly, @"MCM70.ROM.ROMs0-C", Rom);
		}
#endif

		protected void ReadResource
		(
			string resourceName,
			byte[] data
		)
		{
			using var stream = _assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				return;
			}
			using var streamReader = new StreamReader(stream);
			ReadRomStream(streamReader, data);
		}
#if false
		private static void ReadRom(string fileName, byte[] data)
		{
			var r = 0;
			using (var streamReader = File.OpenText(fileName))
			{
				r = ReadRomStream(streamReader, data);
			}
			//Console.WriteLine($"Read ROM {fileName} of {r:N0} bytes.");
		}
#endif
		private static int ReadRomStream
		(
			StreamReader streamReader,
			byte[] data
		)
		{
			var r = 0;
			string s;
			while ((s = streamReader.ReadLine()) != null)
			{
				if (s.Length == 0)
				{
					break;
				}
				var span = s.AsSpan(); // No substring allocations
				for (var i = 0; i < 16; i++)
				{
					data[r++] = ToByte(span.Slice(i * 3, 2));
				}
			}
			return r;
		}

		private static byte ToByte(ReadOnlySpan<char> span)
		{
			return (byte)(ToHex(span[0]) * 16 + ToHex(span[1]));
		}

		private static int ToHex(char ch)
		{
			return (ch <= '9')
				? ch - '0'
				: ch - 'A' + 10;
		}
#if false
		private void InitImages(Assembly assembly)
		{
			Panel = ReadImageResource(assembly, "MCM70.images.panel.data");

			//------------- read tape images ------------------------
			TapeEO = ReadImageResource(assembly, "MCM70.images.tape_empty_opened.data");

			TapeEC = ReadImageResource(assembly, "MCM70.images.tape_empty_closed.data");

			TapeLO = ReadImageResource(assembly, "MCM70.images.tape_loaded_opened.data");

			TapeLC = ReadImageResource(assembly, "MCM70.images.tape_loaded_closed.data");

			SpinLeft = ReadImageResource(assembly, "MCM70.images.spin_left.data");

			SpinRight = ReadImageResource(assembly, "MCM70.images.spin_left.data");

			SpinStop = ReadImageResource(assembly, "MCM70.images.spin_stop.data");


			// read printer's image
			PrinterWin = ReadImageResource(assembly, "MCM70.images.printer.data");

			// read printer's error "on" image
			PrErrorOn = ReadImageResource(assembly, "MCM70.images.pr_error_on.data");

			// read printer's error "off" image
			PrErrorOff = ReadImageResource(assembly, "MCM70.images.pr_error_off.data");


			var fonts = ReadImageResource(assembly, "MCM70.APL_F.apl_fonts.data");
			AplFonts = new AplFont[12];
			var b = 0;
			for (var i = 0; i < AplFonts.Length; i++)
			{
				var font = new byte[3888];
				for (var j = 0; j < 3888; j++)
				{
					font[j] = fonts[b++];
				}
				AplFonts[i].Font = font;
			}
		}

		private static byte[] ReadImageResource
		(
			Assembly assembly,
			string resourceName
		)
		{
			using var stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				throw new Exception($"Missing {resourceName}");
			}
			using var binaryReader = new BinaryReader(stream);
			return binaryReader.ReadBytes((int)stream.Length);
		}
#endif
		public void ProcessFonts(byte[] fonts)
		{
			AplFonts = new AplFont[12];
			var b = 0;
			for (var i = 0; i < AplFonts.Length; i++)
			{
				var font = new byte[3888];
				for (var j = 0; j < 3888; j++)
				{
					font[j] = fonts[b++];
				}
				AplFonts[i].Font = font;
			}
		}
	}
}
