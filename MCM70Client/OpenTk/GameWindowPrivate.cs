using System;
using System.Diagnostics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MCM70Client.OpenTk
{
	public class GameWindowPrivate : GameWindow
	{
		protected Stopwatch WatchRender;
		protected Stopwatch WatchUpdate;

		protected double UpdateEpsilon; // quantization error for UpdateFrame events

		private readonly double _updateFrequencyInternal;

		public bool IsRunningSlowlyInternal { get; set; }

		public bool RunCpu { get; set; }

		public GameWindowPrivate
		(
			GameWindowSettings gameWindowSettings,
			NativeWindowSettings nativeWindowSettings,
			Stopwatch watchUpdate,
			Stopwatch watchRender,
			int updateFrequencyMultiplier
		) : base(gameWindowSettings, nativeWindowSettings)
		{
			WatchUpdate = watchUpdate;
			WatchRender = watchRender;

			RunCpu = true;

			_updateFrequencyInternal = gameWindowSettings.UpdateFrequency * updateFrequencyMultiplier;
		}

		public void Event_OnLoad()
		{
			OnLoad();
		}

		public void Event_Resize(ResizeEventArgs e)
		{
			OnResize(e);
		}

		public void DispatchUpdateFrame()
		{
			var isRunningSlowlyRetries = 4;
			var elapsed = WatchUpdate.Elapsed.TotalSeconds;

			var updatePeriod = UpdateFrequency == 0
				? 0 
				: 1 / _updateFrequencyInternal;

			while (elapsed > 0 && elapsed + UpdateEpsilon >= updatePeriod && RunCpu )
			{
				//count++;
				WatchUpdate.Restart();
				OnUpdateFrame(new FrameEventArgs(elapsed));

				UpdateTime = WatchUpdate.Elapsed.TotalSeconds;

				// Calculate difference (positive or negative) between
				// actual elapsed time and target elapsed time. We must
				// compensate for this difference.
				UpdateEpsilon += elapsed - updatePeriod;

				if (_updateFrequencyInternal <= double.Epsilon)
				{
					// An UpdateFrequency of zero means we will raise
					// UpdateFrame events as fast as possible (one event
					// per ProcessEvents() call)
					break;
				}

				IsRunningSlowlyInternal = UpdateEpsilon >= updatePeriod;

				if (IsRunningSlowlyInternal && --isRunningSlowlyRetries == 0)
				{
					// If UpdateFrame consistently takes longer than TargetUpdateFrame
					// stop raising events to avoid hanging inside the UpdateFrame loop.
					break;
				}

				elapsed = WatchUpdate.Elapsed.TotalSeconds;
			}
		}

		public void DispatchRenderFrame()
		{
			var elapsed = WatchRender.Elapsed.TotalSeconds;

			var renderPeriod = RenderFrequency == 0
				? 0
				: 1 / RenderFrequency;
			if (elapsed > 0 && elapsed >= renderPeriod)
			{
				WatchRender.Restart();
				OnRenderFrame(new FrameEventArgs(elapsed));

				RenderTime = WatchRender.Elapsed.TotalSeconds;

				// Update VSync if set to adaptive
				if (VSync == VSyncMode.Adaptive)
				{
					GLFW.SwapInterval(IsRunningSlowlyInternal ? 0 : 1);
				}
			}
		}
	}
}
