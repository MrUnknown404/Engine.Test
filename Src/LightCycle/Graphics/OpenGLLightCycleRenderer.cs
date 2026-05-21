using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.OpenGL.Renderers;

namespace Engine3.Test.LightCycle.Graphics;

public class OpenGLLightCycleRenderer : OpenGLRendererBase {
	private readonly Assembly assembly = typeof(OpenGLLightCycleRenderer).Assembly;

	public OpenGLLightCycleRenderer(OpenGLBackend graphicsBackend, OpenGLWindow window) : base(graphicsBackend, window) { }

	protected override void DrawFrame() { }
	protected override void CopyBuffers(float delta) { }
}