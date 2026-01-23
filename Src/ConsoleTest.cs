using Engine3.Api.Graphics;
using Engine3.Utility.Versions;

namespace Engine3.Test {
	public class ConsoleTest : GameClient {
		public ConsoleTest() : base("Console Test", new Version4Interweaved(0, 0, 0), GraphicsBackend.Console) { }

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}