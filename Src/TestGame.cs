using Engine4.Client;
using Engine4.Client.Rendering;

namespace Engine4.Test;

public class TestGame : GameClient {
	public Window? VulkanWindow0 { get; private set; }
	public Window? VulkanWindow1 { get; private set; }

	public WindowRenderTarget VulkanWindow0RenderTarget { get; private set; } = null!;
	public WindowRenderTarget VulkanWindow1RenderTarget { get; private set; } = null!;
	public ConsoleRenderTarget ConsoleRenderTarget { get; private set; } = null!;

	public Renderer VulkanWindow0Renderer { get; private set; } = null!;
	public Renderer VulkanWindow1Renderer { get; private set; } = null!;
	public Renderer ConsoleRenderer { get; private set; } = null!; // TODO only allow 1

	public RenderPass TestRenderPass { get; private set; } = null!; // TODO do i want to share this or each have their own?

	public TestGame(string[] args) : base(args, "Test Game", true, true) => OnSetupDoneEvent += OnSetupDone;

	private void OnSetupDone() {
		VulkanWindow0?.Show();
		VulkanWindow1?.Show();
	}

	protected override void Setup() {
		const string Title = "title goes here";

		Console.WriteLine(Title);

		VulkanWindow0 = CreateWindow($"vulkan 0. {Title}", 854, 480);
		VulkanWindow1 = CreateWindow($"vulkan 1. {Title}", 854, 480);

		// VulkanWindow0RenderTarget = new(VulkanWindow0);
		// VulkanWindow1RenderTarget = new(VulkanWindow1);
		// ConsoleRenderTarget = new(false); // TODO test both
		//
		// TestRenderPass = new TestRenderPass();
		//
		// VulkanWindow0Renderer = CreateRenderer(VulkanWindow0RenderTarget, TestRenderPass);
		// VulkanWindow1Renderer = CreateRenderer(VulkanWindow1RenderTarget, TestRenderPass);
		// ConsoleRenderer = CreateRenderer(ConsoleRenderTarget, TestRenderPass);
	}

	protected override void Update() { }

	protected override void Cleanup() { }
}