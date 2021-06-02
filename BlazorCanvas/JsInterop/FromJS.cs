using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorCanvas.JSInterop
{
	public static class FromJS
	{
		[JSInvokable]
		public static Task GetMessage()
		{
			var message = "Hello from C#";
			return Task.FromResult(message);
		}
	}
}
