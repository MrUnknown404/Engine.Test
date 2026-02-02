using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Test.LightCycle.Graphics;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3.Test.LightCycle {
	public sealed class LightCycleTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private Window? window;

		private readonly GameManager gameManager = new();

		private readonly bool useVulkan;

		public static float PrevCubeRotation { get; private set; }
		public static float CubeRotation { get; private set; }

		public LightCycleTest(bool useVulkan) : base("Light Cycle Test", new BuildVersion(0),
			useVulkan ?
					new VulkanGraphicsBackend(new()) {
							EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
					} :
					new OpenGLGraphicsBackend(new())) {
			this.useVulkan = useVulkan;

			OnSetupFinishedEvent += OnSetupFinished;
		}

		private void OnSetupFinished() {
			Color4<Rgba> clearColor = new(0.01f, 0.01f, 0.01f, 1);

			Renderer renderer;

			Logger.Debug("Making Window...");
			if (useVulkan) {
				VulkanGraphicsBackend backend = GraphicsBackend as VulkanGraphicsBackend ?? throw new UnreachableException();
				window = new VulkanWindow(backend, Name, 854, 480) { ClearColor = clearColor, };
				renderer = new VulkanLightCycleRenderer(backend, (VulkanWindow)window, gameManager);
			} else {
				OpenGLGraphicsBackend backend = GraphicsBackend as OpenGLGraphicsBackend ?? throw new UnreachableException();
				window = new OpenGLWindow(backend, Name, 854, 480) { ClearColor = clearColor, };
				renderer = new OpenGLLightCycleRenderer(backend, (OpenGLWindow)window);
			}

			window.OnCloseWindowEvent += Shutdown;

			Windows.Add(window);

			renderer.Setup();
			Renderers.Add(renderer);

			Logger.Info($"Setting up {nameof(GameManager)}");
			gameManager.Setup(window.InputManager);

			Logger.Info("Setup done. Showing windows");

			window.Show();
		}

		protected override void Update() {
			PrevCubeRotation = CubeRotation;
			CubeRotation += 0.01f;
			CubeRotation %= 360;

			gameManager.Update();
		}

		protected override void Cleanup() { }
	}
}