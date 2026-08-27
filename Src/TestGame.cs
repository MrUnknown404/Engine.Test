using Engine4.Graphics;
using Engine4.Graphics.Rendering;
using Engine4.Graphics.Windowing;
using OpenTK.Platform;
using GraphicsApi = Engine4.Graphics.GraphicsApi;

namespace Engine4.Test;

public class TestGame : GameClient {
	public Window Window { get => field ?? throw new NullReferenceException(); private set; }
	public Renderer Renderer { get => field ?? throw new NullReferenceException(); private set; }
	public TestRenderPass RenderPass { get => field ?? throw new NullReferenceException(); private set; }

	public TestGame(Engine engine) : base(engine, engine.GraphicsApi switch {
			GraphicsApi.None => null,
			GraphicsApi.OpenGL => new OpenGLGraphicsApiHints(),
			GraphicsApi.Vulkan => new VulkanGraphicsApiHints(),
			GraphicsApi.Software => null,
			_ => throw new ArgumentOutOfRangeException(),
	}) =>
			OnSetupDoneEvent += OnSetupDone;

	private void OnSetupDone() {
		Console.WriteLine("done");

		Window = CreateWindow("title goes here", 854, 480);
		WindowRenderTarget renderTarget = new(Window); // TODO this action requires OpenTK. how do i handle that? opentk flag?

		RenderPass = new();
		Renderer = CreateRenderer(renderTarget, RenderPass);

		Window.Show();

		// RequestShutdown();
	}

	protected override void Update() {
		//
	}
}