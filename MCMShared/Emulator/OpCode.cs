using MCMShared.Extensions;
using System;
using System.Diagnostics;

namespace MCMShared.Emulator
{
	[DebuggerDisplay("{Op} {Length,d} {Hint} {References}")]
	public class OpCode
	{
		public enum Mask : byte
		{
			None = 0,
			OneThruThree = 0x07,
			TwoThruFour = 0x0E,
			FourThruSix = 0x38,
			FiveSix = 0x30,
			FiveThruSeven = 0x70,
			OneThruSix = 0x3F,
			ACC = 0xFF // SEVERAL opcodes target ACC and have no 000 mask
		}

		// TODO: opcode needs s and d bitmasks
		private Cpu _cpu;
		private Registers _registers;
		public byte Instruction { get;}
		public string Op { get; }
		public string Hint { get; }
		public string Format { get; }
		public int Length { get; }
		public Mask SourceMask { get; }
		public Mask DestMask { get; }
		public int References { get; set; } // Unused
		public OpCode(byte opcode, string op, int length, string format, Cpu cpu)
			: this(opcode, op, length, format, cpu, Mask.None, Mask.None, string.Empty)
		{
		}
		public OpCode(byte opcode, string op, int length, string format, Cpu cpu, string hint)
			: this(opcode, op, length, format, cpu, OpCode.Mask.None, OpCode.Mask.None, hint)
		{
		}

		public OpCode
		(
			byte opcode,
			string op,
			int length,
			string format,
			Cpu cpu,
			Mask dest,
			Mask source,
			string hint
		)
		{
			_cpu = cpu;
			_registers = cpu.regs;
			Instruction = opcode;
			Op = op;
			Length = length;
			References = 0;
			Hint = hint;
			Format = format;
			DestMask = dest;
			SourceMask = source;

			if(dest != Mask.None)
			{
				if( dest == Mask.FourThruSix)
				{
					var dOffset = opcode.AndShiftRight((int)Mask.FourThruSix, 3);
					Dest = dOffset;
				}
			}
		}

		public void SetSD(int source, int destReg, int address)
		{
			//Source = source;
			//Dest = destReg;
			Address = address;
		}
		public int Source { get; set; }
		public int Dest { get; set; }
		public int Address { get; set; } // Only working if set, try to read automatically
		public byte MemoryRead { get; set; }

		public string Summary()
		{
			var op = Instruction;
			return $"{Instruction:X2} {op.ToBinary()} {Op} {Format} {Hint}";
		}

		public string Formatted(int address)
		{

			return string.Format
			(
				Format,
				address, //0 (IP Address)
				Op,         //1
				// This was supposed to be the destination address
				// yet unless the CPU continues updating it, we can't
				// easily tell if it was taken or not (conditionals)
				Address, //_cpu.StackAddressUnwatched,    //2
				Source,     //3
				Dest,       //4
				_registers.regA, //5
				_registers.regB, //6 6
				_registers.regC, //7
				_registers.regD, //8
				_registers.regE, //9
				_registers.M.regH, //10
				_registers.M.regL, //11
				Hint,        //12
							 // TODO: 14bit
				_registers.M.regH * 256 + _registers.M.regL, // 13
				MemoryRead, // 14
				_cpu.StackPointerUnwatched //15
			);
		}

