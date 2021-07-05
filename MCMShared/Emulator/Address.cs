using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MCMShared.Emulator
{
	// Simulate C UNION
	// reg covers the same data as regL regH
	[StructLayout(LayoutKind.Explicit, Size = 2, CharSet = CharSet.Ansi)]
	[DebuggerDisplay("{reg,h}")]
	public struct Address
	{
		[FieldOffset(0)]
		public ushort reg;

		[FieldOffset(0)]
		public byte regL;
		[FieldOffset(1)]
		public byte regH;

		public void MaskAddress(int andTo)
		{
			reg = (ushort)(reg & andTo);
		}
		public void IncrementAndMask(int andTo)
		{
			reg = (ushort)((reg + 1) & andTo);
		}
	}
}
