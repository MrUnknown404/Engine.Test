using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Test.Graphics.Vulkan;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3.Test {
	// # resources
	// https://vkguide.dev/
	// https://vulkan-tutorial.com/
	// https://lesleylai.info/en/vk-khr-dynamic-rendering/
	// TODO read https://medium.com/@heypete/hello-triangle-meet-swift-and-wide-color-6f9e246616d9
	// https://developer.nvidia.com/vulkan-memory-management
	// https://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/#the-view-matrix
	// https://vulkan.lunarg.com/doc/view/1.4.304.0/linux/best_practices.html
	// https://github.com/KhronosGroup/Vulkan-ValidationLayers/blob/main/docs/debug_printf.md

	// # where i'm at
	// https://vulkan-tutorial.com/Loading_models

	// TODO fix white screen while resizing
	// TODO look into using IDisposable more?
	// TODO figure out how to dynamically change images. do i set descriptors each time?

	public class VulkanTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkWindow? Window1 { get; set; }
		public VkWindow? Window2 { get; set; }

		public VulkanTest() : base("Vulkan Test", new Version4Interweaved(0, 0, 0),
			new VulkanGraphicsBackend(new()) {
					EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
			}) =>
				OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			if (GraphicsBackend is not VulkanGraphicsBackend { VkInstance: { } vkInstance, } graphicsBackend) { throw new UnreachableException(); }

			Color4<Rgba> clearColor = new(0.01f, 0.01f, 0.01f, 1);

			Logger.Debug("Making Window 1...");
			Window1 = new(graphicsBackend, vkInstance, Name, 854, 480) { ClearColor = clearColor, };
			Window1.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = new(graphicsBackend, vkInstance, "Window 2", 500, 500) { ClearColor = clearColor, };

			Windows.Add(Window1);
			Windows.Add(Window2);

			VkRenderer1 renderer1 = new(graphicsBackend, Window1, Assembly);
			VkRenderer2 renderer2 = new(graphicsBackend, Window2, Assembly);
			renderer1.Setup();
			renderer2.Setup();
			Renderers.Add(renderer1);
			Renderers.Add(renderer2);

			Logger.Info("Setup done. Showing windows");

			Window1.Show();
			Window2.Show();
		}

		protected override void Update() { }

		protected override void Cleanup() { }
	}
}