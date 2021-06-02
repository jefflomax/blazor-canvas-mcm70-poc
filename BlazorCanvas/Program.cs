using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

//svg, not webgl
//https://github.com/awesomedotnetcore/AsteroidsWasm

// WebGL general
// https://www.tutorialspoint.com/webgl/webgl_drawing_a_quad.htm

// Typescript lib.dom.d.ts defines the Web GL

namespace BlazorCanvas
{
	public class Program
	{
		//public static byte[] Panel { get; set; }
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);

			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

			var assembly = typeof(Program).GetTypeInfo().Assembly;

			//Panel = ReadImageResource(assembly, "BlazorCanvas.wwwroot.images.panel.data");


			await builder.Build().RunAsync();
		}

		private static byte[] ReadImageResource
		(
			Assembly assembly,
			string resourceName
		)
		{
			//https://developer.mozilla.org/en-US/docs/Web/API/Blob/arrayBuffer
			//https://javascript.info/arraybuffer-binary-arrays
			//https://developer.mozilla.org/en-US/docs/Web/API/FileReader/readAsArrayBuffer

			// https://github.com/jackpotdk/BlazorGame/blob/master/game/BlazorGame/wwwroot/index.html
			//https://blazor-tutorial.net/knowledge-base/52136899/servir-archivos-incrustados-en-la-biblioteca--net-a-html-en-blazor
			using var stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				throw new Exception($"Missing {resourceName}");
			}
			using var binaryReader = new BinaryReader(stream);
			return binaryReader.ReadBytes((int)stream.Length);
		}

	}
}
