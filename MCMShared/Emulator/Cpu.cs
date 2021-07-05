using MCMShared.Extensions;
using System;
using System.Diagnostics;
using System.Threading;
using static System.Console;

/*************************************************************************************************

				MCM/70 Emulator

Copyright (c) 2019--, Zbigniew Stachniak

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
**************************************************************************************************/

/**********************************************************************************
 *                       Emulation of MCM 8008 CPU                                *
 * modified fragment of 8008 emulator from Mike Willegal's 8008-disassembler.c at *
 * http://www.willegal.net/scelbi/8008-disassembler.c                             *
 * *******************************************************************************/

/*************************************************************************************************
.NET Core / C# / OpenTK port Copyright (c) 2021 Jeff Lomax
Gratefully acknowledging original work by Zbigniew Stachniak and assistance by OpenTK
community Julius Häger (NogginBops)
All rights identical to those specified by Zbigniew Stachniak
**************************************************************************************************/


namespace MCMShared.Emulator
{
	// byte (unsigned) 256
	// ushort 0-64k Uint16
	// uint 0-4 billion
	// int -2billion to +2billion

	public class Cpu
	{
		private Machine _machine;
		private byte[] _memory;
		private Keyboard _keyboard;
		private Tapes _tapes;
		private Printer _printer;

		private Display _display;

		public OpCode[] Opcodes = new OpCode[256];
		public OpCode opc;
		private bool _showDisassembly;

		public Registers regs;

		private Address[] stack;

		private int _romBank;				// ROM bank number
		private int Bank
		{
			get => _romBank;
			set
			{
				_romBank = value;
				_display.RomBank(value);
			}
		}

		private int _stackPointer;			// stack pointer
		private int Stackptr
		{
			get
			{
				return _stackPointer;
			}
			set
			{
				_stackPointer = value;
				_display.StackPointer(value);
			}
		}
		public int StackPointerUnwatched => _stackPointer;
		public int StackAddressUnwatched => stack[Stackptr].reg;

		private class MemoryWatcher
		{
			private readonly byte[] _memory;
			private readonly Display _display;
			public MemoryWatcher(byte[] memory, Display display)
			{
				_memory = memory;
				_display = display;
			}
			public byte this[int address]
			{
				get
				{
					_display.MemoryRead((ushort)address);
					return _memory[address];
				}
				set
				{
					_memory[address] = value;
					_display.MemoryWrite((ushort)address);
				}
			}
		}

		private MemoryWatcher _memoryWatcher;

		//flags
		private Flag _flagParity;
		private Flag _flagSign;
		private Flag _flagCarry;
		private Flag _flagZero;

		private byte inst;					// currently executed instruction

		byte[] ROM; // Bank switched ROM
		private const ushort RomEnd = 0x1FFF;

		public Cpu(bool showDisassembly)
		{
			regs = new Registers();
			stack = new Address[8];
			_showDisassembly = showDisassembly;

			// TODO:
			// Handle banking w/o copying blocks.
			// Either detect memory read address and
			// offset from correct block
			_romBank = 0;
		}

		public void SetMachine(Machine machine)
		{
			if (machine.Keyboard == null ||
				machine.Printer == null ||
				machine.Display == null ||
				machine.Tapes == null)
			{
				throw new NotSupportedException("Keyboard, Printer, and Tapes must be setup first");
			}

			_machine = machine;
			_keyboard = machine.Keyboard;
			_printer = machine.Printer;
			_tapes = machine.Tapes;
			_display = machine.Display;
			_memory = machine.Memory;
			_memoryWatcher = new MemoryWatcher(_memory, _display);

			OpCode.SetOpcodes(this, Opcodes);
		}

		public void InitMemory(byte[] rom6K, byte[] rom)
		{
			for( var i = 0; i < 0x1800; i++)
			{
				_memory[i] = rom6K[i];
			}

			ROM = rom;
		}

		public void ResetCpu()
		{
			// clear registers and flags
			regs.Clear();

			_flagZero = new Flag();
			_flagCarry = new Flag();
			_flagSign = new Flag();
			_flagParity = new Flag();

			// 0000-17FF ROM6K
			// 1800-1FFF 2K Banked ROM
			// 2000-4000 8K ??? where is 16K

			for (var i=0; i < stack.Length; i++)
			{
				stack[i] = new Address();
			}
			_stackPointer = 0;

			for (var i = 0x2000; i < 0x4000; i++)
			{
				_memory[i] = 0;     // clear RAM
			}
			_machine.RefreshDisplayCounter = 0;
		}
#if false
		private static MemoryPool<byte> pool = MemoryPool<byte>.Shared;
		public void Worker(Memory<byte> buffer)
		{
			var x = buffer.Span.Slice(0, 10);
			//var y = buffer.AsSpan().
		}
		public void InMemorySpan()
		{
			using( IMemoryOwner<byte> rental = pool.Rent(minBufferSize: 0x2000))
			{
				Worker(rental.Memory);
			}
		}
#endif
		public byte GetCurrentInstruction => inst;

