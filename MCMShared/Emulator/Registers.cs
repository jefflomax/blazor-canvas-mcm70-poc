using System;
using System.Diagnostics;

namespace MCMShared.Emulator
{
	// We can't use StructLayout to sneak an array on top of
	// bytes, but we can use an indexer
	// registers:
	[DebuggerDisplay("A {regA,h} H {regH,h} L{regL,h}")]
	public class Registers
	{
		public byte regA;
		public byte regB;
		public byte regC;
		public byte regD;
		public byte regE;
		public Address M; // H & L

		public void Clear()
		{
			regA = 0;
			regB = 0;
			regC = 0;
			regD = 0;
			regE = 0;
			M.reg = 0;
		}

		/// <summary>
		/// M register and 0x3FFF
		/// </summary>
		public ushort M14Bit => (ushort) (M.reg & 0x3FFF);

		public override string ToString()
		{
			return $"A {regA:X2} B {regB:X2} C {regC:X2} D {regD:X2} E {regE:X2} H {M.regH:X2} L {M.regL:X2}";
		}
		public byte this[byte key]
		{
			get
			{
				switch (key)
				{
					case 0: return regA;
					case 1: return regB;
					case 2: return regC;
					case 3: return regD;
					case 4: return regE;
					case 5: return M.regH;
					case 6: return M.regL;
					default:
						throw new Exception($"Reg illegal index {key}");
				}
			}
			set
			{
				switch (key)
				{
					case 0: regA = value; return;
					case 1: regB = value; return;
					case 2: regC = value; return;
					case 3: regD = value; return;
					case 4: regE = value; return;
					case 5: M.regH = value; return;
					case 6: M.regL = value; return;
					default:
						throw new Exception($"Reg illegal index {key}");
				}
			}
		}
		public byte this[ushort key]
		{
			get => this[(byte)key];
			set => this[(byte)key] = value;
		}
	}
}
