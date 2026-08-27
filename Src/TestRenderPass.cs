using Engine4.Graphics;
using Engine4.Graphics.Rendering;
using JetBrains.Annotations;

namespace Engine4.Test;

public class TestRenderPass : RenderPass {
	public TestRenderPass() : base(CreateGraphicsPipeline()) { }

	[MustUseReturnValue]
	private static IGraphicsPipeline CreateGraphicsPipeline() => throw new NotImplementedException();
}