		public static void SetOpcodes(Cpu cpu, OpCode[] Opcodes)
		{
			string IntToReg(int i)
			{
				switch (i)
				{
					case 0:
						return "A";
					case 1:
						return "B";
					case 2:
						return "C";
					case 3:
						return "D";
					case 4:
						return "E";
					case 5:
						return "H";
					case 6:
						return "L";
					case 7:
						return "M";
				}
				return "XXXXXXX";
			}
			string IntToFmtOrd(int i)
			{
				//A5,B6,C7,D8,E9,H10,L11
				return (i + 5).ToString();
			}

			void FlipFlopInstructions
			(
				byte signature,
				string mnemonic,
				OpCode[] Opcodes,
				int length = 3
			)
			{
				var ff = string.Empty;
				byte opcode = 0;
				for (int i = 0; i < 3; i++)
				{
					ff = FlipFlop[i];
					opcode = (byte)(signature + (i << 3));
					if (Opcodes[opcode]!= null)
					{
						throw new Exception("ICK");
					}
					// 2 = Address (stack)
					// 15 = Stack Pointer
					// ??? How do we get the inline jump address?
					var opc = new OpCode(opcode, mnemonic+ff, length,
						"{0:X4} {1} ${2:X4} {15}", cpu, $"{mnemonic} {ff}");
					Opcodes[opcode] = opc;
					//WriteLine($"{opcode:X2} {opcode.ToBinary()} {opc.Op} {opc.Format} {opc.Hint}");
				}
			}

			// 2 Stack Address (changed, verify)
			// 3 Source
			// 4 Dest
			// 5--9 RegA-E
			// 10 RegH
			// 11 RegL
			// 12 Hint
			// 13 M (HL)
			// 14 MemoryRead
			// 15 StackPointer

			// 02 000 00 010 RLC
			// 0A 000 01 010 RRC
			// 12 000 10 010 RAL
			// 1A 000 11 010 RAR
			var orOpCodes = new (byte op, string code, string hint)[]
			{
				(0x02, "RLC","No carry"),
				(0x0A, "RRC","No carry"),
				(0x12, "RAL","Thru carry"),
				(0x1A, "RAR","Thru carry")
			};
			foreach (var orOpCode in orOpCodes)
			{
				Opcodes[orOpCode.op] = new OpCode(orOpCode.op, orOpCode.code, 1,
					"{0:X4} {1} ({5:X2})", cpu, orOpCode.hint);
				//var op = Opcodes[orOpCode.op];
				//WriteLine(op.Summary());
			}

			Opcodes[0x06] = new OpCode(0x06, "LAI", 2,
				"{0:X4} {1} (#{14:X2})", cpu, "Load Acc #");
			Opcodes[0x07] = new OpCode(0x07, "RET", 1,
				"{0:X4} {1} {15}", cpu, "Uncond RET");

			// 03 000 00 011 RFc Carry Clear (Return IF)
			// 0B 000 01 011 RFc Zero Clear
			// 13 000 10 011 RFc Sign Clear
			FlipFlopInstructions(0x03, "RF", Opcodes, length: 1);

			// 04 0000 0100 ADI Immediate (No Carry)
			// 80 1000 0SSS ADr
			// 87 1000 0111 ADM
			Opcodes[0x04] = new OpCode(0x04, "ADI", 2,
				"{0:X4} {1} (#{14:X2}) A={5:X2}", cpu, "ADD #");

			// 0C 00001100 ACI
			Opcodes[0x0C] = new OpCode(0x0C, "ACI", 2,
				"{0:X4} {1} (#{14:X2}) A={5:X2}", cpu, "ADC #");

			// INr 00DDD000 Decrement Index Register
			for (var d = 1; d < 7; d++)
			{
				// Cannot INC A or M
				string desc;
				string hint;
				string format;
				desc = $"INr d=={IntToReg(d)}";
				hint = $"INC {IntToReg(d)}";
				format = "{0:X4} {1} {" + IntToFmtOrd(d) + ":X2}";
				byte opcode = (byte)((d << 3) + 0x0);
				Opcodes[opcode] = new OpCode
				(
					opcode,
					desc, 1, format, cpu,
					OpCode.Mask.FourThruSix, OpCode.Mask.None,
					hint
				);
				//WriteLine($"{opcode:X2} {Convert.ToString(opcode, 2).PadLeft(8, '0')} {desc} {format} {hint}");
			}

			// DCr 00DDD001 Decrement Index Register
			for (var d = 1; d < 7; d++)
			{
				// Cannot DEC A or M
				string desc;
				string hint;
				string format;
				desc = $"DCr d=={IntToReg(d)}";
				hint = $"DEC {IntToReg(d)}";
				format = "{0:X4} {1} {" + IntToFmtOrd(d) +":X2}";
				byte opcode = (byte)((d << 3) + 0x1);
				Opcodes[opcode] = new OpCode
				(
					opcode,
					desc, 1, format, cpu,
					OpCode.Mask.FourThruSix, OpCode.Mask.None,
					hint
				);
				//WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
			}

			// RST 00AAA101 call low memory subroutine at AAA000 (High Byte 0)
			// 05 00000101 RST $00
			// 0D 00001101 RST $08
			// 15 00010101 RST $10
			// 1D 00011101 RST $18
			// 25 00100101 RST $20
			// 2D 00101101 RST $28
			// 35 00110101 RST $30
			// 3D 00111101 RST $38
			for (var a = 0; a < 8; a++)
			{
				byte address = (byte)(a << 3);
				string desc;
				string hint;
				string format;
				desc = $"RST ${(a<<3):X2}";
				hint = $"Call SR ${address.ToBinary()}";
				format = "{0:X4} {1}";
				byte opcode = (byte)((a << 3) + 0x5);
				Opcodes[opcode] = new OpCode
				(
					opcode,
					desc, 1, format, cpu,
					OpCode.Mask.None, OpCode.Mask.None, // TODO: Mask for this call?
					hint
				);
				// WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
			}

			// SUI 14 00010100 SUBtract no borrow 
			// SUR 90 10010SSS 90-96
			// SUM 97 10010111
			Opcodes[0x14] = new OpCode(0x14, "SUI", 2,
				"{0:X4} {1} (#{14:X2}) A={5:X2}", cpu, "SUB # - A");

			// SBI 1C 00011100
			// SBR 98 10011SSS 98-9E
			// SBM 9F 10100111
			Opcodes[0x1C] = new OpCode(0x1C, "SBI", 2,
				"{0:X4} {1} (#{14:X2}) A={5:X2}", cpu, "SUB #+C - A");

			// NDI 24 00100100
			// NDR A0 10100SSS A0-A7
			// NDM A7 10100111
			Opcodes[0x24] = new OpCode(0x24, "NDI", 2,
				"{0:X4} {1} d{5} (#{14:X2})", cpu, "AND #");

			// 23 001 00 011 RTc Carry True
			// 2B 001 01 011 RTc Zero True
			// 33 001 10 011 RTc Sign True
			FlipFlopInstructions(0x23, "RT", Opcodes, length: 1);

			// XRI 2C 0010 1100
			// XRR A8 1010 1SSS A8-AF
			// XRM AF 1010 1111
			Opcodes[0x2C] = new OpCode(0x2C, "XRI", 2,
				"{0:X4} {1} d{4} (#{14:X2})", cpu, "XOR #");

			// ORI 34 0011 0100
			// ORR B0 1011 0SSS B0-B8
			// ORM B7 1011 0111
			Opcodes[0x34] = new OpCode(0x34, "ORI", 2,
				"{0:X4} {1} d{5} (#{14:X2})", cpu, "OR #");

			// CPI 3C 0011 1100 
			// CPR B8 1011 1SSS B8-BE
			// CPM BF 1011 1111
			Opcodes[0x3C] = new OpCode(0x3C, "CPI", 2,
				"{0:X4} {1} d{4} (#{14:X2})", cpu, "CPI #");

			// MVI d Load register d with data
			// 00 DDD 110
			// 06 00000110 MVI d==A
			// 0E 00001110 MVI d==B
			// 16 00010110 MVI d==C
			// 1E 00011110 MVI d==D
			// 26 00100110 MVI d==E
			// 2E 00101110 MVI d==H
			// 36 00110110 MVI d==L
			// 3E 00111110 MVI d==M
			for (var d = 0; d<8; d++)
			{
				string desc;
				string hint;
				string format;
				if (d != 7)
				{
					desc = $"MVI d=={IntToReg(d)}";
					hint = $"{IntToReg(d)}";
					format = "{0:X4} {1} ({" +
						IntToFmtOrd(d) + ":X2})";
				}
				else
				{
					desc = $"MVI d=={IntToReg(d)}";
					hint = $"{IntToReg(d)} == [M]";
					format = "{0:X4} {1} ({" + IntToFmtOrd(d) + ":X2})==({14:X2})";
				}
				byte opcode = (byte)((d<<3) + 0x6);
				Opcodes[opcode] = new OpCode
				(
					opcode,
					desc, 1, format, cpu,
					OpCode.Mask.None, OpCode.Mask.OneThruThree,
					hint
				);
				// WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
			}

			// 0x34 ORI  ^^^
			Opcodes[0x36] = new OpCode(0x36, "LLI", 2,
				"{0:X4} {1} (#{11:X2})", cpu, "Immediate");
			// 0x03C CPI ^^^
			Opcodes[0x3E] = new OpCode(0x3E, "LMI", 2,
				"{0:X4} {1} {13:X4} (#{14:X2})", cpu, "Immed to mem[M]");

			//    0100MMM1 Read port MMM into A
			// 43 01000011 MCM/IO, INP
			// ??? 0x43 resets devices, but it's a port read.
			/*
				41 01000001 INP 0 {0:X4} {1} MCM Port Key Requested
				43 01000011 INP 1 {0:X4} {1} MCM Port
				45 01000101 INP 2 {0:X4} {1} MCM Port
				47 01000111 *INP 3 {0:X4} {1} MCM Port
				49 01001001 *INP 4 {0:X4} {1} MCM Port
				4B 01001011 *INP 5 {0:X4} {1} MCM Port
				4D 01001101 *INP 6 {0:X4} {1} MCM Port
				4F 01001111 *INP 7 {0:X4} {1} MCM Port
				51 01010001 *OUT 0 1 {0:X4} {1} MCM Port BANK A
				53 01010011 OUT 1 1 {0:X4} {1} MCM Port
				55 01010101 OUT 2 1 {0:X4} {1} MCM Port
				57 01010111 OUT 3 1 {0:X4} {1} MCM Port
				59 01011001 *OUT 4 1 {0:X4} {1} MCM Port
				5B 01011011 *OUT 5 1 {0:X4} {1} MCM Port
				5D 01011101 *OUT 6 1 {0:X4} {1} MCM Port
				5F 01011111 *OUT 7 1 {0:X4} {1} MCM Port
				61 01100001 *OUT 0 2 {0:X4} {1} MCM Port
				63 01100011 *OUT 1 2 {0:X4} {1} MCM Port
				65 01100101 *OUT 2 2 {0:X4} {1} MCM Port
				67 01100111 *OUT 3 2 {0:X4} {1} MCM Port
				69 01101001 *OUT 4 2 {0:X4} {1} MCM Port
				6B 01101011 *OUT 5 2 {0:X4} {1} MCM Port
				6D 01101101 *OUT 6 2 {0:X4} {1} MCM Port
				6F 01101111 *OUT 7 2 {0:X4} {1} MCM Port
				71 01110001 *OUT 0 3 {0:X4} {1} MCM Port
				73 01110011 *OUT 1 3 {0:X4} {1} MCM Port
				75 01110101 *OUT 2 3 {0:X4} {1} MCM Port
				77 01110111 *OUT 3 3 {0:X4} {1} MCM Port
				79 01111001 *OUT 4 3 {0:X4} {1} MCM Port
				7B 01111011 *OUT 5 3 {0:X4} {1} MCM Port
				7D 01111101 *OUT 6 3 {0:X4} {1} MCM Port
				7F 01111111 OUT 7 3 {0:X4} {1} MCM Port
			 */
			var verifiedInOut = new byte[] { 0x41, 0x43, 0x45, 0x53, 0x55, 0x57, 0x7F };
			for (var r = 0; r <4; r++)
				for (var m = 0; m < 8; m++)
				{
					string desc = string.Empty;
					string hint;
					string format;
					if (r == 0)
					{
						desc = $"INP {m:X}";
					}
					else
					{
						desc = $"OUT {m:X} {r:X}";
					}
					hint = $"MCM Port";
					format = "{0:X4} {1}";
					byte opcode = (byte)(0x40 + (r << 4)+  (m << 1) + 0x1);
					//if (Array.IndexOf(verifiedInOut, opcode) == -1)
					//{
					//	desc = "*" + desc;
					//}
					Opcodes[opcode] = new OpCode
					(
						opcode,
						desc, 1, format, cpu,
						OpCode.Mask.None, OpCode.Mask.None, // TODO: Mask 1110 for this call?
						hint
					);
					//WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
				}

			// 40 0100 0000  010 00 000 JFc Carry Clear
			// 48 0100 1000  010 01 000 JFc Zero Clear
			// 50 0101 0000  010 10 000 JFc Sign Clear
			FlipFlopInstructions(0x40, "JF", Opcodes );

			//Opcodes[0x40] = new OpCode(0x40, "JFC", 3,
			//    "{0:X4} {1} {2:X4}", cpu, "JMP if carry clear");

			// CF 010 FF 010
			// 42 01000010 CFC
			// 4A 01001010 CFZ
			// 52 01010010 CFS
			FlipFlopInstructions(0x42, "CF", Opcodes);

			Opcodes[0x44] = new OpCode(0x44, "JMP", 3,
				"{0:X4} {1} {2:X4}", cpu, "Absolute");
			Opcodes[0x46] = new OpCode(0x46, "CALL", 3,
				"{0:X4} {1} {2:X4} ({12})", cpu, "unconditional");

			Opcodes[0x51] = new OpCode(0x51, "OUT A", 1,
				"{0:X4} {1} ({2:X4}) {12}", cpu, "BANK A");

			// 60 0110 0000 JTc C
			// 68 0110 1000 JTc Z
			// 70 0111 0000 JTc S
			FlipFlopInstructions(0x60, "JT", Opcodes);

			// 62 0110 0010 CTc C
			// 6A 0110 1010 CTc Z
			// 72 0111 0010 CTc S
			FlipFlopInstructions(0x62, "CT", Opcodes);

			// ADR 1000 0SSS 80 ADD
			// ACR 1000 1SSS 88 ADC
			// SUR 1001 0SSS 90
			// SBR 1001 1SSS 98
			// NDR 1010 0SSS A0
			// XRR 1010 1SSS A8
			// ORR 1011 0SSS B0
			// CPR 1011 1SSS B8
			foreach (var op in new byte[] { 0x80, 0x88, 0x90, 0x98, 0xA0, 0xA8, 0xB0, 0xB8 })
				for (var s = 0; s < 8; s++)
				{
					string mnemonic;
					string mnemonicHint;

					// NDR 111 is M
					string desc;
					string hint;
					string format;
					switch (op)
					{
						case 0x80:
							mnemonic = "AD";
							mnemonicHint = "ADD";
							break;
						case 0x88:
							mnemonic = "AC";
							mnemonicHint = "ADC";
							break;
						case 0x90:
							mnemonic = "SU";
							mnemonicHint = "SUB NoBrw";
							break;
						case 0x98:
							mnemonic = "SB";
							mnemonicHint = "SUB Brw";
							break;
						case 0xA0:
							mnemonic = "ND";
							mnemonicHint = "AND";
							break;
						case 0xA8:
							mnemonic = "XR";
							mnemonicHint = "XOR";
							break;
						case 0xB0:
							mnemonic = "OR";
							mnemonicHint = "OR";
							break;
						case 0xB8:
							mnemonic = "CP";
							mnemonicHint = "CMP";
							break;
						default:
							throw new Exception("Unknown Mnemonic");
					}

					if (s != 7)
					{
						desc = $"{mnemonic}r s={IntToReg(s)}";
						hint = $"{mnemonicHint} {IntToReg(s)}";
						format = "{0:X4} {1} {" + IntToFmtOrd(s) + ":X2}";
					}
					else
					{
						desc = $"{mnemonic}M s={IntToReg(s)}";
						hint = $"{mnemonicHint} {IntToReg(s)}";
						format = "{0:X4} {1} {" + IntToFmtOrd(s) + ":X2} {14:X2}";
					}
					byte opcode = (byte)(op + s);
					Opcodes[opcode] = new OpCode
					(
						opcode,
						desc, 1, format, cpu,
						OpCode.Mask.ACC,  // Dest is always A
						OpCode.Mask.OneThruThree,
						hint
					);
					//WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
				}

			// B0 10110000 ORr , same as NDr

			// MOV d,s
			// 11 D000 S011 (0-A,1-B,2-C,3-D,4-E,5-H,6-L)
			// A5,B6,C7,D8,E9,H10,L11
			// C1 11 D000 S001
			// 11 D000 S100
			// C0 11000000 MOV d,s A<A NOP
			// C1 11000001 MOV d,s A<B
			// C2 11000010 MOV d,s A<C
			// C3 11000011 MOV d,s A<D
			// C4 11000100 MOV d,s A<E
			// C5 11000101 MOV d,s A<H
			// C6 11000110 MOV d,s A<L
			// C7 11000111 MOV d,s A<[M]
			// C8 11001000 MOV d,s B<A
			// CA 11001010 MOV d,s B<C
			// CB 11001011 MOV d,s B<D
			// CC 11001100 MOV d,s B<E
			// CD 11001101 MOV d,s B<H
			// CE 11001110 MOV d,s B<L
			// CF 11001111 MOV d,s B<[M]
			// D0 11010000 MOV d,s C<A
			// D1 11010001 MOV d,s C<B
			// D3 11010011 MOV d,s C<D
			// D4 11010100 MOV d,s C<E
			// D5 11010101 MOV d,s C<H
			// D6 11010110 MOV d,s C<L
			// D7 11010111 MOV d,s C<[M]
			// D8 11011000 MOV d,s D<A
			// D9 11011001 MOV d,s D<B
			// DA 11011010 MOV d,s D<C
			// DC 11011100 MOV d,s D<E
			// DD 11011101 MOV d,s D<H
			// DE 11011110 MOV d,s D<L
			// DF 11011111 MOV d,s D<[M]
			// E0 11100000 MOV d,s E<A
			// E1 11100001 MOV d,s E<B
			// E2 11100010 MOV d,s E<C
			// E3 11100011 MOV d,s E<D
			// E5 11100101 MOV d,s E<H
			// E6 11100110 MOV d,s E<L
			// E7 11100111 MOV d,s E<[M]
			// E8 11101000 MOV d,s H<A
			// E9 11101001 MOV d,s H<B
			// EA 11101010 MOV d,s H<C
			// EB 11101011 MOV d,s H<D
			// EC 11101100 MOV d,s H<E
			// EE 11101110 MOV d,s H<L
			// EF 11101111 MOV d,s H<[M]
			// F0 11110000 MOV d,s L<A
			// F1 11110001 MOV d,s L<B
			// F2 11110010 MOV d,s L<C
			// F3 11110011 MOV d,s L<D
			// F4 11110100 MOV d,s L<E
			// F5 11110101 MOV d,s L<H
			// F7 11110111 MOV d,s L<[M]
			// F8 11111000 MOV d,s [M]<A
			// F9 11111001 MOV d,s [M]<B
			// FA 11111010 MOV d,s [M]<C
			// FB 11111011 MOV d,s [M]<D
			// FC 11111100 MOV d,s [M]<E
			// FD 11111101 MOV d,s [M]<H
			// FE 11111110 MOV d,s [M]<L

			var extraHint = string.Empty;
			for (var d = 0; d < 8; d++)
			{
				for (var s = 0; s < 8; s++)
				{
					if (s == d)
					{
						if (s != 0)
						{
							continue;
						}
						// MOV a,a is NOP
						extraHint = " NOP";
					}
					byte opcode = (byte)(0xC0 + (d << 3) + s);

					string desc = string.Empty;
					string hint = string.Empty;
					string format = string.Empty;
					if (s != 7 && d != 7)
					{
						desc = $"MOV d,s {IntToReg(d)}<{IntToReg(s)}";
						hint = $"{IntToReg(d)}<-{IntToReg(s)}" + extraHint;
						format = "{0:X4} {1} ({" +
							IntToFmtOrd(d) + ":X2}<-{"+
							IntToFmtOrd(s) + ":X2})";
					}

					if (s == 7)
					{
						desc = $"MOV d,s {IntToReg(d)}<[M]";
						hint = $"{IntToReg(d)}<-[M]";
						format = "{0:X4} {1} {" + IntToFmtOrd(d) + ":X2}, ({14:X2})";
					}
					if (d == 7)
					{
						desc = $"MOV d,s [M]<{IntToReg(s)}";
						hint = $"[M]<-{IntToReg(s)}";
						format = "{0:X4} {1} {" + IntToFmtOrd(d) + ":X2}, ({14:X2})";
					}

					Opcodes[opcode] = new OpCode
					(
						opcode,
						desc, 1, format, cpu,
						OpCode.Mask.FourThruSix, OpCode.Mask.OneThruThree,
						hint
					);
					if (extraHint.Length > 0)
					{
						extraHint = string.Empty;
					}
					// WriteLine($"{opcode:X2} {opcode.ToBinary()} {desc} {format} {hint}");
				}
			}

			Opcodes[0xF8] = new OpCode(0xF8, "LMA", 1,
				"{0:X4} {1} ({2:X4}),A ({5:X2})", cpu, "(M) ← A");
			Opcodes[0xFD] = new OpCode(0xFD, "LMs H", 1,
				"{0:X4} {1} ({2:X4}),A ({10:X2})", cpu, "(M) ← H");
		}

		private static string[] FlipFlop = { "C", "Z", "S" };

	}

}
