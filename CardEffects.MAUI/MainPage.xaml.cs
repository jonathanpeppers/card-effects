namespace CardEffects.MAUI;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		const int count = 30;
		var list = new List<string>(count);
		for (int i = 1; i <= count; i++)
		{
			list.Add ($"https://images.pokemontcg.io/sm35/{i}_hires.png");
		}
		_collectionView.ItemsSource = list;
	}
}