		public int RunCpu()
		{
			var halted= 131313; // C# compiler nag

			if (!_machine.McmOn)
			{
				return 0;
			}

			var adr = stack[Stackptr].reg;
			inst = _memory[stack[Stackptr].reg];

			// Much of the decoding could be replaced by just
			// accessing information from this array
			opc = Opcodes[inst];
#if false
			if( opCode != null)
			{
				//WriteLine($"{opCode.Op} {opCode.Length} {opCode.Hint}");
				if( opCode.Op.StartsWith("*"))
				{
					var dummy = 0;
				}

				opCode.SetStart(stack[stackptr],stackptr);
				op = opCode;
			}
			else
			{
				WriteLine($"Unknown OP {inst:X2} {inst.ToBinary()} {Convert.ToString(inst, 8)}");
			}
#endif

			stack[Stackptr].IncrementAndMask(0x3FFF);

			_tapes.TapeMovement(inst);

			var temp = inst.RotateRight(6);

			_display.InstructionPointer(adr);

			/*   call into 8008's four major instruction subgroups for decoding; these subgroups are determined by MS 2 bits and are:
				00  load (mediate), add/subtract (mediate), increment, decrement, 
					 and/or/eor/cmp (mediate), rotate, return, halt
				01  jump, call, input, output
				10  add/subtract (not mediate), and/or/eor/cmp (not mediate)
				11  load (not mediate), halt
			*/

			switch (temp)
			{
				case 0: /* group 00:  INC/DEC/ROTATE, RST/RET and IMMEDIATE OPERATIONS
			LS 3 bits:
				000 increment (bits 3-5 determine reg except 0 = hlt inst)
				001 decrement (bits 3-5 determine reg except 0 = hlt inst)
				010 rotate  (bits 3-5 specify
				000 A left
				001 A right
				010 A left through carry
				011 A right through carry
				1xx ????

				011 conditional return
				100 add/subtract/and/or/eor/cmp mediate
				bits 3-5
				000 add w/o carry
				001 add w carry
				010 subtract w/o borrow
				011 subtract w borrow
				100 and
				101 xor
				110 or
				111 cmp

				 101 RST -call routine in low mem (address = inst & 0x38)
				 110 load mediate(bits 3-5 determine dest)
				 111 unconditional return
			*/
					halted = ImmediatePlus(inst);
					break;
				case 1: /* group 01: I/O, JUMP AND CALL
			LS 3 bits:

				000 jump conditionally
				XX1 input/output
				010 call conditionally
				100 jump
				110 call
			*/
					halted = IoJmpCall(inst);
					break;
				case 2:
					/* group 10:   MATH AND BOOLEAN OPERATIONS
						bits 3-5

						000 add (bit 0-2 determines source)
						001 add w/carry (bit 0-2 determines source)
						010 subtract (bit 0-2 determines source)
						011 subtract w/borrow (bit 0-2 determines source)
						100 and (bit 0-2 determines source)
						101 eor (bit 0-2 determines source)
						110 or (bit 0-2 determines source)
						111 cmp (bit 0-2 determines source)
					*/
					halted = MathBoolean(inst);
					break;
				case 3:
					/* group 11: Case MS 2 bits =  11  LOAD INSTRUCTION
					   XXX load (bits 0-5 determine src/dest,except 0xFF = halt)
					*/
					halted = Load(inst);
					break;
			}


			if(_showDisassembly && opc != null)
			{
				var opStr = opc.Formatted(adr);

				Write(opStr.PadRight(34));
				//Write(regs);
				WriteLine($" [Z {_flagZero} C {_flagCarry} S {_flagSign}]");
		 	}

			return halted;
		}

