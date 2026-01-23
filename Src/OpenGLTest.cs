using Engine3.Graphics.OpenGL;
using Engine3.Test.Graphics.OpenGL;
using Engine3.Utils.Versions;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Test {
	public class OpenGLTest : GameClient {
		public const string Title = "OpenGL Test";

		public GlWindow? MainWindow { get; set; }
		public GlWindow? Window2 { get; set; }

		public OpenGLTest() : base("OpenGL Test", new Version4Interweaved(0, 0, 0), new OpenGLGraphicsApiHints()) => OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			Color4<Rgba> clearColor = new(0.1f, 0.1f, 0.1f, 1);

			MainWindow = new(this, Title, 854, 480) { ClearColor = clearColor, };
			MainWindow.OnCloseWindowEvent += Shutdown;

			Window2 = new(this, "Window 2", 500, 500) { ClearColor = clearColor, };

			GlRenderer1 renderer1 = new(MainWindow, Assembly);
			GlRenderer2 renderer2 = new(Window2, Assembly);
			renderer1.Setup();
			renderer2.Setup();

			MainWindow.Renderer = renderer1;
			Window2.Renderer = renderer2;

			MainWindow.Show();
			Window2.Show();
		}

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}