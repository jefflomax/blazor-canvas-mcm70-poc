using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorCanvas.JsInterop
{
	/// <summary>
	/// Defined in mcmCanvas.ts
	/// </summary>
	public static class JSMethod
	{
		private const string ns = "mcm70.";
		public static string initialize => $"{ns}initialize";
		public static string startGameLoop => $"{ns}startGameLoop";
		public static string gameLoop => $"{ns}gameLoop";
		public static string jeffGetCanvasId => $"{ns}jeffGetCanvasId";
		public static string drawPixelsUnm => $"{ns}drawPixelsUnm";
	}
}
