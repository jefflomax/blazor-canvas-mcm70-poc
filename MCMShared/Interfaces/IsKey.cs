namespace MCMShared.Interfaces
{
	public interface IsKey
	{
		bool IsF1();
		bool IsF2();
		bool IsTab();
		bool IsSpace();
		bool IsBackspace();
		bool HasCtrlModifier();
	}
}
