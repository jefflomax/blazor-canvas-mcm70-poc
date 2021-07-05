namespace MCMShared.Emulator
{
	public class Rectangle
	{
		public Rectangle(int x, int y, int w, int h)
		{
			X = x;
			Y = y;
			W = w;
			H = h;
		}
		public int X { get; }
		public int Y { get; }
		public int W { get; }
		public int H { get; }
	}
}
