using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorCanvas.Runner
{
	public class AppState
	{
		public string InstructionCount { get; private set; }

		public int InstructionsPerSecond { get; private set; }

		public event Action OnChange;

		public void SetInstructionCount(long ic)
		{
			InstructionCount = ic.ToString("X");
			//Uncomment from MainLayout.razor
			//NotifyStateChanged();
		}

		public void SetInstructionsPerSecond(int ips)
		{
			InstructionsPerSecond = ips;
		}

		private void NotifyStateChanged() => OnChange?.Invoke();
	}
}
