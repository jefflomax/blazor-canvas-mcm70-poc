using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorWasmClient
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);

			// Used to communicate from MCM Component to MainLayout
			builder.Services.AddSingleton<Runner.AppState>();

			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped
			(
				sp => new HttpClient
				{
					BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
				}
			);

			await builder.Build().RunAsync();
		}
	}
}