		private int ImmediatePlus(byte instr)
		{
			// LOAD do not affect flags
			byte bReg;
			int iResult;
			byte bResult;
			ushort sdata1, sdata2, adr;

			/* first check for halt */
			if ((instr & 0xfe) == 0x0)
			{
				WriteLine($"halt {instr:X2}");
				return 1;
			}

			var bOp = instr.And(0x07);
			switch (bOp)
			{
				case 0:  /* increment */
					bReg = instr.AndShiftRight(0x38, 3);
					bResult = (byte)(regs[bReg] + 1); // C# any add results in int
					SetFlagsInc(bResult);
					regs[bReg] = bResult;
					break;
				case 1: /* decrement */
					bReg = instr.AndShiftRight(0x38, 3);
					bResult = (byte)(regs[bReg] - 1);
					SetFlagsInc(bResult);
					regs[bReg] = bResult;
					break;
				case 2: /* rotate */
					bOp = instr.AndShiftRight(0x38, 3);
					switch (bOp)
					{
						case 0: /* rotate left */
							iResult = regs.regA << 1;
							regs.regA = (byte)(((iResult >> 8) | iResult) & 0xff);
							_flagCarry.Value = (byte)( regs.regA & 1 );
							break;
						case 1: /* rotate right */
							_flagCarry.Value = (byte)(regs.regA & 1);
							iResult = regs.regA >> 1;
							regs.regA = (byte)(((regs.regA << 7) | iResult) & 0xff);
							break;
						case 2: /* rotate left through carry */
							iResult = regs.regA << 1 | _flagCarry.Value;
							_flagCarry.Value = (byte)(regs.regA >> 7);
							regs.regA = (byte)(iResult & 0xff);
							break;
						case 3: /* rotate right through carry */
							iResult = regs.regA >> 1 | (_flagCarry.Value << 7);
							_flagCarry.Value = (byte)(regs.regA & 0x1);
							regs.regA = (byte)(iResult & 0xff);
							break;
						default: /*  4-7 = ??? */
							break;
					}
					break;
				case 3: /* conditional return */
					var cc = ChkConditional(instr); /* condition code to check, if conditional operation */
					if (cc)
					{
						//   & 0x7 to guarantee taking care of stack overflow (circular behavior)
						Stackptr = (Stackptr - 1) & 0x7;
					}
					break;
				case 4: /* math with mediate operands */
					iResult = 0XAAAA; // Just for C# nag
					var carryzero = false;

					sdata2 = (byte)regs.regA;  /* reg A is source and target */
					sdata1 = (byte)_memory[stack[Stackptr].reg]; /* source is mediate */

					stack[Stackptr].IncrementAndMask(0x3FFF);

					sdata1 = (byte)(sdata1 & 0xff);
					this.opc.MemoryRead = (byte)sdata1;
					sdata2 = (byte)(sdata2 & 0xff);

					bOp = instr.ShiftRightAnd(3, 0x07);
					switch (bOp)
					{
						case 0: /* add without carry, but set carry */
							iResult = sdata1 + sdata2;
							regs.regA = (byte) iResult;
							carryzero = false;
							break;
						case 1: /* add with carry, and set carry */
							iResult = sdata1 + sdata2 + _flagCarry.Value;
							regs.regA = (byte) iResult;
							carryzero = false;
							break;
						case 2: /* subtract with no borrow, but set borrow */
							iResult = sdata2 - sdata1;
							regs.regA = (byte) iResult;
							carryzero = false;
							break;
						case 3: /* subtract with borrow, and set borrow */
							iResult = sdata2 - sdata1 - _flagCarry.Value;
							regs.regA = (byte) iResult;
							carryzero = false;
							break;
						case 4: /* logical and */
							iResult = sdata1 & sdata2;
							regs.regA = (byte) iResult;
							carryzero = true;
							break;
						case 5: /* exclusive or */
							iResult = sdata1 ^ sdata2;
							regs.regA = (byte) iResult;
							carryzero = true;
							break;
						case 6: /* or */
							iResult = sdata1 | sdata2;
							regs.regA = (byte) iResult;
							carryzero = true;
							break;
						case 7: /* compare (assume no borrow for now) */
							iResult = sdata2 - sdata1;
							carryzero = false;
							break;
					}
					SetFlags(iResult, carryzero);
					break;
				case 5:  /* RST (call function in low memory) */
					//   & 0x7 to guarantee taking care of stack overflow (circular behavior)
					// TODO: This is an unusual op that might deserve it's own blinking light
					Stackptr = (Stackptr + 1) & 0x07;
					stack[Stackptr].reg = (ushort)((instr & 0x38) & 0x3FFF);
					break;
				case 6: /* load mediate */
					sdata1 = _memoryWatcher[stack[Stackptr].reg]; /* source is mediate */

					stack[Stackptr].IncrementAndMask(0x3FFF);

					bReg = instr.ShiftRightAnd(3, 0x07);
					if (bReg == 7)
					{
						adr = regs.M14Bit;  // destination address in memory

						if ((adr > RomEnd) && (_memory[adr] != sdata1))
						{
							_memoryWatcher[adr] = (byte) sdata1;

							// if writing something new to display memory, then set the refresh_display flag
							// to allow display refreshing when called
							if ((adr > 0x2020) && (adr < 0x20FF))
							{
								// increase refresh counter; when it reaches 7,
								// SS is refreshed
								_machine.RefreshDisplayCounter++;
							}
						}
					}
					else
					{
						regs[bReg] = (byte)sdata1;
					}
					break;
				case 7: /* unconditional return */
					//   & 0x7 to guarantee taking care of stack overflow (circular behavior)
					Stackptr = (Stackptr - 1) & 0x07;
					break;
			}
			return 0;
		}

