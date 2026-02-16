using Engine3.Client;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Exceptions;
using Engine3.Test.Voxel.Graphics;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel {
	public class VoxelTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static float PrevCubeRotation { get; private set; }
		public static float CubeRotation { get; private set; }

		public VulkanWindow? Window { get; set; }

		public Camera? Camera { get; set; }
		public FloatingCameraController? CameraController { get; set; }

		public World.World? World { get; set; }

		internal VoxelTest(bool useVulkan) : base("Voxel Test", new BuildVersion(),
			useVulkan ?
					new VulkanGraphicsBackend(new()) {
							EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
					} :
					throw new NotImplementedException()) {
			OnSetupFinishedEvent += OnSetupFinished;
			PerformanceMonitor = new() { CalculateMinMaxAverage = true, StoreTimesForGraph = true, FrameTimeGraphSize = 1000, };
		}

		private void OnSetupFinished() {
			if (GraphicsBackend is not VulkanGraphicsBackend { VkInstance: not null, } graphicsBackend) { throw new IllegalStateException(); }

			Logger.Debug("Making Window...");
			Window = new(graphicsBackend, Name, 854, 480) { ClearColor = new(0.01f, 0.01f, 0.01f, 1), };
			Window.OnCloseWindowEvent += Shutdown;
			Window.OnResize += (w, h) => Camera?.PerspectiveAspectRatio = (float)w / h;

			AddWindow(Window);

			Camera = Camera.CreatePerspective(854f / 480f, 90, 0.01f, 100f);
			Camera.Transform.Position = new(0, 0, 2.5f);
			Camera.YawDegrees = 270;

			CameraController = new(Window, Camera);

			VoxelRenderer renderer = new(graphicsBackend, Window, Camera, Assembly);

			renderer.Setup();

			AddRenderer(renderer);

			Logger.Info("Setup done. Showing windows");

			Window.Show();

			Logger.Info("Creating World");
			World = new(renderer);
			renderer.World = World;
		}

		protected override void Update() {
			const float Rotation = float.Pi / 3f / 60f;

			PrevCubeRotation = CubeRotation;
			CubeRotation += Rotation;
			CubeRotation %= 360;

			CameraController?.Update();
		}

		protected override void Cleanup() { }
	}
}