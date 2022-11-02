using SkiaSharp.Views.Maui;
using SkiaSharp;

namespace CardEffects.MAUI;

public partial class MainPage : ContentPage
{
	const int Count = 20;
	List<SKBitmap> _bitmaps = new(Count);
	HttpClient _client = new();
	SKPoint? _touch;

	public MainPage()
	{
		InitializeComponent();

		for (int i = 1; i <= Count; i++)
		{
			_ = DownloadAsync($"https://images.pokemontcg.io/sm35/{i}_hires.png");
		}
	}

	async Task DownloadAsync(string url)
	{
		var path = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));

		if (!File.Exists(path))
		{
			using var stream = await _client.GetStreamAsync(url).ConfigureAwait(false);
			using var file = File.Create(path);
			await stream.CopyToAsync(file);
		}

		var bitmap = SKBitmap.Decode(path);
		lock (_bitmaps)
		{
			_bitmaps.Add(bitmap);
			Dispatcher.Dispatch(_skia.InvalidateSurface);
		}
	}

	void OnTouch(object sender, SKTouchEventArgs e)
	{
		if (e.InContact)
			_touch = e.Location;
		else
			_touch = null;

		_skia.InvalidateSurface();

		e.Handled = true;
	}

	void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Black);
	
		lock (_bitmaps)
		{
			var src = new SKRect();
			var dest = new SKRect(0, 0, e.Info.Width / 4, e.Info.Height / 5);
			var location = new SKPoint();
			for (int i = 0; i < _bitmaps.Count; i++)
			{
				var bitmap = _bitmaps[i];
				src.Right = bitmap.Width;
				src.Bottom = bitmap.Height;
				location.X = (i % 4) * dest.Size.Width;
				location.Y = (i / 4) * dest.Size.Height;
				dest.Location = location;
				canvas.DrawBitmap(bitmap, src, dest);
			}
		}
	}
}

