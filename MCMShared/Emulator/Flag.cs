namespace MCMShared.Emulator
{
	public struct Flag
	{
		public byte Value;
		public bool State
		{
			get { return Value != 0; }
			set { Value = (byte)(value ? 1 : 0); }
		}
		public override string ToString()
		{
			return State ? "+" : "-";
		}
	}
}
