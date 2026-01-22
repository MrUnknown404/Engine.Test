using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Graphics.OpenGL;
using Engine3.Utils.Versions;
using OpenTK.Platform;

namespace Engine3.Test {
	public class OpenGLTest : GameClient { // TODO impl
		public const string Title = "OpenGL Test";

		public GlWindow? MainWindow { get; set; }
		public GlWindow? Window2 { get; set; }

		public OpenGLTest() : base("OpenGL Test", new Version4Interweaved(0, 0, 0), new OpenGLGraphicsApiHints()) =>
				OnSetupFinishedEvent += () => {
					MainWindow = Window.MakeWindow(this, Title, 854, 480) as GlWindow;
					if (MainWindow == null) { throw new Engine3Exception("Failed to create window"); }
					MainWindow.OnCloseWindowEvent += Shutdown;

					Window2 = Window.MakeWindow(this, "Window 2", 854, 480) as GlWindow;
					if (Window2 == null) { throw new Engine3Exception("Failed to create window"); }

					MainWindow.Show();
					Window2.Show();
				};

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}