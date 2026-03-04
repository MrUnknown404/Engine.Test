using Engine3.Client;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Exceptions;
using Engine3.Test.Voxel.Graphics;
using Engine3.Test.Voxel.Graphics.Renderers;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel {
	public class VoxelTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VulkanWindow? Window { get; set; }

		public Camera? Camera { get; set; }
		public FloatingCameraController? CameraController { get; set; }

		public World.World? World { get; set; }

		internal VoxelTest(bool useVulkan) : base("Voxel Test", new BuildVersion(),
			useVulkan ?
					new VoxelGraphicsBackend(new()) {
							Settings = new() {
									EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
							},
					} :
					throw new NotImplementedException()) {
			OnSetupFinishedEvent += OnSetupFinished;
			PerformanceMonitor = new() { CalculateMinMaxAverage = true, StoreTimesForGraph = true, LastFrameTimeSize = 1000, };
		}

		private void OnSetupFinished() {
			if (GraphicsBackend is not VulkanGraphicsBackend { VkInstance: not null, } graphicsBackend) { throw new IllegalStateException(); }

			Logger.Debug("Making Window...");
			Window = new(graphicsBackend, Name, 854, 480) { ClearColor = new(0.01f, 0.01f, 0.01f, 1), };
			Window.OnCloseWindowEvent += RequestShutdown;
			Window.OnResize += (w, h) => Camera?.PerspectiveAspectRatio = (float)w / h;

			AddWindow(Window);

			Camera = Camera.CreatePerspective(854f / 480f, 90, 0.01f, 1000f);
			Camera.Position = new(0, 0, 2.5f);

			CameraController = new(Window, Camera);

			VoxelRenderPassRenderer renderer = new(this, graphicsBackend, Window, Camera, Assembly);
			renderer.OnSetupDoneEvent += () => {
				renderer.SetWorld(World !); // shouldn't be null here

				Window?.Show();
				Logger.Info("Showing window");
			};

			AddRenderer(renderer);

			Logger.Debug("Creating World");
			World = new();

			Logger.Info("Setup done");
		}

		protected override void Update() => CameraController?.Update();

		protected override void Cleanup() { }
	}
}