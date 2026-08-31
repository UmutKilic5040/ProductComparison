using HtmlAgilityPack;
using UrunKarsilastirma.Models;

namespace UrunKarsilastirma.Services;

public class ScraperService
{
    private readonly HttpClient _http;

    public ScraperService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<List<Urun>> AraAsync(string aranan)
    {
        var gorevler = new List<Task<List<Urun>>>
        {
            SafeAraAsync(AmazonTRAraAsync, aranan),
            SafeAraAsync(TrendyolAraAsync, aranan),
            SafeAraAsync(N11AraAsync, aranan),
            SafeAraAsync(DRAraAsync, aranan),
            SafeAraAsync(KitapYurduAraAsync, aranan),
            SafeAraAsync(HepsiburadaAraAsync, aranan),
            SafeAraAsync(MorhipoAraAsync, aranan)
        };

        var tumSonuclar = await Task.WhenAll(gorevler);
        var sonuclar = new List<Urun>();
        foreach (var s in tumSonuclar)
            sonuclar.AddRange(s);
        return sonuclar;
    }

    private async Task<List<Urun>> SafeAraAsync(Func<string, Task<List<Urun>>> fonksiyon, string aranan)
    {
        try { return await fonksiyon(aranan); }
        catch { return new List<Urun>(); }
    }

    // ===== AMAZON TR =====
    private async Task<List<Urun>> AmazonTRAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.amazon.com.tr/s?k={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[@data-component-type='s-search-result']");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                // Urun adi: h2 > span icinde
                var ad = kart.SelectSingleNode(".//h2/span")?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(ad)) continue;

                // Fiyat
                var fiyatDugumu = kart.SelectSingleNode(".//span[@class='a-price-whole']");
                var fiyatKesir = kart.SelectSingleNode(".//span[@class='a-price-fraction']");
                string fiyat;
                if (fiyatDugumu != null)
                {
                    fiyat = fiyatDugumu.InnerText.Trim();
                    if (fiyatKesir != null)
                        fiyat += "," + fiyatKesir.InnerText.Trim();
                    fiyat += " TL";
                }
                else
                {
                    fiyat = "Fiyat bilinmiyor";
                }

                // Link: s-line-clamp-4 class'li a tag'i veya h2 icindeki a
                var linkDugumu = kart.SelectSingleNode(".//a[contains(@class,'s-line-clamp')]")
                             ?? kart.SelectSingleNode(".//h2/a")
                             ?? kart.SelectSingleNode(".//a[@class='a-link-normal s-no-outline']")
                             ?? kart.SelectSingleNode(".//a[contains(@href,'/dp/')]");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.amazon.com.tr" + link;

                liste.Add(new Urun { Ad = ad, Fiyat = fiyat, Site = "Amazon TR", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== TRENDYOL =====
    private async Task<List<Urun>> TrendyolAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.trendyol.com/sr?q={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'p-card-wrppr')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//div[contains(@class,'prdct-desc-cntnr-ttl')]")
                      ?? kart.SelectSingleNode(".//span[@class='prdct-desc-cntnr-name']")
                      ?? kart.SelectSingleNode(".//div[contains(@class,'product-name')]");
                var adMetni = ad?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//div[contains(@class,'prc-box-dscntc')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'prc-box-sllng')]")
                        ?? kart.SelectSingleNode(".//span[contains(@class,'prc-dsc')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.trendyol.com" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "Trendyol", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== N11 =====
    private async Task<List<Urun>> N11AraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.n11.com/arama?q={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'productListItem')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//li[contains(@class,'column')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//a[contains(@class,'productName')]")
                      ?? kart.SelectSingleNode(".//span[@class='productName']")
                      ?? kart.SelectSingleNode(".//a[contains(@class,'product-name')]");
                var adMetni = ad?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//span[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'price')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.n11.com" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "n11", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== D&R =====
    private async Task<List<Urun>> DRAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.dr.com.tr/search?q={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-item')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-card')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//li[contains(@class,'product')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//a[contains(@class,'product-name')]")
                      ?? kart.SelectSingleNode(".//a[contains(@class,'name')]")
                      ?? kart.SelectSingleNode(".//h3");
                var adMetni = ad?.InnerText.Trim() ?? ad?.GetAttributeValue("title", "") ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//span[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//span[contains(@class,'fiyat')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'price')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.dr.com.tr" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "D&R", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== KITAP YURDU =====
    private async Task<List<Urun>> KitapYurduAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.kitapyurdu.com/index.php?filter_name={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-item')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//li[contains(@class,'product')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//a[contains(@class,'name')]")
                      ?? kart.SelectSingleNode(".//span[contains(@class,'name')]")
                      ?? kart.SelectSingleNode(".//div[contains(@class,'name')]");
                var adMetni = ad?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//span[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//span[contains(@class,'price-new')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'price')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.kitapyurdu.com" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "Kitap Yurdu", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== HEPSIBURADA =====
    private async Task<List<Urun>> HepsiburadaAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.hepsiburada.com/ara?q={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//li[contains(@class,'productListContent-item')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'searchItemContent')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//li[contains(@class,'moria-product')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//span[contains(@class,'product-name')]")
                      ?? kart.SelectSingleNode(".//h3")
                      ?? kart.SelectSingleNode(".//div[contains(@class,'product-name')]");
                var adMetni = ad?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//span[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//span[contains(@data-test-id,'price')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.hepsiburada.com" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "Hepsiburada", Link = link });
            }
            catch { }
        }
        return liste;
    }

    // ===== MORHIPO (GittiGidiyor yerine) =====
    private async Task<List<Urun>> MorhipoAraAsync(string aranan)
    {
        var liste = new List<Urun>();
        var url = $"https://www.morhipo.com/arama?q={Uri.EscapeDataString(aranan)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-card')]");
        if (kartlar == null)
            kartlar = doc.DocumentNode.SelectNodes("//div[contains(@class,'productItem')]");
        if (kartlar == null) return liste;

        foreach (var kart in kartlar)
        {
            try
            {
                var ad = kart.SelectSingleNode(".//span[contains(@class,'productName')]")
                      ?? kart.SelectSingleNode(".//a[contains(@class,'name')]")
                      ?? kart.SelectSingleNode(".//h3");
                var adMetni = ad?.InnerText.Trim() ?? "";
                if (string.IsNullOrEmpty(adMetni)) continue;

                var fiyat = kart.SelectSingleNode(".//span[contains(@class,'price')]")
                        ?? kart.SelectSingleNode(".//div[contains(@class,'price')]");
                var fiyatMetni = fiyat?.InnerText.Trim() ?? "Fiyat bilinmiyor";

                var linkDugumu = kart.SelectSingleNode(".//a");
                var link = linkDugumu?.GetAttributeValue("href", "") ?? "";
                if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    link = "https://www.morhipo.com" + link;

                liste.Add(new Urun { Ad = adMetni, Fiyat = fiyatMetni, Site = "Morhipo", Link = link });
            }
            catch { }
        }
        return liste;
    }
}
