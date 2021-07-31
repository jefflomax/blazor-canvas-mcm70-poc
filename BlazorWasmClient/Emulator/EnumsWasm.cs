
namespace BlazorWasmClient.Emulator
{
	public enum ImagesWasm : byte
	{
		SpinLeft = 1,
		SpinRight,
		SpinStop,
		TapeEmptyClosed,
		TapeEmptyOpened,
		TapeLoadedClosed,
		TapeLoadedOpened
	}

	public enum JSKeyCode
	{
		None = 0,
		Space,
		BackSpace,
		F1,
		F2,
		TAB
	}
}
