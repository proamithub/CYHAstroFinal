using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CYHAstro.Infrastructure.Reports;

public sealed class KundliPdfGenerator : IKundliPdfService
{
    private readonly ILogger<KundliPdfGenerator> _logger;

    public KundliPdfGenerator(ILogger<KundliPdfGenerator> logger)
    {
        _logger = logger;
        // Community License for open-source / developer usage
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public Task<Result<byte[]>> GenerateKundliPdfAsync(KundliChartDto chart, KundliRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var pdfBytes = Document.Create(container =>
            {
                // Page 1: Elegant Cover Page
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.Grey.Lighten5);

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(20).AlignCenter().Text("|| OM SHREE GANESHAYA NAMAHA ||")
                            .FontSize(15).Bold().FontColor("#B45309").LetterSpacing(2);

                        col.Item().PaddingTop(15).AlignCenter().Text("CYHAstro")
                            .FontSize(32).ExtraBold().FontColor("#1E1B4B");

                        col.Item().AlignCenter().Text("VEDIC ASTROLOGY & KUNDLI HOROSCOPE REPORT")
                            .FontSize(12).Bold().LetterSpacing(2).FontColor("#D97706");

                        col.Item().PaddingVertical(25).AlignCenter()
                            .LineHorizontal(2).LineColor("#D97706");

                        // Seeker Profile Box
                        col.Item().Background("#1E1B4B").Padding(24).Column(profileCol =>
                        {
                            profileCol.Item().AlignCenter().Text("HOROSCOPE OF")
                                .FontSize(11).FontColor("#FDE68A").LetterSpacing(2);

                            profileCol.Item().AlignCenter().Text(request.Name.ToUpperInvariant())
                                .FontSize(26).Bold().FontColor(Colors.White);

                            profileCol.Item().PaddingTop(15).LineHorizontal(1).LineColor("#4338CA");

                            profileCol.Item().PaddingTop(15).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn();
                                    cd.RelativeColumn();
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Date of Birth: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.DateOfBirth.ToString("dd MMMM yyyy")).FontColor(Colors.White).Bold().FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Time of Birth: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(DateTime.Today.Add(request.TimeOfBirth).ToString("hh:mm tt")).FontColor(Colors.White).Bold().FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Place of Birth: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.BirthPlace).FontColor(Colors.White).Bold().FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Gender: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.Gender == "F" ? "Female" : request.Gender == "M" ? "Male" : "Other").FontColor(Colors.White).Bold().FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Latitude: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.Latitude.ToString("F4")).FontColor(Colors.White).FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Longitude: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.Longitude.ToString("F4")).FontColor(Colors.White).FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Ayanamsha: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.Ayanamsha).FontColor(Colors.White).FontSize(10);
                                });

                                table.Cell().Padding(4).Text(t =>
                                {
                                    t.Span("Chart Style: ").FontColor("#94A3B8").FontSize(10);
                                    t.Span(request.ChartStyle).FontColor(Colors.White).FontSize(10);
                                });
                            });
                        });

                        // Quick Highlights Row
                        col.Item().PaddingTop(25).Row(row =>
                        {
                            row.Spacing(12);

                            row.RelativeItem().Background("#FEF3C7").Border(1).BorderColor("#F59E0B").Padding(12).Column(c =>
                            {
                                c.Item().AlignCenter().Text("ASCENDANT (LAGNA)").FontSize(9).Bold().FontColor("#92400E");
                                c.Item().AlignCenter().Text(chart.AscendantSign).FontSize(14).Bold().FontColor("#B45309");
                            });

                            row.RelativeItem().Background("#E0E7FF").Border(1).BorderColor("#6366F1").Padding(12).Column(c =>
                            {
                                c.Item().AlignCenter().Text("MOON SIGN (RASHI)").FontSize(9).Bold().FontColor("#3730A3");
                                c.Item().AlignCenter().Text(chart.MoonSign).FontSize(14).Bold().FontColor("#4338CA");
                            });

                            row.RelativeItem().Background("#ECFDF5").Border(1).BorderColor("#10B981").Padding(12).Column(c =>
                            {
                                c.Item().AlignCenter().Text("BIRTH NAKSHATRA").FontSize(9).Bold().FontColor("#065F46");
                                c.Item().AlignCenter().Text($"{chart.BirthNakshatra} (Pada {chart.BirthPada})").FontSize(13).Bold().FontColor("#047857");
                            });
                        });

                        col.Item().PaddingTop(35).AlignCenter().Text("Authentic Parashari Vedic Astrology Synthesis")
                            .FontSize(10).Italic().FontColor("#64748B");

                        col.Item().PaddingTop(10).AlignCenter().Text($"Report Generated: {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC | ID: {chart.PublicId}")
                            .FontSize(9).FontColor("#94A3B8");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("CYHAstro Platform — Confidential & Prepared for " + request.Name)
                            .FontSize(9).FontColor("#94A3B8");
                    });
                });

                // Page 2: Avakahada Chakra & Planetary Positions
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);

                    page.Header().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Kundli Report").Bold().FontColor("#1E1B4B").FontSize(10);
                        r.RelativeItem().AlignRight().Text($"Seeker: {request.Name}").FontColor("#64748B").FontSize(10);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Text("1. Avakahada Chakra & Dosha Profile")
                            .FontSize(16).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1);
                            });

                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Ascendant (Lagna)").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.AscendantSign).FontSize(10);
                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Moon Sign (Rashi)").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.MoonSign).FontSize(10);

                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Sun Sign").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.SunSign).FontSize(10);
                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Nakshatra / Pada").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text($"{chart.BirthNakshatra} - Pada {chart.BirthPada}").FontSize(10);

                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Mangal Dosha").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.MangalDoshaStatus).Bold()
                                .FontColor(chart.MangalDoshaStatus.Contains("Yes", StringComparison.OrdinalIgnoreCase) ? "#DC2626" : "#059669").FontSize(10);
                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Kaal Sarp Dosha").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.KaalSarpDoshaStatus).Bold()
                                .FontColor(chart.KaalSarpDoshaStatus.Contains("Yes", StringComparison.OrdinalIgnoreCase) ? "#DC2626" : "#059669").FontSize(10);

                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Sade Sati Status").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.SadeSatiStatus).FontSize(10);
                            table.Cell().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Text("Current Mahadasha").Bold().FontSize(10);
                            table.Cell().Border(1).BorderColor("#E2E8F0").Padding(8).Text(chart.CurrentDasha).FontSize(10);
                        });

                        col.Item().PaddingTop(25).Text("2. Planetary Positions (Graha Spashta)")
                            .FontSize(16).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1.4f);
                                cd.RelativeColumn(1.5f);
                                cd.RelativeColumn(1.2f);
                                cd.RelativeColumn(1.6f);
                                cd.RelativeColumn(0.8f);
                                cd.RelativeColumn(0.8f);
                                cd.RelativeColumn(0.8f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#1E1B4B").Padding(6).Text("Planet").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).Text("Rashi (Sign)").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).Text("Longitude").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).Text("Nakshatra").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).AlignCenter().Text("Pada").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).AlignCenter().Text("House").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background("#1E1B4B").Padding(6).AlignCenter().Text("Motion").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            foreach (var p in chart.Planets)
                            {
                                var degInSign = p.Longitude % 30m;
                                var deg = (int)degInSign;
                                var min = (int)((degInSign - deg) * 60m);

                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text($"{p.PlanetName} ({p.SanskritName})").Bold().FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(p.SignName).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text($"{deg}° {min}'").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(p.Nakshatra).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(p.Pada.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(p.House.ToString()).Bold().FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(p.IsRetrograde ? "R (Vakri)" : "Direct")
                                    .FontColor(p.IsRetrograde ? "#DC2626" : "#059669").FontSize(9);
                            }
                        });

                        col.Item().PaddingTop(25).Text("3. House Sign & Planetary Occupancy (Bhava Chakra)")
                            .FontSize(16).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                            });

                            foreach (var h in chart.Houses.OrderBy(x => x.HouseNumber))
                            {
                                table.Cell().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#CBD5E1").Padding(8).Column(c =>
                                {
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"House {h.HouseNumber}").Bold().FontSize(10).FontColor("#1E1B4B");
                                        r.RelativeItem().AlignRight().Text(h.SignName).FontSize(9).FontColor("#64748B");
                                    });

                                    var occupants = h.PlanetsInHouse.Any() 
                                        ? string.Join(", ", h.PlanetsInHouse) 
                                        : "Vacant (Aspects Apply)";

                                    c.Item().PaddingTop(4).Text(occupants)
                                        .FontSize(9).FontColor(h.PlanetsInHouse.Any() ? "#B45309" : "#94A3B8").Bold();
                                });
                            }
                        });
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Platform").FontSize(9).FontColor("#94A3B8");
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                // Page 3: Vedic Interpretation & Gemini AI Insights
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);

                    page.Header().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Kundli Report").Bold().FontColor("#1E1B4B").FontSize(10);
                        r.RelativeItem().AlignRight().Text($"Seeker: {request.Name}").FontColor("#64748B").FontSize(10);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Item().Text("4. Astrological Synthesis & Life Path Forecast")
                            .FontSize(16).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(5).Text("AI-Powered Parashari Synthesis powered by Google Gemini AI")
                            .FontSize(9).Italic().FontColor("#64748B");

                        var interpretation = !string.IsNullOrWhiteSpace(chart.AiInterpretation)
                            ? chart.AiInterpretation
                            : $"Based on the Ascendant {chart.AscendantSign} and Moon Sign {chart.MoonSign}, the native exhibits a resilient nature with acute intellect and balanced karmic disposition. The birth nakshatra {chart.BirthNakshatra} grants auspicious intuitive instincts and natural leadership tendencies.";

                        col.Item().PaddingTop(12).Background("#F1F5F9").Border(1).BorderColor("#CBD5E1").Padding(16)
                            .Text(interpretation)
                            .FontSize(10.5f).LineHeight(1.5f).FontColor("#1E293B");

                        // Vedic Remedies & Guidance Box
                        col.Item().PaddingTop(25).Text("5. Vedic Recommendations & Remedies")
                            .FontSize(16).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                            });

                            table.Cell().Padding(4).Background("#FEF3C7").Border(1).BorderColor("#F59E0B").Padding(12).Column(rc =>
                            {
                                rc.Item().Text("Favorable Gemstones").Bold().FontSize(11).FontColor("#92400E");
                                rc.Item().PaddingTop(4).Text("Consult with our verified astrologers before wearing gemstones. Primaries based on Lagna lord and 9th Bhagya lord.")
                                    .FontSize(9.5f).FontColor("#78350F");
                            });

                            table.Cell().Padding(4).Background("#E0E7FF").Border(1).BorderColor("#6366F1").Padding(12).Column(rc =>
                            {
                                rc.Item().Text("Mantra & Meditation").Bold().FontSize(11).FontColor("#3730A3");
                                rc.Item().PaddingTop(4).Text("Chanting Gayatri Mantra and Maha Mrityunjaya Mantra promotes mental tranquility and clears planetary afflictions.")
                                    .FontSize(9.5f).FontColor("#312E81");
                            });

                            table.Cell().Padding(4).Background("#ECFDF5").Border(1).BorderColor("#10B981").Padding(12).Column(rc =>
                            {
                                rc.Item().Text("Auspicious Activities (Daan)").Bold().FontSize(11).FontColor("#065F46");
                                rc.Item().PaddingTop(4).Text("Charity on Saturdays and Thursdays harmonizes planetary karma and reduces malefic influences.")
                                    .FontSize(9.5f).FontColor("#047857");
                            });

                            table.Cell().Padding(4).Background("#FDF2F8").Border(1).BorderColor("#EC4899").Padding(12).Column(rc =>
                            {
                                rc.Item().Text("Consultation Advice").Bold().FontSize(11).FontColor("#9D174D");
                                rc.Item().PaddingTop(4).Text("Connect with a verified CYHAstro astrologer via live Audio/Video call for custom dasha transition solutions.")
                                    .FontSize(9.5f).FontColor("#831843");
                            });
                        });

                        col.Item().PaddingTop(30).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(12).Column(disc =>
                        {
                            disc.Item().Text("Astrological Disclaimer").Bold().FontSize(9).FontColor("#475569");
                            disc.Item().PaddingTop(2).Text("This Janam Kundli report is calculated based on authentic ancient Vedic astrology principles and modern ephemeris algorithms. Astrological insights are intended for self-reflection and guidance. Life events are governed by individual karma, effort, and divine grace.")
                                .FontSize(8).FontColor("#64748B");
                        });
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Platform — https://cyhastro.com").FontSize(9).FontColor("#94A3B8");
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();

            return Task.FromResult(Result<byte[]>.Success(pdfBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Kundli PDF for seeker {Name}", request.Name);
            return Task.FromResult(Result<byte[]>.Failure($"PDF Generation failed: {ex.Message}"));
        }
    }
}
