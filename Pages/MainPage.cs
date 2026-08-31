using FmgLib.MauiMarkup;
using UrunKarsilastirma.Models;
using UrunKarsilastirma.Services;
using System.Collections.ObjectModel;

namespace UrunKarsilastirma.Pages;

public class MainPage : ContentPage
{
    readonly Entry _aramaKutusu;
    readonly Button _araButonu;
    readonly ActivityIndicator _yukleniyor;
    readonly CollectionView _sonuclarListesi;
    readonly Label _durumLabel;
    readonly Button _artanBtn;
    readonly Button _azalanBtn;
    readonly ObservableCollection<Urun> _sonuclar = new();
    List<Urun> _tumSonuclar = new();
    readonly List<Button> _siteButonlari = new();
    readonly HashSet<string> _aktifSiteler = new();
    readonly ScraperService _scraper = new();

    bool _koyuTema = false;

    public MainPage()
    {
        Title = "Ürün Karşılaştırma";

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "🌙",
            Command = new Command(TemaDegistir)
        });

        _aramaKutusu = new Entry
        {
            Placeholder = "Ürün adı girin...",
            TextColor = Colors.Black,
            PlaceholderColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center
        };

        _araButonu = new Button
        {
            Text = "Ara",
            VerticalOptions = LayoutOptions.Center,
            WidthRequest = 90,
            BackgroundColor = Color.FromArgb("#007AFF"),
            TextColor = Colors.White
        };
        _araButonu.Clicked += AraButonu_Clicked;

        _yukleniyor = new ActivityIndicator
        {
            IsVisible = false,
            IsRunning = false,
            HorizontalOptions = LayoutOptions.Center,
            Color = Color.FromArgb("#007AFF")
        };

        _durumLabel = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Gray,
            IsVisible = false
        };

        _artanBtn = new Button
        {
            Text = "Fiyat: Artan",
            FontSize = 12,
            BackgroundColor = Color.FromArgb("#007AFF"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(10, 0)
        };
        _artanBtn.Clicked += (s, e) => SiralamaYap(true);

        _azalanBtn = new Button
        {
            Text = "Fiyat: Azalan",
            FontSize = 12,
            BackgroundColor = Color.FromArgb("#34C759"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(10, 0)
        };
        _azalanBtn.Clicked += (s, e) => SiralamaYap(false);

        _sonuclarListesi = new CollectionView
        {
            ItemsSource = _sonuclar,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var ad = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                ad.SetBinding(Label.TextProperty, nameof(Urun.Ad));

                var fiyatLabel = new Label
                {
                    TextColor = Color.FromArgb("#34C759"),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold
                };
                fiyatLabel.SetBinding(Label.TextProperty, nameof(Urun.Fiyat));

                var siteLabel = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                siteLabel.SetBinding(Label.TextProperty, nameof(Urun.Site));

                var stackLayout = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        ad,
                        fiyatLabel,
                        siteLabel,
                        new Label
                        {
                            Text = "Ürün sayfasını açmak için tıklayın",
                            FontSize = 10,
                            TextColor = Color.FromArgb("#007AFF")
                        }
                    }
                };

                return new Border
                {
                    Padding = new Thickness(12),
                    Margin = new Thickness(5),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(10)
                    },
                    Stroke = Color.FromArgb("#E5E5EA"),
                    BackgroundColor = Colors.White,
                    Content = stackLayout
                };
            })
        };
        _sonuclarListesi.SelectionChanged += SonuclarListesi_SelectionChanged;

        var aramaAlaniGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        aramaAlaniGrid.Add(_aramaKutusu, 0, 0);
        aramaAlaniGrid.Add(_araButonu, 1, 0);

        var aramaKutusuFrame = new Border
        {
            Padding = new Thickness(10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            BackgroundColor = Color.FromArgb("#F2F2F7"),
            Content = aramaAlaniGrid
        };

        var siralamaAlani = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Children = { _artanBtn, _azalanBtn }
        };

        // Site filtreleme chipleri
        var siteler = new[] { "Amazon TR", "Trendyol", "n11", "D\u0026R", "Kitap Yurdu", "Hepsiburada", "Morhipo" };
        var filtrePaneli = new VerticalStackLayout { Spacing = 6 };

        var filtreBaslik = new Label
        {
            Text = "Site Filtresi:",
            FontSize = 12,
            TextColor = Colors.Gray
        };
        filtrePaneli.Children.Add(filtreBaslik);

        var chipWrap = new HorizontalStackLayout
        {
            Spacing = 6
        };
        foreach (var site in siteler)
        {
            var chip = new Button
            {
                Text = site,
                FontSize = 11,
                CornerRadius = 14,
                HeightRequest = 30,
                Padding = new Thickness(10, 0),
                BackgroundColor = Color.FromArgb("#E5E5EA"),
                TextColor = Colors.Black
            };
            chip.Margin = new Thickness(0, 0, 6, 6);
            chip.Clicked += (s, e) => SiteFiltresiDegistir(site, chip);
            _siteButonlari.Add(chip);
            chipWrap.Children.Add(chip);
        }
        filtrePaneli.Children.Add(chipWrap);

        var grid = new Grid
        {
            Padding = new Thickness(15),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            RowSpacing = 10
        };
        grid.Add(aramaKutusuFrame, 0, 0);
        grid.Add(_durumLabel, 0, 1);
        grid.Add(siralamaAlani, 0, 2);
        grid.Add(filtrePaneli, 0, 3);
        grid.Add(_yukleniyor, 0, 4);
        grid.Add(_sonuclarListesi, 0, 5);

        Content = grid;

        _aramaKutusu.Completed += async (s, e) => await AraBaslat();
    }

    private void TemaDegistir()
    {
        _koyuTema = !_koyuTema;

        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = _koyuTema ? AppTheme.Dark : AppTheme.Light;
        }

        // ToolbarItem ikonunu guncelle
        if (ToolbarItems.Count > 0)
            ToolbarItems[0].Text = _koyuTema ? "☀️" : "🌙";
    }

    private void SiralamaYap(bool artan)
    {
        if (_tumSonuclar.Count == 0) return;

        // Site filtresi
        var filtreli = _aktifSiteler.Count == 0
            ? new List<Urun>(_tumSonuclar)
            : _tumSonuclar.Where(u => _aktifSiteler.Contains(u.Site)).ToList();

        // Fiyat siralamasi
        var sirali = artan
            ? filtreli.OrderBy(u => FiyatParse(u.Fiyat)).ToList()
            : filtreli.OrderByDescending(u => FiyatParse(u.Fiyat)).ToList();

        _sonuclar.Clear();
        foreach (var urun in sirali)
            _sonuclar.Add(urun);

        var filtreMetni = _aktifSiteler.Count > 0
            ? $" — Filtre: {string.Join(", ", _aktifSiteler)}"
            : "";
        _durumLabel.Text = $"{_sonuclar.Count} ürün — Fiyat: {(artan ? "Artan" : "Azalan")}{filtreMetni}";
    }

    private void SiteFiltresiDegistir(string site, Button chip)
    {
        if (_aktifSiteler.Contains(site))
        {
            _aktifSiteler.Remove(site);
            chip.BackgroundColor = Color.FromArgb("#E5E5EA");
            chip.TextColor = Colors.Black;
        }
        else
        {
            _aktifSiteler.Add(site);
            chip.BackgroundColor = Color.FromArgb("#007AFF");
            chip.TextColor = Colors.White;
        }
        FiltreleVeGoster();
    }

    private void FiltreleVeGoster()
    {
        if (_tumSonuclar.Count == 0) return;

        // Site filtresi
        var filtreli = _aktifSiteler.Count == 0
            ? new List<Urun>(_tumSonuclar)
            : _tumSonuclar.Where(u => _aktifSiteler.Contains(u.Site)).ToList();

        // Son siralama butonu durumunu kontrol et
        // Varsayilan: ekrandaki siralamayi koru
        _sonuclar.Clear();
        foreach (var urun in filtreli)
            _sonuclar.Add(urun);

        var filtreMetni = _aktifSiteler.Count > 0
            ? $" — Filtre: {string.Join(", ", _aktifSiteler)}"
            : "";
        _durumLabel.Text = $"{_sonuclar.Count} ürün{filtreMetni}";
    }

    private static double FiyatParse(string fiyat)
    {
        if (string.IsNullOrWhiteSpace(fiyat)) return double.MaxValue;

        try
        {
            // "1.661,55 TL" -> "1661.55"
            var temiz = fiyat.Replace("TL", "").Trim();
            temiz = temiz.Replace(".", "").Replace(",", ".");
            return double.Parse(temiz, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return double.MaxValue;
        }
    }

    private async void SonuclarListesi_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Urun urun)
        {
            if (!string.IsNullOrEmpty(urun.Link))
            {
                try
                {
                    await Browser.Default.OpenAsync(urun.Link, BrowserLaunchMode.SystemPreferred);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Hata", $"Link acilamadi: {ex.Message}", "Tamam");
                }
            }
            else
            {
                await DisplayAlertAsync("Bilgi", "Bu urunun linki bulunmuyor.", "Tamam");
            }

            if (sender is CollectionView cv)
                cv.SelectedItem = null;
        }
    }

    private async void AraButonu_Clicked(object? sender, EventArgs e)
    {
        await AraBaslat();
    }

    private async Task AraBaslat()
    {
        var aranan = _aramaKutusu.Text;
        if (string.IsNullOrWhiteSpace(aranan)) return;

        _yukleniyor.IsVisible = true;
        _yukleniyor.IsRunning = true;
        _araButonu.IsEnabled = false;
        _sonuclar.Clear();

        _durumLabel.Text = "Aranıyor...";
        _durumLabel.IsVisible = true;

        try
        {
            var sonuclar = await _scraper.AraAsync(aranan);
            _tumSonuclar = sonuclar;

            if (sonuclar.Count == 0)
            {
                _durumLabel.Text = "Sonuç bulunamadı.";
            }
            else
            {
                _durumLabel.Text = $"{sonuclar.Count} ürün bulundu.";
                foreach (var urun in sonuclar)
                    _sonuclar.Add(urun);
            }
        }
        catch (Exception ex)
        {
            _durumLabel.Text = $"Hata: {ex.Message}";
        }
        finally
        {
            _yukleniyor.IsRunning = false;
            _yukleniyor.IsVisible = false;
            _araButonu.IsEnabled = true;
        }
    }
}