		private int IoJmpCall(byte instr)
		{
			bool cc; /* condition code to check, if conditional operation */
			ushort addrL, addrH;

			ushort op = instr.And( 0x07 );
			switch (op)
			{
				case 0: /* jump conditionally */
					cc = ChkConditional(instr);
					addrL = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					addrH = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					this.opc.Address = 0;
					if (cc)
					{
						stack[Stackptr].reg = (ushort)((addrL + (addrH << 8)) & 0x3FFF);
						this.opc.Address = stack[Stackptr].reg;
					}
					break;
				case 2: /* call conditionally */
					cc = ChkConditional(instr);
					// advances regardless of branch
					addrL = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					addrH = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					if (cc)
					{
						/* now bump stack to use new pc and save it */
						Stackptr = (Stackptr + 1) & 0x7;     //   & 0x7 to guarantee taking care of stack overflow (circular behavior)
						// TODO: DEL this.opc.Machine.StackPointer = Stackptr;

						stack[Stackptr].reg = (ushort)((addrL + (addrH << 8)) & 0x3fff);
					}
					break;
				case 4: /* jump */
					addrL = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);
					addrH = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].reg = (ushort) ((addrL + (addrH << 8)) & 0x3fff);

					this.opc.Address = stack[Stackptr].reg;

					break;
				case 6: /* call */
					addrL = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					addrH = (ushort)_memory[stack[Stackptr].reg];
					stack[Stackptr].IncrementAndMask(0x3FFF);

					/* now bump stack to use new pc and save it */
					Stackptr = (Stackptr + 1) & 0x07;   //   & 0x7 to guarantee taking care of stack overflow (circular behavior)

					stack[Stackptr].reg = (ushort)((addrL + (addrH << 8)) & 0x3FFF);
					this.opc.Address = stack[Stackptr].reg;

