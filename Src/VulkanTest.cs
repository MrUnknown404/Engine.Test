using System.Diagnostics;
using Engine3.Graphics.Vulkan;
using Engine3.Test.Graphics.Vulkan;
using Engine3.Utils.Versions;
using NLog;
using OpenTK.Mathematics;
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
	// TODO look into using IDisposable more?

	public class VulkanTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkWindow? MainWindow { get; set; }
		public VkWindow? Window2 { get; set; }

		public VulkanTest() : base("Vulkan Test", new Version4Interweaved(0, 0, 0), new VulkanGraphicsApiHints()) => OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			if (VkInstance is not { } vkInstance) { throw new UnreachableException(); }

			Color4<Rgba> clearColor = new(0.01f, 0.01f, 0.01f, 1);

			Logger.Debug("Making Main Window...");
			MainWindow = new(this, vkInstance, Name, 854, 480) { ClearColor = clearColor, };
			MainWindow.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = new(this, vkInstance, "Window 2", 500, 500) { ClearColor = clearColor, };

			VkRenderer1 renderer1 = new(MainWindow, MaxFramesInFlight, Assembly);
			VkRenderer2 renderer2 = new(Window2, MaxFramesInFlight, Assembly);
			renderer1.Setup();
			renderer2.Setup();

			MainWindow.Renderer = renderer1;
			Window2.Renderer = renderer2;

			Logger.Debug("Setup done. Showing windows");

			MainWindow.Show();
			Window2.Show();
		}

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}