using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
public class SeoController : ControllerBase
{
    private readonly IListingRepository _listingRepository;

    // Базовите URL-и — смени ако API-то е на различен домейн
    private const string FrontendBase = "https://moto-zona.com";
    private const string ApiBase = "https://moto-zona.com"; // или твоя API домейн

    public SeoController(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    // ─────────────────────────────────────────────────────────────
    // ОБЯВА: /obiavi/{id}
    // Google crawl-ва това → вижда реални данни → индексира
    // Потребителят → meta refresh го праща към ListingDetails.html
    // ─────────────────────────────────────────────────────────────
    [HttpGet("/obiavi/{id:long}")]
    public async Task<IActionResult> ListingPage(long id)
    {
        var listing = await _listingRepository.GetPublicDetailsAsync(id);
        if (listing == null)
            return NotFound(BuildNotFoundHtml());

        // Не броим view при SEO crawl
        var title = HtmlEncode(listing.Title);
        var brand = HtmlEncode(listing.BrandName ?? "");
        var model = HtmlEncode(listing.ModelName ?? listing.ItemModelText ?? "");
        var year = listing.VehicleYear?.ToString() ?? "";
        var price = listing.PriceEUR.ToString("0") + " EUR";
        var mileage = listing.Mileage.HasValue ? $"{listing.Mileage:N0} км" : "";
        var cc = listing.EngineCC.HasValue ? $"{listing.EngineCC} cc" : "";
        var hp = listing.HorsePower.HasValue ? $"{listing.HorsePower} к.с." : "";
        var category = HtmlEncode(listing.MainCategoryName ?? "Обява");
        var subCategory = HtmlEncode(listing.SubCategoryName ?? "");
        var country = HtmlEncode(listing.CountryName ?? "България");
        var region = HtmlEncode(listing.RegionName ?? "");
        var city = HtmlEncode(listing.CityName ?? "");
        var condition = HtmlEncode(listing.ConditionName ?? "");
        var licenseCategory = HtmlEncode(listing.LicenseCategoryName ?? "");
        var promotion = listing.CurrentPromotionType;

        // Описание — максимум 160 символа за meta
        var rawDesc = listing.Description ?? "";
        var metaDesc = rawDesc.Length > 160
            ? HtmlEncode(rawDesc[..157]) + "..."
            : HtmlEncode(rawDesc);

        // Пълно описание за страницата
        var fullDesc = HtmlEncode(rawDesc);

        // Снимка за og:image
        var mainPhoto = listing.Photos.FirstOrDefault(p => p.IsMain)?.FileUrl
                     ?? listing.Photos.FirstOrDefault()?.FileUrl
                     ?? $"{FrontendBase}/ImagesVideos/MzLogoSquare.png";

        // Всички снимки за body
        var photosHtml = string.Join("\n", listing.Photos.Select(p =>
            $"""<img src="{HtmlEncode(p.FileUrl)}" alt="{title}" loading="lazy" style="max-width:100%;margin:4px;">"""));

        // Локация
        var location = string.Join(", ", new[] { city, region, country }
            .Where(s => !string.IsNullOrEmpty(s)));

        // Seller info
        var sellerName = HtmlEncode(listing.Seller?.DisplayName ?? "Продавач");
        var sellerType = HtmlEncode(listing.Seller?.SellerTypeLabel ?? "");
        var sellerPhone = listing.Seller?.Phone ?? "";

        // Structured data (JSON-LD) за Google
        var jsonLd = $$"""
        {
          "@context": "https://schema.org",
          "@type": "Product",
          "name": "{{EscapeJson(listing.Title)}}",
          "description": "{{EscapeJson(rawDesc.Length > 500 ? rawDesc[..500] : rawDesc)}}",
          "image": "{{EscapeJson(mainPhoto)}}",
          "brand": {
            "@type": "Brand",
            "name": "{{EscapeJson(listing.BrandName ?? "")}}"
          },
          "offers": {
            "@type": "Offer",
            "priceCurrency": "EUR",
            "price": "{{listing.PriceEUR:0.00}}",
            "availability": "https://schema.org/InStock",
            "seller": {
              "@type": "{{(listing.Seller?.AccountType == "COMPANY" ? "Organization" : "Person")}}",
              "name": "{{EscapeJson(listing.Seller?.DisplayName ?? "Продавач")}}"
            }
          }
        }
        """;

        // SEO заглавие
        var pageTitle = $"{title}";
        if (!string.IsNullOrEmpty(brand) && !string.IsNullOrEmpty(model))
            pageTitle = $"{brand} {model} {year} — {price} | Мото Зона";
        else
            pageTitle = $"{title} — {price} | Мото Зона";

        // Хлебни трохи (breadcrumb)
        var categorySlug = GetCategorySlug(listing.MainCategoryName);
        var breadcrumbHtml = $"""
            <nav aria-label="breadcrumb">
              <a href="{FrontendBase}/">Начало</a> &rsaquo;
              <a href="{FrontendBase}/obiavi/{categorySlug}">{category}</a> &rsaquo;
              <span>{title}</span>
            </nav>
        """;

        // Технически характеристики
        var specs = new List<(string Label, string Value)>();
        if (!string.IsNullOrEmpty(brand)) specs.Add(("Марка", brand));
        if (!string.IsNullOrEmpty(model)) specs.Add(("Модел", model));
        if (!string.IsNullOrEmpty(year)) specs.Add(("Година", year));
        if (!string.IsNullOrEmpty(mileage)) specs.Add(("Пробег", mileage));
        if (!string.IsNullOrEmpty(cc)) specs.Add(("Кубатура", cc));
        if (!string.IsNullOrEmpty(hp)) specs.Add(("Мощност", hp));
        if (!string.IsNullOrEmpty(licenseCategory)) specs.Add(("Категория", licenseCategory));
        if (!string.IsNullOrEmpty(condition)) specs.Add(("Състояние", condition));
        if (!string.IsNullOrEmpty(subCategory)) specs.Add(("Тип", subCategory));
        if (!string.IsNullOrEmpty(location)) specs.Add(("Локация", location));
        if (!string.IsNullOrEmpty(promotion) && promotion != "NORMAL")
            specs.Add(("Промоция", promotion));

        var specsHtml = string.Join("\n", specs.Select(s =>
            $"<tr><th>{s.Label}</th><td>{s.Value}</td></tr>"));

        var html = $$"""
        <!DOCTYPE html>
        <html lang="bg">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <meta name="google" content="notranslate">

          <title>{{pageTitle}}</title>
          <meta name="description" content="{{metaDesc}}">
          <link rel="canonical" href="{{FrontendBase}}/obiavi/{{id}}">

          <!-- Open Graph -->
          <meta property="og:type" content="product">
          <meta property="og:title" content="{{pageTitle}}">
          <meta property="og:description" content="{{metaDesc}}">
          <meta property="og:image" content="{{HtmlEncode(mainPhoto)}}">
          <meta property="og:url" content="{{FrontendBase}}/obiavi/{{id}}">
          <meta property="og:site_name" content="Мото Зона">
          <meta property="og:locale" content="bg_BG">

          <!-- Twitter Card -->
          <meta name="twitter:card" content="summary_large_image">
          <meta name="twitter:title" content="{{pageTitle}}">
          <meta name="twitter:description" content="{{metaDesc}}">
          <meta name="twitter:image" content="{{HtmlEncode(mainPhoto)}}">

          <!-- Пренасочване към реалния фронтенд -->
          <meta http-equiv="refresh" content="0; url={{FrontendBase}}/ListingDetails.html?id={{id}}">

          <!-- JSON-LD Structured Data -->
          <script type="application/ld+json">{{jsonLd}}</script>

          <style>
            body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
            h1 { font-size: 24px; }
            .price { font-size: 28px; font-weight: bold; color: #ff6a2a; }
            table { border-collapse: collapse; width: 100%; margin: 16px 0; }
            th { text-align: left; padding: 8px; background: #f5f5f5; width: 140px; }
            td { padding: 8px; border-bottom: 1px solid #eee; }
            .photos { display: flex; flex-wrap: wrap; gap: 8px; margin: 16px 0; }
            .seller { background: #f9f9f9; padding: 16px; border-radius: 8px; margin: 16px 0; }
            nav { margin-bottom: 16px; color: #888; font-size: 14px; }
            nav a { color: #888; text-decoration: none; }
            .redirect-msg { color: #888; font-size: 13px; margin-top: 20px; }
          </style>
        </head>
        <body>
          {{breadcrumbHtml}}

          <h1>{{title}}</h1>
          <div class="price">{{price}}</div>

          <div class="photos">{{photosHtml}}</div>

          <table>{{specsHtml}}</table>

          {{(string.IsNullOrEmpty(fullDesc) ? "" : $"<h2>Описание</h2><p>{fullDesc}</p>")}}

          <div class="seller">
            <strong>{{sellerType}}</strong>
            <p>{{sellerName}}</p>
          </div>

          <p class="redirect-msg">
            Зареждаме страницата... Ако не се пренасочи автоматично,
            <a href="{{FrontendBase}}/ListingDetails.html?id={{id}}">натисни тук</a>.
          </p>
        </body>
        </html>
        """;

        return Content(html, "text/html; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────
    // КАТЕГОРИЙНИ СТРАНИЦИ
    // /obiavi/motori, /obiavi/ekipirovka, /obiavi/chasti, /obiavi/aksesоari
    // ─────────────────────────────────────────────────────────────
    [HttpGet("/obiavi/{category}")]
    public IActionResult CategoryPage(string category)
    {
        var (title, description, h1) = category.ToLowerInvariant() switch
        {
            "motori" => (
                "Мотори втора ръка — обяви за мотоциклети в България | Мото Зона",
                "Купи или продай мотоциклет в България. Над хиляди обяви за нови и употребявани мотори на ниски цени. Филтрирай по марка, модел, цена и локация.",
                "Мотори за продажба в България"
            ),
            "ekipirovka" => (
                "Мото екипировка втора употреба — каски, якета, ръкавици | Мото Зона",
                "Купи или продай мото екипировка в България. Каски, якета, ботуши, ръкавици, панталони на добри цени.",
                "Мото екипировка — обяви"
            ),
            "chasti" => (
                "Мото части втора употреба — ауспуси, кормила, двигатели | Мото Зона",
                "Обяви за мото части в България. Ауспуси, фарове, двигатели, спирачки, гуми и много повече.",
                "Мото части — обяви"
            ),
            "aksesоari" or "aksesоari" or "aksesoari" => (
                "Мото аксесоари — куфари, чанти, интеркоми | Мото Зона",
                "Обяви за мото аксесоари в България. Топкейси, странични чанти, интеркоми, стойки за телефон.",
                "Мото аксесоари — обяви"
            ),
            _ => (null, null, null)
        };

        if (title == null)
            return NotFound(BuildNotFoundHtml());

        var html = $$"""
        <!DOCTYPE html>
        <html lang="bg">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{title}}</title>
          <meta name="description" content="{{description}}">
          <link rel="canonical" href="{{FrontendBase}}/obiavi/{{category}}">
          <meta property="og:title" content="{{title}}">
          <meta property="og:description" content="{{description}}">
          <meta property="og:url" content="{{FrontendBase}}/obiavi/{{category}}">
          <meta property="og:site_name" content="Мото Зона">
          <meta property="og:locale" content="bg_BG">
          <meta http-equiv="refresh" content="0; url={{FrontendBase}}/?category={{category}}">
          <style>
            body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
          </style>
        </head>
        <body>
          <nav><a href="{{FrontendBase}}/">Начало</a> &rsaquo; <span>{{h1}}</span></nav>
          <h1>{{h1}}</h1>
          <p>{{description}}</p>
          <p><a href="{{FrontendBase}}/?category={{category}}">Виж всички обяви</a></p>
        </body>
        </html>
        """;

        return Content(html, "text/html; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────
    // SITEMAP.XML
    // Google го чете и индексира всички обяви автоматично
    // ─────────────────────────────────────────────────────────────
    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        // Вземи последните 5000 обяви (Google препоръчва max 50 000 на sitemap)
        var request = new MotoMarket.Api.Models.Requests.PublicListingSearchRequest
        {
            Page = 1,
            PageSize = 5000,
            SortBy = "newest"
        };

        var listings = await _listingRepository.SearchPublicAsync(request);

        var staticUrls = new[]
        {
            (Url: $"{FrontendBase}/", Priority: "1.0", Freq: "daily"),
            (Url: $"{FrontendBase}/obiavi/motori", Priority: "0.9", Freq: "daily"),
            (Url: $"{FrontendBase}/obiavi/ekipirovka", Priority: "0.9", Freq: "daily"),
            (Url: $"{FrontendBase}/obiavi/chasti", Priority: "0.9", Freq: "daily"),
            (Url: $"{FrontendBase}/obiavi/aksesoari", Priority: "0.9", Freq: "daily"),
        };

        var staticXml = string.Join("\n", staticUrls.Select(u => $"""
          <url>
            <loc>{u.Url}</loc>
            <changefreq>{u.Freq}</changefreq>
            <priority>{u.Priority}</priority>
          </url>
        """));

        var listingsXml = string.Join("\n", listings.Select(l => $"""
          <url>
            <loc>{FrontendBase}/obiavi/{l.Id}</loc>
            <lastmod>{l.PublishedAt:yyyy-MM-dd}</lastmod>
            <changefreq>weekly</changefreq>
            <priority>0.7</priority>
          </url>
        """));

        var xml = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
        {staticXml}
        {listingsXml}
        </urlset>
        """;

        return Content(xml, "application/xml; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────
    // ROBOTS.TXT
    // ─────────────────────────────────────────────────────────────
    [HttpGet("/robots.txt")]
    public IActionResult RobotsTxt()
    {
        var content = $"""
        User-agent: *
        Allow: /

        Sitemap: {FrontendBase}/sitemap.xml
        """;

        return Content(content, "text/plain");
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────
    private static string HtmlEncode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return System.Net.WebUtility.HtmlEncode(value);
    }

    private static string EscapeJson(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string GetCategorySlug(string? categoryName)
    {
        return categoryName?.ToLowerInvariant() switch
        {
            "мотори" or "vehicle" => "motori",
            "екипировка" or "gear" => "ekipirovka",
            "части" or "part" => "chasti",
            "аксесоари" or "accessory" => "aksesoari",
            _ => "motori"
        };
    }

    private static string BuildNotFoundHtml() => """
        <!DOCTYPE html>
        <html lang="bg">
        <head>
          <meta charset="UTF-8">
          <title>Обявата не е намерена | Мото Зона</title>
        </head>
        <body>
          <h1>Обявата не е намерена</h1>
          <p><a href="https://moto-zona.com/">Към началото</a></p>
        </body>
        </html>
        """;
}