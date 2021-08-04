using System.Linq;

namespace MCM70Client.Extensions
{
	public static class GlfwExtensions
	{
		public static bool IsIn
		(
			this OpenTK.Windowing.GraphicsLibraryFramework.Keys k,
			params OpenTK.Windowing.GraphicsLibraryFramework.Keys[] keys
		)
		{
			return keys.Any(key => key == k);
		}
	}
}
