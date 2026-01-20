using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Test.Graphics;
using NLog;
using OpenTK.Graphics.Vulkan;
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
	// https://vulkan-tutorial.com/Uniform_buffers/Descriptor_set_layout_and_buffer

	// TODO fix white screen while resizing
	// TODO make repo for this

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

			VkDevice vkLogicalDevice1 = mainWindow.LogicalGpu.LogicalDevice;
			VkDevice vkLogicalDevice2 = window2.LogicalGpu.LogicalDevice;
			QueueFamilyIndices queueFamilyIndices1 = mainWindow.SelectedGpu.QueueFamilyIndices;
			QueueFamilyIndices queueFamilyIndices2 = window2.SelectedGpu.QueueFamilyIndices;

			VkCommandPool vkGraphicsCommandPool1 = VkH.CreateCommandPool(vkLogicalDevice1, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, queueFamilyIndices1.GraphicsFamily);
			VkCommandPool vkGraphicsCommandPool2 = VkH.CreateCommandPool(vkLogicalDevice2, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, queueFamilyIndices2.GraphicsFamily);
			Logger.Debug("Created graphics command pools");

			VkCommandPool vkTransferCommandPool1 = VkH.CreateCommandPool(vkLogicalDevice1, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, queueFamilyIndices1.TransferFamily);
			VkCommandPool vkTransferCommandPool2 = VkH.CreateCommandPool(vkLogicalDevice2, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, queueFamilyIndices2.TransferFamily);
			Logger.Debug("Created transfer command pools");

			VkRenderer1 renderer1 = new(this, mainWindow, vkGraphicsCommandPool1, vkTransferCommandPool1);
			VkRenderer2 renderer2 = new(this, window2, vkGraphicsCommandPool2, vkTransferCommandPool2);
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