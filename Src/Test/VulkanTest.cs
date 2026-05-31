using System.Diagnostics;
using Engine3.Client;
using Engine3.Client.Client;
using Engine3.Client.Client.Graphics.Vulkan;
using Engine3.Core;
using Engine3.Core.Utility;
using Engine3.Core.Utility.Versions;
using Engine3.Test.Test.Graphics.Vulkan;
using NLog;
using VulkanRenderer2 = Engine3.Test.Test.Graphics.Vulkan.VulkanRenderer2;

namespace Engine3.Test.Test;

public class VulkanTest : EngineGame {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static float PrevCubeRotation { get; private set; }
	public static float CubeRotation { get; private set; }

	public VulkanWindow? Window1 { get; set; }
	public VulkanWindow? Window2 { get; set; }
	public VulkanWindow? Window3 { get; set; }

	public Camera? Camera { get; set; }

	internal VulkanTest() : base("Vulkan Test", new Version4Interweaved(0, 0, 0)) => OnSetupFinishedEvent += OnSetupFinished;

	private void OnSetupFinished() {
		Engine3Client engine = (Engine3Client)Engine3.Core.Engine3.Engine;
		if (engine.GraphicsBackend is not VulkanBackend { VkInstance: not null, } graphicsBackend) { throw new UnreachableException(); }

		Color4 clearColor = new(0.01f, 0.01f, 0.01f, 1);

		Logger.Debug("Making Window 1...");
		Window1 = new(graphicsBackend, Name, 854, 480) { ClearColor = clearColor, };
		Window1.OnCloseWindowEvent += RequestShutdown;
		Window1.OnResize += (w, h) => Camera?.PerspectiveAspectRatio = (float)w / h;

		Logger.Debug("Making Window 2...");
		Window2 = new(graphicsBackend, "Window 2", 500, 500) { ClearColor = clearColor, };

		Logger.Debug("Making Window 3...");
		Window3 = new(graphicsBackend, "Window 3", 500, 500) { ClearColor = clearColor, };

		Camera = Camera.CreatePerspective(854f / 480f, 90, 0.01f, 100f);
		Camera.Position = new(0, 0, 2.5f);

		VulkanRenderer1 renderer1 = new(this, graphicsBackend, Window1, Camera, Assembly);
		VulkanRenderer2 renderer2 = new(graphicsBackend, Window2, Assembly);
		TestRenderPassRenderer renderer3 = new(this, graphicsBackend, Window3, Camera, Assembly);

		engine.AddWindow(Window1, renderer1);
		engine.AddWindow(Window2, renderer2);
		engine.AddWindow(Window3, renderer3);

		engine.AddRenderer(renderer1);
		engine.AddRenderer(renderer2);
		engine.AddRenderer(renderer3);

		Logger.Info("Setup done. Showing windows");

		Window1.Show();
		Window2.Show();
		Window3.Show();
	}

	protected override void Update() {
		PrevCubeRotation = CubeRotation;
		CubeRotation += 0.01f;
		CubeRotation %= 360;
	}

	protected override void Cleanup() { }
}