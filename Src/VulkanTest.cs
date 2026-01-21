using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Test.Graphics;
using NLog;
using OpenTK.Platform;

namespace Engine3.Test {
	// # resources
	// https://vkguide.dev/
	// https://vulkan-tutorial.com/
	// https://lesleylai.info/en/vk-khr-dynamic-rendering/
	// TODO read https://medium.com/@heypete/hello-triangle-meet-swift-and-wide-color-6f9e246616d9
	// https://developer.nvidia.com/vulkan-memory-management
	// https://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/#the-view-matrix

	// # where i'm at
	// https://vulkan-tutorial.com/Texture_mapping/Images

	// TODO fix white screen while resizing
	// TODO look into using IDisposable more

	public class VulkanTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Window? MainWindow { get; set; }
		public Window? Window2 { get; set; }

		public VulkanTest() : base("Vulkan Test", new(0, 0, 0), new VulkanGraphicsApiHints()) => OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			Logger.Debug("Making Main Window...");
			MainWindow = Window.MakeWindow(this, Name, 854, 480);
			if (MainWindow is not VkWindow mainWindow) { throw new Engine3Exception("Failed to create window"); }
			MainWindow.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = Window.MakeWindow(this, "Window 2", 500, 500);
			if (Window2 is not VkWindow window2) { throw new Engine3Exception("Failed to create window"); }

			VkRenderer1 renderer1 = new(mainWindow, MaxFramesInFlight, Assembly);
			VkRenderer2 renderer2 = new(window2, MaxFramesInFlight, Assembly);
			renderer1.Setup();
			renderer2.Setup();

			mainWindow.Renderer = renderer1;
			window2.Renderer = renderer2;

			Logger.Debug("Setup done. Showing windows");

			MainWindow.Show();
			Window2.Show();
		}

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}