using Engine4.Client;
using Engine4.Client.Graphics;
using Engine4.Client.Rendering;

namespace Engine4.Test;

public class TestGame : GameClient {
	public Window VulkanWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer VulkanWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass VulkanWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public Window OpenGLWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer OpenGLWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass OpenGLWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public Window SoftwareWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer SoftwareWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass SoftwareWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public Window EmptyWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer EmptyWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass EmptyWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public TestGame(Engine engine) : base(engine, "Test Game", GraphicsApis.Vulkan | GraphicsApis.OpenGL, true) => OnSetupDoneEvent += OnSetupDone;

	private void OnSetupDone() {
		Console.WriteLine("done");

		const string Title = "title goes here";

		VulkanWindow = CreateWindow(GraphicsApi.Vulkan, $"vulkan. {Title}", 854, 480);
		WindowRenderTarget vulkanWindowRenderTarget = new(this, VulkanWindow);

		OpenGLWindow = CreateWindow(GraphicsApi.OpenGL, $"opengl. {Title}", 854, 480);
		WindowRenderTarget openglWindowRenderTarget = new(this, OpenGLWindow);

		// SoftwareWindow = CreateWindow(GraphicsApi.Software, $"software. {Title}", 854, 480); // TODO opentk doesn't currently support making windows without OpenGL/Vulkan. look into this
		// WindowRenderTarget softwareWindowRenderTarget = new(this, SoftwareWindow);

		// EmptyWindow = CreateWindow(GraphicsApi.None, $"none. {Title}", 854, 480);
		// WindowRenderTarget emptyWindowRenderTarget = new(this, EmptyWindow);

		VulkanWindowRenderPass = new();
		OpenGLWindowRenderPass = new();
		// SoftwareWindowRenderPass = new();
		// EmptyWindowRenderPass = new();

		VulkanWindowRenderer = CreateRenderer(vulkanWindowRenderTarget, VulkanWindowRenderPass);
		OpenGLWindowRenderer = CreateRenderer(openglWindowRenderTarget, OpenGLWindowRenderPass);
		// SoftwareWindowRenderer = CreateRenderer(softwareWindowRenderTarget, SoftwareWindowRenderPass);
		// EmptyWindowRenderer = CreateRenderer(emptyWindowRenderTarget, EmptyWindowRenderPass);

		VulkanWindow.Show();
		OpenGLWindow.Show();
		// SoftwareWindow.Show();
		// EmptyWindow.Show();

		Console.WriteLine("show");

		// RequestShutdown();
	}

	protected override void Update() {
		if (ReadonlyWindows.Count == 0) { RequestShutdown(); }
	}
}