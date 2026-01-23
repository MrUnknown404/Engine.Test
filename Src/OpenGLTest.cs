using Engine3.Graphics.OpenGL;
using Engine3.Test.Graphics.OpenGL;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Test {
	public class OpenGLTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string Title = "OpenGL Test";

		public GlWindow? Window1 { get; set; }
		public GlWindow? Window2 { get; set; }

		public OpenGLTest() : base("OpenGL Test", new Version4Interweaved(0, 0, 0), new OpenGLGraphicsApiHints()) => OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			Color4<Rgba> clearColor = new(0.1f, 0.1f, 0.1f, 1);

			Logger.Debug("Making Window 1...");
			Window1 = new(this, Title, 854, 480) { ClearColor = clearColor, };
			Window1.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = new(this, "Window 2", 500, 500) { ClearColor = clearColor, };

			Windows.Add(Window1);
			Windows.Add(Window2);

			GlRenderer1 renderer1 = new(Window1, Assembly);
			GlRenderer2 renderer2 = new(Window2, Assembly);
			renderer1.Setup();
			renderer2.Setup();
			RenderingPipelines.Add(renderer1);
			RenderingPipelines.Add(renderer2);

			Logger.Debug("Setup done. Showing windows");

			Window1.Show();
			Window2.Show();
		}

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}