					break;
				default: // MCM/70 IO and reset instructions
					MCM_IO(instr);
					break;
			}
			return 0;
		}

		private int MathBoolean(byte instr)
		{
			bool carryZero = false;
			ushort sdata1, sdata2;
			ushort result = 0xFFFF;

			var src = instr.And(0x07);
			if (src == 0x07)
			{
				sdata1 = _memoryWatcher[regs.M14Bit];
			}
			else
			{
				sdata1 = regs[src];
			}
			sdata1 = (ushort)(sdata1 & 0xff);
			sdata2 = regs.regA;  /* reg A is other source and usually target */

			var bOp = instr.ShiftRightAnd(3, 0x07);
			switch (bOp)
			{
				case 0: /* add without carry, but set carry */
					result = (ushort)( sdata1 + sdata2 );
					regs.regA = (byte) result;
					carryZero = false;
					break;

				case 1: /* add with carry, and set carry */
					result = (ushort)(sdata1 + sdata2 + _flagCarry.Value);
					regs.regA = (byte) result;
					carryZero = false;
					break;

				case 2: /* subtract with no borrow, but set borrow */
					result = (ushort)(sdata2 - sdata1);
					regs.regA = (byte) result;
					carryZero = false;
					break;

				case 3: /* subtract with borrow, and set borrow */
					result = (ushort)(sdata2 - sdata1 - _flagCarry.Value);
					regs.regA = (byte) result;
					carryZero = false;
					break;

				case 4: /* logical and */
					result = (ushort)(sdata1 & sdata2);
					regs.regA = (byte) result;
					carryZero = true;
					break;

				case 5: /* exclusive or */
					result = (ushort)(sdata1 ^ sdata2);
					regs.regA = (byte) result;
					carryZero = true;
					break;

				case 6: /* or */
					result = (ushort)(sdata1 | sdata2);
					regs.regA = (byte) result;
					carryZero = true;
					break;

				case 7: /* compare (assume no borrow for now) */
					result = (ushort)(sdata2 - sdata1);
					carryZero = false;
					break;
			}
			SetFlags(result, carryZero);
			return 0;
		}

		private void SetFlags(int result, bool carryZero)
		{
			byte bResult = (byte)(result & 0xFF);
			if (bResult != 0) // Any on bits
			{
				_flagZero.State = false;
				if ((result & 0x80) == 0x80)
				{
					_flagSign.State = true;
				}
				else
				{
					_flagSign.State = false;
				}
			}
			else
			{
				_flagSign.State = false;
				_flagZero.State = true;
			}
			if (carryZero)
			{
				_flagCarry.State = false;
			}
			else if (result > 255 || result < 0)
			{
				_flagCarry.State = true;
			}
			else
			{
				_flagCarry.State = false;
			}
			SetParity(bResult);
		}

		private void SetFlagsInc(byte result)
		{
			if ((result & 0xff) != 0 ) // Any bit on?
			{
				_flagZero.State = false;
				if ((result & 0x80) == 0x80)
				{
					_flagSign.State = true;
				}
				else
				{
					_flagSign.State = false;
				}
			}
			else
			{
				_flagSign.State = false;
				_flagZero.State = true;
			}
			SetParity(result);
		}

		private readonly bool[] _parity =
		{
			true, false, false, true, false, true, true, false, false, true,
			true, false, true, false, false, true, false, true, true, false,
			true, false, false, true, true, false, false, true, false, true,
			true, false, false, true, true, false, true, false, false, true,
			true, false, false, true, false, true, true, false, true, false,
			false, true, false, true, true, false, false, true, true, false,
			true, false, false, true, false, true, true, false, true, false,
			false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true,
			true, false, true, false, false, true, true, false, false, true,
			false, true, true, false, false, true, true, false, true, false,
			false, true, false, true, true, false, true, false, false, true,
			true, false, false, true, false, true, true, false, false, true,
			true, false, true, false, false, true, true, false, false, true,
			false, true, true, false, true, false, false, true, false, true,
			true, false, false, true, true, false, true, false, false, true,
			true, false, false, true, false, true, true, false, false, true,
			true, false, true, false, false, true, false, true, true, false,
			true, false, false, true, true, false, false, true, false, true,
			true, false, true, false, false, true, false, true, true, false,
			false, true, true, false, true, false, false, true, false, true,
			true, false, true, false, false, true, true, false, false, true,
			false, true, true, false, false, true, true, false, true, false,
			false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true,
			true, false, true, false, false, true
		};

		private void SetParity(byte result)
		{
			// This entire method can be a lookup table
			// possibly using the same bit lookup OpenTK uses for key state

			_flagParity.State = _parity[result] ;
#if false
			int i;

			var p = 0;
			_flagParity.Value = 1;
			for (i = 0; i < 8; i++)
			{
				//flagParity = flagParity ^ ((result>>i) & 1);
				_flagParity.Value = (byte)(_flagParity.Value ^ ((result >> i) & 1));
				//if (result & (1<<i))
				if ( (result & (1 << i)) != 0)
				{
					p++;
				}
			}

			if ((p & 1) == 1)
			{
				_flagParity.State = false;
			}
#endif
		}

		private int Load(byte instr)
		{
			byte data;

			var src = instr.And(0x07);
			ushort dest = instr.ShiftRightAnd(3, 0x07);
			var adr = regs.M14Bit;

			if (src == 0x7 && dest == 0x7)
			{   /* this is HALT instruction  */
				WriteLine($"halt {instr:X2}");
				return 1;
			}
			if (src == 0x7)
			{   /* data comes from memory   */
				data = _memoryWatcher[adr];
			}
			else
			{   /* data comes from register */
				data = regs[src];
			}
			if (dest == 0x7)	/* load to memory */
			{
				if ((adr > RomEnd) && (_memory[adr] != data))
				{
					_memoryWatcher[adr] = data;
					// if writing something new to display memory, then set the refresh_display flag
					// to allow display refreshing when called
					if ((adr > 0x2020) && (adr < 0x20FF))
					{
						_machine.RefreshDisplayCounter++;
					}
				}
			}
			else				/* load a register */
			{
				regs[dest] = data;
			}
			return 0;
		}

		private bool ChkConditional(byte instr)
		{
			var cc = false;
			// TODO: Some instructions, like CTc, seem to only
			// want AND $18 00011000
			var ccval = instr.AndShiftRight(0x38, 3);

			switch (ccval)
			{
				case 0: /* overflow false */
					cc = !_flagCarry.State;
					break;
				case 1: /* zero false */
					cc = !_flagZero.State;
					break;
				case 2: /* sign false */
					cc = !_flagSign.State;
					break;
				case 3: /* parity false */
					cc = !_flagParity.State;
					break;
				case 4: /* overflow true */
					cc = _flagCarry.State;
					break;
				case 5: /* zero true */
					cc = _flagZero.State;
					break;
				case 6: /* sign true */
					cc = _flagSign.State;
					break;
				case 7: /* parity true */
					cc = _flagParity.State;
					break;
			}
			return (cc);
		}


		// IO instructions
		void MCM_IO(byte instr)
		{
			byte sp;

			switch (instr)
			{
				case 0x41: //  INP 00:  keyboard input
					_keyboard.KeyRequested = true;				// turn keyboard read on
					regs.regA = _keyboard.GetRow(regs.regA);	// collect keyboard input from kbd queue

					break;

				case 0x43: // INP 01 -- system status check and other functions (uses input in register A, and output to A)
					switch (regs.regA)
					{
						case 0: //  check if power on; the value returned in A will be the "BOUNCE" line from the power supply on bit 1
							if (! _machine.Power)
							{
								regs.regA = 2;  // if power down, then return this as status
							}
							break;
						case 1: /*
							Sets the RESET line of Omniport to 1 to force peripherals to perform their initializations
							appropriate for these devices. The RESET signal is sent during poer-up (after START key is pressed)
							or during system reset time.

							Reset cassette drives -- resets USRT by modifying cassettes' status in the following way:
							- Received Over Run: set bit 4 to 0
							- Received Parity Error: set bit 5 to 0
							- transmit buffer empty: set bit 6 to 1
							- clear all receive bits: set bit 7 to 0
						*/
							_tapes.tape0_s.status = (byte)((_tapes.tape0_s.status & 0x0F) | 0x40);
							_tapes.tape1_s.status = (byte)((_tapes.tape1_s.status & 0x0F) | 0x40);

							// reset auxiliary variables for tapes:
							_tapes.tape0_s.speed = 0;
							_tapes.tape1_s.speed = 0;		// tapes not moving
							_tapes.MoveClock = 0;

							// Reset printer, if connected
							if (_printer.PrinterConnected)
							{
/*
								glutSetWindow(window2);                       // make printer's window active
								glBindTexture(GL_TEXTURE_2D, texturePrinter); // work with printer's window
								reset_head();                                // rest the head
								glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, p_width, p_height, GL_RGB, GL_UNSIGNED_BYTE, printer_win);  // modify image
								glutPostRedisplay();                          // set flag to re-display window
								glutSetWindow(mainWindow);                    // return control to main window
*/
								_printer.RenderResetHead = true;
								_printer.pr_status = 241;                              // printer connected and ready
							}

							break;
						case 2: // Turn off computer (e.g.[]OFF or on power fail): clear RAM, registers, etc.
							_machine.ResetMCM();
							break;
						case 4:/* Refresh SelfScan: display the current content of display memory on SelfScan
							This should be a call to refresh_SS(). However, this is already taken care of 
							by the emulation of DMA at the very beginning of this code */
							//WriteLine("0x43 Refresh Display Request");
							break;
						default:
							WriteLine($"INP 01 with A={regs.regA:X2} -- not implemented");
							break;
					}
					break;
				case 0x45: // INP 02 -- get the status of selected device into the accumulator
					switch (_machine.SelectedDevice)
					{
						case 0x00: // no output device is selected, return 0; this happens, for instance,
								   // when trying to print text when printer is not yet selected
							regs.regA = 0;
							break;

						case 0x01: // printer
							// Click Printer Icon to connect Printer
							// Enter [QUAD]OUT 1
							// Test with [QUAD]<-'HELLO'
							if (_printer.PrinterConnected == false)
							{
								regs.regA = 0; // printer not connected, so no status, no Answer-Back Code
							}
							else if (_printer.ABC == 1)
							{
								regs.regA = 66;		// return Answer-Back Code on 1st request for status;
													//    66 value comes from printer's jumpers
								_printer.ABC = 0;	// reset ABC so that the consecutive status requests 
													//    return "real" status
							}
							else
							{
								regs.regA = _printer.pr_status;
							}
							break;
						case 0x0A: // future device (template)
							// regs.regA=Device_0A_status;
							break;
						case 0xC8: // cassette tape 1
							regs.regA = _tapes.tape0_s.status;			// status of tape 0 returned in A
							break;
						case 0xC9: // cassette tape 2
							regs.regA = _tapes.tape1_s.status;			// status of tape 1 returned in A
							break;
						default:   // get device status of a peripheral
							WriteLine($"INP 02: this status info is not yet implemented, SelectedDevice ={_machine.SelectedDevice} A ={regs.regA:X2}");
							break;
					}
					break;

				case 0x47: // INP 03: (GDI) read the selected device's data register 

					switch (_machine.SelectedDevice)
					{
						case 0xC8: // 1st cassette drive
							var tape0 = _tapes.tape0;
							regs.regA = (byte)(tape0[_tapes.tape0_s.w_head - Tapes.delta] & 0xFF);
							_tapes.tape0_s.status = _tapes.tape0_s.status.And(0x7F);	// Clear receive data available
							break;
						case 0xC9: // 2nd cassette drive
							var tape1 = _tapes.tape1;
							regs.regA = (byte)(tape1[_tapes.tape1_s.w_head - Tapes.delta] & 0xFF);
							_tapes.tape1_s.status = _tapes.tape1_s.status.And(0x7F);	// Clear receive data available
							break;
						default:
							WriteLine($"NP 03: reading data from device -- not yet implemented, SelectedDevice {_machine.SelectedDevice} A {regs.regA:X2}");
							break;
					}
					break;

				case 0x51: // OUT 08 -- MCM/70 instruction to switch ROM; bank number is the top nibble of A
					Bank = (regs.regA >> 4);           // address in ROM[] of the first byte to copy
					//memcpy(&memory[0x1800], &ROM[bank], 2048);  
					//var rom = new ReadOnlyMemory<byte>(ROM);
					//var mem = new Memory<byte>(memory);
					//rom.CopyTo(mem)

					// bank copied to memory starting at address 0x1800, $800 (2048) bytes
					for (int iRom = Bank*0x800, iMem=0; iMem < 0x800; iMem++, iRom++)
					{
						_memory[0x1800 + iMem] = ROM[iRom];
					}
					break;
				case 0x53: /* OUT 09   -- I/O device selection:
				- device address (given in reg. A) is placed on OMNIPORT data lines  -- emulated but not implemented;
				- AOS (Address Out Strobe) is set to 1-- emulated but not implemented.*/

					if (_machine.SelectedDevice == 1)
						if (_printer.PrinterConnected == false)
						{
							break;								// printer not connected
						}
						else
						{
							_printer.ABC = 1;					// set Answer-Back Code request flag ABC
						}

					_machine.SelectedDevice = regs.regA;		// emulator's internal storage for device address
					break;
				case 0x55: // OUT 0A -- control selected device; the parameters are encoded in register A
					switch (_machine.SelectedDevice)
					{
						case 0x01: /* printer
							printer's command is a 2-byte long instruction: the ms byte is in regs.r.regA and
							the ls byte is already supplied by OUT 0B and is stored in  pr_data */

							_printer.RenderRunPrinterOut0AData = regs.regA;
							//runPrinter(regs.regA);        // execute printer's command
							// This is the principle driver out printer output
							// INT OB stored the high but, now we print
							_printer.RenderRunPrinterOut0A = true;
							break;
						case 0x0A: // future device (template)
								   // runDevice_0A (regs.regA);        // execute printer's command
							break;
						case 0xC8: /* Set tape 1 actions/parameters: tape movement direction/speed, etc.
						Check if change of the direction of tape's movement is requested*/
							sp = _tapes.tape0_s.speed.And(0x07);    // record current tape movement direction (3 ls bits only)

							if (sp != (regs.regA & 0x07))
							{
								// if change of direction, then...
								switch (regs.regA & 0x07)
								{
									case 0: // stop tape
										//sleep(1);
										_machine.Sleep(1);
										_display.SubImage(177, 267, 172, 32, _tapes.SpinStop);
										Console.WriteLine("SpinStop");
										break;
									case 3: // forward movement
										//sleep(1);
										_machine.Sleep(1);
										_display.SubImage(177, 267, 172, 32, _tapes.SpinRight);
										Console.WriteLine("SpinRight");
										break;
									case 5: // reverse movement
										//sleep(1);
										_machine.Sleep(1);
										_display.SubImage(177, 267, 172, 32, _tapes.SpinLeft);
										Console.WriteLine("SpinLeft");
										break;
								}
							}

							// set new tape actions/parameters 
							_tapes.tape0_s.speed = regs.regA;
							if ((regs.regA & 0x01) == 1)		// if reset tape request:
							{
								// comment in orig:  if (tape0_s.status & 0x04) tape0_s.status= (tape0_s.status & 0xF7);  // if cassette unloaded, then make it ready 
								_tapes.tape0_s.status = (byte)(_tapes.tape0_s.status & 0x87);   // clear receive flags but preserve "byte ready", 
								// make cassette ready
								_tapes.tape0_s.status = (byte)(_tapes.tape0_s.status | 0x40);   // set transmit buffer to empty
							}
							break;
						case 0xC9: // Set tape 2 actions/parameters: tape movement direction/speed, etc.
							sp = _tapes.tape1_s.speed.And( 0x07);
							if (sp != (regs.regA & 0x07))
								switch (regs.regA & 0x07)
								{
									case 0: // stop tape
										_machine.Sleep(1);
										_display.SubImage(620, 267, 172, 32, _tapes.SpinStop);
										break;
									case 3: // forward movement
										_machine.Sleep(1);
										_display.SubImage(620, 267, 172, 32, _tapes.SpinRight);
										break;
									case 5: // reverse movement
										_machine.Sleep(1);
										_display.SubImage(620, 267, 172, 32, _tapes.SpinLeft);
										break;
								}

							_tapes.tape1_s.speed = regs.regA;
							if ((regs.regA & 0x01) == 1)		// if reset tape request:
							{
								_tapes.tape1_s.status = (byte)(_tapes.tape1_s.status & 0x87);
								_tapes.tape1_s.status = (byte)(_tapes.tape1_s.status | 0x40);
							}
							break;
						default:
							WriteLine($"OUT 0A: controlling device {_machine.SelectedDevice:X2} not implemented -- control value={regs.regA:X2}");
							break;
					}
					break;
				case 0x57: //  OUT 0B -- send data (stored in register A) to currently selected I/O device.
					switch (_machine.SelectedDevice)
					{
						case 0x01: // printer
							_printer.pr_data = regs.regA;	// pr_data stores the ls byte of 2-byte printer command;
													// the ms byte is supplied via OUT 0A that follows OUT 0B
							break;
						case 0xC8: // output to TAPE 0
							if ((_tapes.tape0_s.speed & 0x10) == 0) break;  // not a tape data, just a hardware signal
							if ((_tapes.tape0_s.speed == 0) | (_tapes.tape0_s.speed == 0x10)) break; // no writing when tape is halted

							// all is fine, write to tape
							_tapes.tape0[_tapes.tape0_s.w_head] = regs.regA;	// store byte on tape
							_tapes.MoveTape(0);									// move tape 0
							break;
						case 0xC9: // output to TAPE 1
							if ((_tapes.tape1_s.speed & 0x10) == 0) break;
							if ((_tapes.tape1_s.speed == 0) | (_tapes.tape1_s.speed == 0x10)) break;
							
							_tapes.tape1[_tapes.tape1_s.w_head] = regs.regA;
							_tapes.MoveTape(1);
							break;
						default:
							WriteLine($"OUT 0A: writing to device {_machine.SelectedDevice:X2} not implemented -- control value={regs.regA:X2}");
							break;
					}
					break;
				case 0x7B:  /* send request to reset printer, caused by printer error, e.g. out of paper
					MCM/70 displays COMM DEVICE ERROR
					this instruction is called from addr=18C9 in ROM=C 
					in this implementation, printer's error light will be turned on; 
					the reset function is accomplished by clicking on printer's reset button; */
					break;
				case 0x7D:
					//wait140();      // "sleep" for 140uSec
					_machine.Sleep(14); // TODO: Need high resolution timer
					break;
				case 0x7F: /* OUT 1F -- Output a single column from display memory of MCM/70 to SelfScan Display.
							  Because SelfScan is not emulated (refresh is done "globally" by GLUT) -- no need
						  to do anything: the emulator's display memory is used directly as SelfScan memory.
				*/
					break;
				case 0x09:
					//printf("unimplemented OUT 0x09 %X used! at addr=%X, ROM=%X\n", inst, stack.stk[stackptr], memory[0x1FFF]);
					// functionality unknown,... see ROM 2, addr. 1872 
					WriteLine("unimplemented OUT 0x09");
					break;
				case 0x0C:
					//printf("unimplemented OUT 0x0C%X used! at addr=%X, ROM=%X\n", inst, stack.stk[stackptr], memory[0x1FFF]);
					// functionality unknown,... 
					WriteLine("unimplemented OUT 0x0C");
					break;

				default:
					//printf("unimplemented I/O %X used! at addr=%X, ROM=%X; selected device=%d\n", inst, stack.stk[stackptr], memory[0x1FFF], SelectedDevice);
					WriteLine("unimplemented I/O");
					break;
			}
		}


		//----------------------------------------------------------------------------------------------
		//  NextInstr: idle callback -- gets called continuously to execute emulator's CPU instructions
		//----------------------------------------------------------------------------------------------

#if false
		void NextInstr()
		{
			int i, temp, sp;

			if (_machine.mcm_on == 0) return;        // if MCM is off, do nothing

			/* Delay execution to get (more or less) 8008 instruction cycle timing to adjust the
			// emulator's speed;
			// if needed, change the value of emulator_speed in mcm.h: higher values slow down the
			// emulator. The emulator should execute (and show the first line of results) of this 
			// instruction after approx. 50 seconds:
			//    0.7÷⍳255   */

			//sp = emulator_speed * 100;
			//for (i = 0; i < sp; i++) temp = 0;

			RunCPU();
		}
#endif
	}
}
