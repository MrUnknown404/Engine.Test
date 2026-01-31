using Engine3.Client.Graphics.Console;
using Engine3.Utility.Versions;

namespace Engine3.Test {
	public class ConsoleTest : GameClient {
		internal ConsoleTest() : base("Console Test", new Version4Interweaved(0, 0, 0), new ConsoleGraphicsBackend()) { }

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}