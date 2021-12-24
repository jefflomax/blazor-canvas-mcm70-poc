using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BlazorWasmClient.Runner
{
	public class GameTime
	{
		private readonly Stopwatch _stopwatch;
		private readonly float _ticksInSixtiethOfASecond;
		public readonly int InstructionsPerFrame = 1090; // Until we have timing
		private readonly int _maxInstructionsPerFrame = 1500;

		private long _lastTick = 0;
		private long _elapsedTicks = 0;

		// Intel 8008 clock speed 500Mhz
		// 500,000,000 cycles per second
		// 1ms = 1 / 1000 of a second
		// 1tick = 1 / 10000000 of a second
		// We need exact cycles/instruction which we don't have yet
		// In order to compute 0.7 / iota 255 in 50 seconds,
		// we need to execute around 65000 instructions per second

		public GameTime()
		{
			// not sure why the * 100
			_ticksInSixtiethOfASecond = TimeSpan.TicksPerSecond / 60.0f * 100 ;
			_stopwatch = new Stopwatch();
		}

		public void Start()
		{
			_stopwatch.Reset();
			_stopwatch.Start();

			_lastTick = _stopwatch.ElapsedTicks;
		}

		public int Step()
		{
			_elapsedTicks = _stopwatch.ElapsedTicks - _lastTick;

			_lastTick = _stopwatch.ElapsedTicks;

			var frameMultiple = _elapsedTicks / _ticksInSixtiethOfASecond;
			int instructions = (int)(frameMultiple * InstructionsPerFrame);

			return Math.Min(instructions, _maxInstructionsPerFrame);
		}

		/// <summary>
		/// total time elapsed since the beginning of the game, in ticks
		/// </summary>
		public long TotalTicks => _stopwatch.ElapsedTicks;

		/// <summary>
		/// time elapsed since last frame, in ticks
		/// </summary>
		public long ElapsedTicks => _elapsedTicks;

	}
}
