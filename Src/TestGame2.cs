using Engine4.Client;
using Engine4.Client.Graphics;
using Engine4.Client.Rendering;

namespace Engine4.Test;

public class TestGame2 : GameClient2 {
	public Window2? OpenGLWindow { get; private set; }
	public Window2? VulkanWindow { get; private set; }
	public Window2? SoftwareWindow { get; private set; }
	public Window2? NoGraphicsWindow { get; private set; }

	public WindowRenderTarget OpenGLWindowRenderTarget { get; private set; } = null!;
	public WindowRenderTarget VulkanWindowRenderTarget { get; private set; } = null!;
	public WindowRenderTarget SoftwareWindowRenderTarget { get; private set; } = null!;
	public WindowRenderTarget NoGraphicsWindowRenderTarget { get; private set; } = null!; // should this be necessary?

	public Renderer OpenGLWindowRenderer { get; private set; } = null!;
	public Renderer VulkanWindowWindowRenderer { get; private set; } = null!;
	public Renderer SoftwareWindowWindowRenderer { get; private set; } = null!;
	public Renderer NoGraphicsWindowWindowRenderer { get; private set; } = null!;

	public RenderPass TestRenderPass { get; private set; } = null!;

	public TestGame2(string[] args) : base(args, "Test Game", true, GraphicsApis.All) => OnSetupDoneEvent += OnSetupDone;

	private void OnSetupDone() {
		OpenGLWindow?.Show();
		VulkanWindow?.Show();
		SoftwareWindow?.Show();
		NoGraphicsWindow?.Show();
	}

	protected override void Setup() {
		const string Title = "title goes here";

		Console.WriteLine(Title);

		OpenGLWindow = CreateWindow(GraphicsApi.OpenGL, $"opengl. {Title}", 854, 480);
		VulkanWindow = CreateWindow(GraphicsApi.Vulkan, $"vulkan. {Title}", 854, 480);
		SoftwareWindow = CreateWindow(GraphicsApi.Software, $"software. {Title}", 854, 480);
		NoGraphicsWindow = CreateWindow(GraphicsApi.None, $"no graphics. {Title}", 854, 480);

		// OpenGLWindowRenderTarget = new(this, OpenGLWindow);
		// VulkanWindowRenderTarget = new(this, VulkanWindow);
		// SoftwareWindowRenderTarget = new(this, SoftwareWindow);
		// NoGraphicsWindowRenderTarget = new(this, NoGraphicsWindow);
		//
		// TestRenderPass = new TestRenderPass(); // TODO do i want to share this or each have their own?
		//
		// OpenGLWindowRenderer = CreateRenderer(OpenGLWindowRenderTarget, TestRenderPass);
		// VulkanWindowWindowRenderer = CreateRenderer(VulkanWindowRenderTarget, TestRenderPass);
		// SoftwareWindowWindowRenderer = CreateRenderer(SoftwareWindowRenderTarget, TestRenderPass);
		// NoGraphicsWindowWindowRenderer = CreateRenderer(NoGraphicsWindowRenderTarget, TestRenderPass);
	}

	protected override void Update() { }

	protected override void Cleanup() { }
}