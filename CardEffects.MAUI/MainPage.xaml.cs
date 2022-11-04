using SkiaSharp.Views.Maui;
using SkiaSharp;
using static System.Diagnostics.Debug;

namespace CardEffects.MAUI;

public partial class MainPage : ContentPage
{
	const int Count = 20;
	List<SKBitmap> _bitmaps = new(Count);
	readonly HttpClient _client = new();
	readonly SKPaint _paint = new SKPaint
	{
		IsAntialias = true,
	};
	SKBitmap _selected;
	float _width, _height;
	float? _scale;
	SKMatrix _matrix;
	SKImageFilter _filter =
		// Blend mode
		SKImageFilter.CreateBlendMode(SKBlendMode.Plus, 
			// Specular light
			SKImageFilter.CreatePointLitSpecular(new SKPoint3(200, 540, 50), SKColors.White, surfaceScale: 1f, ks: 2f, shininess: 100f)
		);

	public MainPage()
	{
		InitializeComponent();

		for (int i = 1; i <= Count; i++)
		{
			//_ = DownloadAsync($"https://images.pokemontcg.io/sm35/{i}_hires.png");
			_ = DownloadAsync($"https://images.pokemontcg.io/swsh3/{i}_hires.png");
		}
	}

	async Task DownloadAsync(string url)
	{
		var path = Path.Combine(Path.GetTempPath(), $"pokemon-{Hash(url)}.png");

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

	/// <summary>
	/// Based on: https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
	/// </summary>
	static int Hash(string str)
	{
		unchecked
		{
			int hash1 = (5381 << 16) + 5381;
			int hash2 = hash1;

			for (int i = 0; i < str.Length; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ str[i];
				if (i == str.Length - 1)
					break;
				hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}

	void OnTouch(object sender, SKTouchEventArgs e)
	{
		if (e.ActionType == SKTouchAction.Released)
		{
			if (_selected == null)
			{
				int index = (int)(e.Location.X / _width * 4) + (int)(e.Location.Y / _height * 5) * 4;
				_selected = _bitmaps[index];
			}
			else
			{
				_selected = null;
			}
			_skia.InvalidateSurface();
			_matrix = SKMatrix.Identity;
		}
		else
		{
			// This code came from: https://learn.microsoft.com/xamarin/xamarin-forms/user-interface/graphics/skiasharp/transforms/3d-rotation

			// Find center of canvas
			float xCenter = _width / 2;
			float yCenter = _height / 2;

			// Translate center to origin
			SKMatrix matrix = SKMatrix.MakeTranslation(-xCenter, -yCenter);


			float depth = 500;
			var diff = new SKPoint((e.Location.X * 2 - _width) / _width, (e.Location.Y * 2 - _height) / _height);

			// Use 3D matrix for 3D rotations and perspective
			var matrix44 = SKMatrix44.CreateIdentity();
			matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(1, 0, 0, diff.Y * 10));    // x
			matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(0, 1, 0, diff.X * 10));    // y
			matrix44.PostConcat(SKMatrix44.CreateRotationDegrees(0, 0, 1, 0));              // z

			SKMatrix44 perspectiveMatrix = SKMatrix44.CreateIdentity();
			perspectiveMatrix[3, 2] = -1 / depth;
			matrix44.PostConcat(perspectiveMatrix);

			// Concatenate with 2D matrix
			SKMatrix.PostConcat(ref matrix, matrix44.Matrix);

			// Translate back to center
			SKMatrix.PostConcat(ref matrix, SKMatrix.MakeTranslation(xCenter, yCenter));

			_matrix = matrix;
			_skia.InvalidateSurface();
		}

		e.Handled = true;
	}

	void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
	{
		if (_scale == null)
		{
			_scale = (float)e.RawInfo.Width / (float)e.Info.Width;
		}
		_width = e.Info.Width;
		_height = e.Info.Height;

		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Black);
	
		lock (_bitmaps)
		{
			if (_selected != null)
			{
				DrawOne(e, canvas);
			}
			else
			{
				DrawAll(e, canvas);
			}
		}
	}

	void DrawOne(SKPaintSurfaceEventArgs e, SKCanvas canvas)
	{
		canvas.SetMatrix(_matrix);
		canvas.Scale(_scale ?? 1);

		var dest = new SKRect(0, 0, _width, _height);
		_paint.ImageFilter = _filter;
		canvas.DrawBitmap(_selected, dest, _paint);
	}

	void DrawAll(SKPaintSurfaceEventArgs e, SKCanvas canvas)
	{
		_paint.ImageFilter = null;
		var src = new SKRect();
		var dest = new SKRect(0, 0, _width / 4, _height / 5);
		var location = new SKPoint();
		for (int i = 0; i < _bitmaps.Count; i++)
		{
			var bitmap = _bitmaps[i];
			src.Right = bitmap.Width;
			src.Bottom = bitmap.Height;
			location.X = (i % 4) * dest.Size.Width;
			location.Y = (i / 4) * dest.Size.Height;
			dest.Location = location;
			canvas.DrawBitmap(bitmap, src, dest, _paint);
		}
	}
}

