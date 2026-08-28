using Engine4.Graphics;
using Engine4.Graphics.Rendering;
using Engine4.Graphics.Windowing;

namespace Engine4.Test;

public class TestGame : GameClient {
	public Window VulkanWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer VulkanWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass VulkanWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public Window OpenGLWindow { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer OpenGLWindowRenderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass OpenGLWindowRenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public TestGame(Engine engine) : base(engine, "Test Game", GraphicsApis.Vulkan | GraphicsApis.OpenGL, true) => OnSetupDoneEvent += OnSetupDone;

	private void OnSetupDone() {
		Console.WriteLine("done");

		VulkanWindow = CreateWindow(GraphicsApi.Vulkan, "vulkan. title goes here", 854, 480); // TODO this action requires OpenTK. how do i handle that? opentk flag?
		WindowRenderTarget vulkanRenderTarget = new(this, VulkanWindow);

		OpenGLWindow = CreateWindow(GraphicsApi.OpenGL, "opengl. title goes here", 854, 480); // TODO this action requires OpenTK. how do i handle that? opentk flag?
		WindowRenderTarget openglRenderTarget = new(this, OpenGLWindow);

		// VulkanWindowRenderPass = new();
		// OpenGLWindowRenderPass = new();
		//
		// VulkanWindowRenderer = CreateRenderer(vulkanRenderTarget, VulkanWindowRenderPass);
		// OpenGLWindowRenderer = CreateRenderer(openglRenderTarget, OpenGLWindowRenderPass);

		VulkanWindow.Show();
		OpenGLWindow.Show();

		Console.WriteLine("show");

		// RequestShutdown();
	}

	protected override void Update() {
		//
	}
}