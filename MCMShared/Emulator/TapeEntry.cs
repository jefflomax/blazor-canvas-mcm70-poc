
namespace MCMShared.Emulator
{
	public class TapeEntry
	{
		protected string _name;
		public TapeEntry(int id, string name, bool isEject = false)
		{
			Id = id;
			_name = name;
			IsEject = isEject;
		}

		public int Id { get; set; }
		public virtual string GetName()
		{
			return _name;
		}
		public string GetPathName()
		{
			return _name;
		}
		public bool IsEject { get; set; }
	}
}
