using Engine4.Graphics;

namespace Engine4.Test.Rendering;

public class TestRenderer : Renderer {
	public TestRenderer(GraphicsApi graphicsApi, RenderTarget renderTarget, IGraphicsApiProvider graphicsProvider, params RenderPass[] renderPasses) : base(graphicsApi, renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => throw new NotImplementedException();
	public override void UpdateBuffers(float delta) => throw new NotImplementedException();
	public override void DrawFrame() => throw new NotImplementedException();
	public override void EndFrame() => throw new NotImplementedException();
	public override void PresentFrame() => throw new NotImplementedException();
}