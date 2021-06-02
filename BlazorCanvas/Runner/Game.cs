using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorCanvas.Runner
{
	public class Game
	{
		private bool _isInitialized;
		public Game()
		{
			_isInitialized = false;
		}

		public async ValueTask Init()
		{
			await Task.CompletedTask;
		}

		public async ValueTask Step()
		{
			if (!_isInitialized)
			{
				await this.Init();

				GameTime.Start();

				_isInitialized = true;
			}

			GameTime.Step();
		}

		public GameTime GameTime { get; } = new();

	}
}
