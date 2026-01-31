using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Test.Graphics.OpenGL;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Mathematics;

namespace Engine3.Test {
	public class OpenGLTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string Title = "OpenGL Test";

		public OpenGLWindow? Window1 { get; set; }
		public OpenGLWindow? Window2 { get; set; }

		internal OpenGLTest() : base("OpenGL Test", new Version4Interweaved(0, 0, 0), new OpenGLGraphicsBackend(new()) { DisabledCallbackIds = [ 131185, ], }) => OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			if (GraphicsBackend is not OpenGLGraphicsBackend graphicsBackend) { throw new UnreachableException(); }

			Color4<Rgba> clearColor = new(0.1f, 0.1f, 0.1f, 1);

			Logger.Debug("Making Window 1...");
			Window1 = new(graphicsBackend, Title, 854, 480) { ClearColor = clearColor, };
			Window1.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = new(graphicsBackend, "Window 2", 500, 500) { ClearColor = clearColor, };

			Windows.Add(Window1);
			Windows.Add(Window2);

			OpenGLRenderer1 renderer1 = new(graphicsBackend, Window1, Assembly);
			OpenGLRenderer2 renderer2 = new(graphicsBackend, Window2, Assembly);
			renderer1.Setup();
			renderer2.Setup();
			Renderers.Add(renderer1);
			Renderers.Add(renderer2);

			Logger.Debug("Setup done. Showing windows");

			Window1.Show();
			Window2.Show();
		}

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}