using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;

namespace Engine3.Test.LightCycle.Graphics {
	public class OpenGLLightCycleRenderer : OpenGLRenderer {
		private readonly Assembly assembly = typeof(OpenGLLightCycleRenderer).Assembly;

		public OpenGLLightCycleRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window) : base(graphicsBackend, window) { }

		protected override void DrawFrame(float delta) { }
	}
}