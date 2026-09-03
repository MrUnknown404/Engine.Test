using Engine4.Client.Graphics.Console;
using Engine4.Client.Rendering;

namespace Engine4.Test;

// this is kinda just hacked on. idk how this should work yet
public class TestConsoleRenderPass : RenderPass {
	/// you need to set this manually atm
	internal ConsoleRenderer ConsoleRenderer { private get; set; } = null!;
	/// you need to set this manually atm
	internal ConsoleGraphicsProvider ConsoleGraphics { private get; set; } = null!;

	public TestConsoleRenderPass() : base(null!) { } // TODO should i use null! and ignore or refactor

	protected override void RecordCommandBuffer() { ConsoleGraphics.Blit(ConsoleRenderer.Buffer, 'c', 3, 3, 3, 3); }
}