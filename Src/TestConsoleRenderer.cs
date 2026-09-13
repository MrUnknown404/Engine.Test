using Engine4.Client.Rendering;

namespace Engine4.Test;

public class TestConsoleRenderer : ConsoleRenderer {
	protected override void Setup() { }
	protected override void DrawFrame(float delta) => Blit('o', FrameCount % 2 == 0 ? 3 : 9, 3, 3, 3);
}