using System;

namespace MCMShared.Extensions
{
	/// <summary>
	/// These exist only to reduce casting from int, as in C#
	/// all these operations return int
	/// </summary>
	public static class ByteExtensions
	{
		public static byte RotateRight(this byte b, int places)
		{
			return (byte)(b >> places);
		}
		public static byte AndShiftRight(this ref byte b, int mask, int places)
		{
			return (byte)((b & mask) >> places);
		}
		public static byte ShiftRightAnd(this ref byte b, int places, int mask)
		{
			return (byte)((b >> places) & mask);
		}
		public static byte And(this ref byte b, int mask)
		{
			return (byte)(b & mask);
		}
		public static string ToBinary(this ref byte b)
		{
			return Convert.ToString(b, 2).PadLeft(8, '0');
		}
	}

}
