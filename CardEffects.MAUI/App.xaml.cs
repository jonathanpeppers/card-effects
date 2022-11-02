namespace CardEffects.MAUI;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		var window = base.CreateWindow(activationState);
		window.Width = 754;
		window.Height = 1044;
		window.X = (1920 - window.Width) / 2;
		window.Y = (1080 - window.Height) / 2;
		return window;
	}
}
