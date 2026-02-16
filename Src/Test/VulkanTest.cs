using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Test.Test.Graphics.Vulkan;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3.Test.Test {
	// # resources
	// https://vulkan-tutorial.com/
	// https://vkguide.dev/
	// https://lesleylai.info/en/vk-khr-dynamic-rendering/
	// https://developer.nvidia.com/vulkan-memory-management
	// https://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/#the-view-matrix
	// https://vulkan.lunarg.com/doc/view/1.4.304.0/linux/best_practices.html
	// https://github.com/KhronosGroup/Vulkan-ValidationLayers/blob/main/docs/debug_printf.md
	// https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md
	// https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md#rendering-adding-support-for-imguibackendflags_rendererhastextures-192 add if i update ImGui
	// https://github.com/ocornut/imgui/blob/master/docs/FAQ.md#q-how-can-i-create-my-own-backend
	// https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/GUI/02_imgui_setup.html
	// https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_vulkan.cpp
	// https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_opengl3.cpp
	// TODO make resources.md

	// # where i'm at
	// https://vulkan-tutorial.com/Loading_models

	// TODO fix white screen while resizing. is it possible to show the last swap frame and scale then present?
	// TODO figure out how to dynamically change images. do i update descriptors each time?
	// TODO add way more debug logging. i kinda want more levels though. look into that. maybe redo logging in general. multiple files or single file?
	// TODO read https://docs.vulkan.org/samples/latest/samples/extensions/descriptor_indexing/README.html
	// TODO read https://docs.vulkan.org/guide/latest/buffer_device_address.html
	// TODO setup ImPlot
	// TODO use more stackallocs with vulkan

	public class VulkanTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static float PrevCubeRotation { get; private set; }
		public static float CubeRotation { get; private set; }

		public VulkanWindow? Window1 { get; set; }
		public VulkanWindow? Window2 { get; set; }

		public Camera? Camera { get; set; }

		internal VulkanTest() : base("Vulkan Test", new Version4Interweaved(0, 0, 0),
			new VulkanGraphicsBackend(new()) {
					EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
			}) {
			OnSetupFinishedEvent += OnSetupFinished;
			PerformanceMonitor = new() { CalculateMinMaxAverage = true, StoreTimesForGraph = true, FrameTimeGraphSize = 1000, };
		}

		private void OnSetupFinished() {
			if (GraphicsBackend is not VulkanGraphicsBackend { VkInstance: not null, } graphicsBackend) { throw new UnreachableException(); }

			Color4<Rgba> clearColor = new(0.01f, 0.01f, 0.01f, 1);

			Logger.Debug("Making Window 1...");
			Window1 = new(graphicsBackend, Name, 854, 480) { ClearColor = clearColor, };
			Window1.OnCloseWindowEvent += Shutdown;
			Window1.OnResize += (w, h) => Camera?.PerspectiveAspectRatio = (float)w / h;

			Logger.Debug("Making Window 2...");
			Window2 = new(graphicsBackend, "Window 2", 500, 500) { ClearColor = clearColor, };

			AddWindow(Window1);
			AddWindow(Window2);

			Camera = Camera.CreatePerspective(854f / 480f, 90, 0.01f, 100f);
			Camera.Transform.Position = new(0, 0, 2.5f);
			Camera.YawDegrees = 270;

			VulkanRenderer1 renderer1 = new(graphicsBackend, Window1, Camera, Assembly);
			VulkanRenderer2 renderer2 = new(graphicsBackend, Window2, Assembly);

			renderer1.Setup();
			renderer2.Setup();

			AddRenderer(renderer1);
			AddRenderer(renderer2);

			Logger.Info("Setup done. Showing windows");

			Window1.Show();
			Window2.Show();
		}

		protected override void Update() {
			PrevCubeRotation = CubeRotation;
			CubeRotation += 0.01f;
			CubeRotation %= 360;
		}

		protected override void Cleanup() { }
	}
}