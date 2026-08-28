using Engine4.Client.Graphics;
using Engine4.Client.Rendering;
using JetBrains.Annotations;

namespace Engine4.Test;

public class TestRenderPass : RenderPass {
	public TestRenderPass() : base(CreateGraphicsPipeline()) { }

	[MustUseReturnValue]
	private static IGraphicsPipeline CreateGraphicsPipeline() => throw new NotImplementedException();
}