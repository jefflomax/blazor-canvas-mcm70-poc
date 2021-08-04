namespace MCM70Client.Emulator.NotOriginal
{
	public struct Light
	{
		public Light(Rgb left, Rgb center, Rgb right)
		{
			Left = left;
			Middle = center;
			Right = right;
		}
		public Rgb Left;
		public Rgb Middle;
		public Rgb Right;
	}
}
