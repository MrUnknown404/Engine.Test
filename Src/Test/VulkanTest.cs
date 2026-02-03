using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Test.Test.Graphics.Vulkan;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Test.Test {
	// # resources
	// https://vulkan-tutorial.com/
	// https://vkguide.dev/
	// https://lesleylai.info/en/vk-khr-dynamic-rendering/
	// TODO read https://medium.com/@heypete/hello-triangle-meet-swift-and-wide-color-6f9e246616d9
	// https://developer.nvidia.com/vulkan-memory-management
	// https://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/#the-view-matrix
	// https://vulkan.lunarg.com/doc/view/1.4.304.0/linux/best_practices.html
	// https://github.com/KhronosGroup/Vulkan-ValidationLayers/blob/main/docs/debug_printf.md

	// # where i'm at
	// https://vulkan-tutorial.com/Loading_models

	// TODO fix white screen while resizing
	// TODO figure out how to dynamically change images. do i update descriptors each time?
	// TODO add way more debug logging. i kinda want more levels though. look into that. maybe redo logging in general
	// TODO i'd like the engine to use instancing when rendering but how should that work?
	// TODO read https://docs.vulkan.org/samples/latest/samples/extensions/descriptor_indexing/README.html
	// TODO read https://docs.vulkan.org/guide/latest/buffer_device_address.html
	// TOOD setup ImGui & ImPlot and render debug info. fps/frame graph

	public class VulkanTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VulkanWindow? Window1 { get; set; }
		public VulkanWindow? Window2 { get; set; }

		public static float PrevCubeRotation { get; private set; }
		public static float CubeRotation { get; private set; }

		internal VulkanTest() : base("Vulkan Test", new Version4Interweaved(0, 0, 0),
			new VulkanGraphicsBackend(new()) {
					EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
			}) =>
				OnSetupFinishedEvent += OnSetupFinished;

		private void OnSetupFinished() {
			if (GraphicsBackend is not VulkanGraphicsBackend { VkInstance: not null, } graphicsBackend) { throw new UnreachableException(); }

			Color4<Rgba> clearColor = new(0.01f, 0.01f, 0.01f, 1);

			Logger.Debug("Making Window 1...");
			Window1 = new(graphicsBackend, Name, 854, 480) { ClearColor = clearColor, };
			Window1.OnCloseWindowEvent += Shutdown;

			Logger.Debug("Making Window 2...");
			Window2 = new(graphicsBackend, "Window 2", 500, 500) { ClearColor = clearColor, };

			Windows.Add(Window1);
			Windows.Add(Window2);

			VulkanRenderer1 renderer1 = new(graphicsBackend, Window1, Assembly);
			VulkanRenderer2 renderer2 = new(graphicsBackend, Window2, Assembly);
			renderer1.Setup();
			renderer2.Setup();
			Renderers.Add(renderer1);
			Renderers.Add(renderer2);

			Logger.Info("Setup done. Showing windows");

			Window1.Show();
			Window2.Show();
		}

		protected override void Update() {
			PrevCubeRotation = CubeRotation;
			CubeRotation += 0.01f;
			CubeRotation %= 360;

			Toolkit.Window.SetTitle(Window1?.WindowHandle ?? throw new NullReferenceException(),
				PerformanceMonitor.CalculateMinMaxAverage ?
						$"{Name} - Idx/Per/Avg/Min/Max - Update: {UpdateIndex}, {PerformanceMonitor.Ups}, {PerformanceMonitor.AvgUpdateTime:F}ms, {PerformanceMonitor.MinUpdateTime:F}ms, {PerformanceMonitor.MaxUpdateTime
							:F}ms - Frame: {FrameIndex}, {PerformanceMonitor.Fps}, {PerformanceMonitor.AvgFrameTime:F}ms, {PerformanceMonitor.MinFrameTime:F}ms, {PerformanceMonitor.MaxFrameTime:F}ms" :
						$"{Name} - Update: {UpdateIndex}, {PerformanceMonitor.Ups}, {PerformanceMonitor.UpdateTime:F}ms - Frame: {FrameIndex}, {PerformanceMonitor.Fps}, {PerformanceMonitor.FrameTime:F}ms");
		}

		protected override void Cleanup() { }
	}
}