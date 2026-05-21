using Engine3.Client;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Exceptions;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics;
using Engine3.Test.Voxel.Graphics.Renderers;
using Engine3.Test.Voxel.Registries;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;
using ModSource = Engine3.Test.Voxel.Modding.ModSource;

namespace Engine3.Test.Voxel;

public class VoxelTest : GameClient {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static ModSource ModSource { get; } = new("voxel_test", typeof(VoxelTest).Assembly);

	public VulkanWindow? Window { get; private set; }
	public VoxelRenderPassRenderer? Renderer { get; private set; }

	public Camera? Camera { get; private set; }
	public FloatingCameraController? CameraController { get; private set; }

	public World.World? World { get; private set; }

	public BakedMasterRegistry<Block> MasterBlockRegistry { get; }

	public BakedRegistry<Block> BlockRegistry { get; }

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

		Logger.Info("Baking registries");

		using MasterRegistry<Block> masterBlockRegistry = new();
		BlockRegistry = new(ModSource, Blocks.Blocks.AllBlocks);
		masterBlockRegistry.AddRegistry(BlockRegistry);

		MasterBlockRegistry = masterBlockRegistry.Bake();
		Logger.Debug($"Baked {MasterBlockRegistry.AllObjects.Count} blocks");
	}

	private void OnSetupFinished() {
		if (GraphicsBackend is not VulkanBackend { VkInstance: not null, } graphicsBackend) { throw new IllegalStateException(); }

		Logger.Debug("Making Window...");
		Window = new(graphicsBackend, Name, 854, 480) { ClearColor = new(0.01f, 0.01f, 0.01f, 1), };
		Window.OnCloseWindowEvent += RequestShutdown;
		Window.OnResize += (w, h) => Camera?.PerspectiveAspectRatio = (float)w / h;

		AddWindow(Window);

		Camera = Camera.CreatePerspective(854f / 480f, 90, 0.01f, 1000f);
		Camera.Position = new(0, 0, 2.5f);

		CameraController = new(Window, Camera);

		Renderer = new(this, graphicsBackend, Window, Camera, Assembly);
		AddRenderer(Renderer);

		Logger.Debug("Creating World");
		World = new(new() { Seed = 1, }, Camera, Renderer.WorldRenderPass, Renderer.ChunkOutlineRenderPass);

		Logger.Info("Setup done");

		Window?.Show();
		Logger.Info("Showing window");
	}

	protected override void Update() {
		CameraController?.Update();
		World?.Update();
	}

	protected override void Cleanup() { }
}