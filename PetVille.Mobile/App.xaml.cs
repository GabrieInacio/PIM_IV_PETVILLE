using Microsoft.Extensions.DependencyInjection;

namespace PetVille.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Mantém controles nativos, texto digitado e seleção de datas no tema claro.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}