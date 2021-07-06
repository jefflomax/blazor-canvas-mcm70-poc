﻿namespace BlazorCanvas.JsInterop
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
		public static string drawImageToCanvas => $"{ns}drawImageToCanvas";

		// UnMarshalled
		public static string refreshSsUnm => $"{ns}refreshSsUnm";
		public static string clearSsUnm => $"{ns}clearSsUnm";
		public static string dspAplPrinterOperations => $"{ns}dspAplPrinterOperations";
		// Unused
		public static string scrollTextTest => $"{ns}scrollTextTest";

	}
}