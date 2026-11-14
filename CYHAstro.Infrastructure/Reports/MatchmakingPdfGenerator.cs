using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CYHAstro.Infrastructure.Reports;

public sealed class MatchmakingPdfGenerator : IMatchmakingPdfService
{
    private readonly ILogger<MatchmakingPdfGenerator> _logger;

    public MatchmakingPdfGenerator(ILogger<MatchmakingPdfGenerator> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public Task<Result<byte[]>> GenerateMatchmakingPdfAsync(MatchmakingResultDto result, MatchmakingRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var pdfBytes = Document.Create(container =>
            {
                // Page 1: Vivah Milan Summary & Compatibility Analysis
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.Grey.Lighten5);

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text("|| OM SHREE GANESHAYA NAMAHA ||")
                            .FontSize(13).Bold().FontColor("#B45309").LetterSpacing(2);

                        col.Item().PaddingTop(6).AlignCenter().Text("CYHAstro")
                            .FontSize(28).ExtraBold().FontColor("#1E1B4B");

                        col.Item().AlignCenter().Text("VEDIC ASHTAKOOT KUNDLI MILAN REPORT (VIVAH MATCHMAKING)")
                            .FontSize(11).Bold().LetterSpacing(1.5f).FontColor("#D97706");

                        col.Item().PaddingVertical(14).LineHorizontal(1.5f).LineColor("#D97706");

                        // Groom and Bride Profile Comparison
                        col.Item().Row(r =>
                        {
                            r.Spacing(14);

                            // Groom Card
                            r.RelativeItem().Background("#1E1B4B").Padding(16).Column(gc =>
                            {
                                gc.Item().AlignCenter().Text("GROOM (VAR)").FontSize(10).FontColor("#FDE68A").Bold().LetterSpacing(1.5f);
                                gc.Item().AlignCenter().Text(request.MaleName.ToUpperInvariant())
                                    .FontSize(16).Bold().FontColor(Colors.White);

                                gc.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor("#4338CA");

                                gc.Item().PaddingTop(8).Text(t =>
                                {
                                    t.Span("DOB: ").FontColor("#94A3B8").FontSize(9.5f);
                                    t.Span(request.MaleDob.ToString("dd MMMM yyyy")).FontColor(Colors.White).Bold().FontSize(9.5f);
                                });

                                gc.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Birth Time: ").FontColor("#94A3B8").FontSize(9.5f);
                                    t.Span(DateTime.Today.Add(request.MaleTob).ToString("hh:mm tt")).FontColor(Colors.White).FontSize(9.5f);
                                });

                                gc.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Birth Place: ").FontColor("#94A3B8").FontSize(9.5f);
                                    t.Span(!string.IsNullOrWhiteSpace(request.MalePlace) ? request.MalePlace : $"{request.MaleLat:F2}°N, {request.MaleLon:F2}°E").FontColor(Colors.White).FontSize(9.5f);
                                });

                                if (result.MaleChart != null)
                                {
                                    gc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Lagna / Rashi: ").FontColor("#94A3B8").FontSize(9.5f);
                                        t.Span($"{result.MaleChart.AscendantSign} / {result.MaleChart.MoonSign}").FontColor("#FDE68A").Bold().FontSize(9.5f);
                                    });
                                    gc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Nakshatra: ").FontColor("#94A3B8").FontSize(9.5f);
                                        t.Span($"{result.MaleChart.BirthNakshatra} (Pada {result.MaleChart.BirthPada})").FontColor(Colors.White).FontSize(9.5f);
                                    });
                                    gc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Manglik: ").FontColor("#94A3B8").FontSize(9.5f);
                                        t.Span(result.MaleManglik ? "Manglik (Mars in H" + (result.MaleChart.Planets.FirstOrDefault(p => p.PlanetName == "Mars")?.House) + ")" : "Non-Manglik").FontColor(result.MaleManglik ? "#FCA5A5" : "#86EFAC").Bold().FontSize(9.5f);
                                    });
                                }
                            });

                            // Bride Card
                            r.RelativeItem().Background("#311432").Padding(16).Column(bc =>
                            {
                                bc.Item().AlignCenter().Text("BRIDE (KANYA)").FontSize(10).FontColor("#FBCFE8").Bold().LetterSpacing(1.5f);
                                bc.Item().AlignCenter().Text(request.FemaleName.ToUpperInvariant())
                                    .FontSize(16).Bold().FontColor(Colors.White);

                                bc.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor("#831843");

                                bc.Item().PaddingTop(8).Text(t =>
                                {
                                    t.Span("DOB: ").FontColor("#F472B6").FontSize(9.5f);
                                    t.Span(request.FemaleDob.ToString("dd MMMM yyyy")).FontColor(Colors.White).Bold().FontSize(9.5f);
                                });

                                bc.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Birth Time: ").FontColor("#F472B6").FontSize(9.5f);
                                    t.Span(DateTime.Today.Add(request.FemaleTob).ToString("hh:mm tt")).FontColor(Colors.White).FontSize(9.5f);
                                });

                                bc.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Birth Place: ").FontColor("#F472B6").FontSize(9.5f);
                                    t.Span(!string.IsNullOrWhiteSpace(request.FemalePlace) ? request.FemalePlace : $"{request.FemaleLat:F2}°N, {request.FemaleLon:F2}°E").FontColor(Colors.White).FontSize(9.5f);
                                });

                                if (result.FemaleChart != null)
                                {
                                    bc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Lagna / Rashi: ").FontColor("#F472B6").FontSize(9.5f);
                                        t.Span($"{result.FemaleChart.AscendantSign} / {result.FemaleChart.MoonSign}").FontColor("#FDE68A").Bold().FontSize(9.5f);
                                    });
                                    bc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Nakshatra: ").FontColor("#F472B6").FontSize(9.5f);
                                        t.Span($"{result.FemaleChart.BirthNakshatra} (Pada {result.FemaleChart.BirthPada})").FontColor(Colors.White).FontSize(9.5f);
                                    });
                                    bc.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Manglik: ").FontColor("#F472B6").FontSize(9.5f);
                                        t.Span(result.FemaleManglik ? "Manglik (Mars in H" + (result.FemaleChart.Planets.FirstOrDefault(p => p.PlanetName == "Mars")?.House) + ")" : "Non-Manglik").FontColor(result.FemaleManglik ? "#FCA5A5" : "#86EFAC").Bold().FontSize(9.5f);
                                    });
                                }
                            });
                        });

                        // Score Card & Conclusion
                        col.Item().PaddingTop(16).Background(Colors.White).Border(1.5f).BorderColor("#D97706").Padding(18).Column(sc =>
                        {
                            sc.Item().AlignCenter().Text("TOTAL ASHTAKOOT GUNAS MATCH SCORE").FontSize(11).Bold().FontColor("#78350F").LetterSpacing(1);

                            sc.Item().AlignCenter().Text(t =>
                            {
                                t.Span(result.TotalScore.ToString("F1")).FontSize(36).ExtraBold().FontColor(result.TotalScore >= 18 ? "#059669" : "#DC2626");
                                t.Span(" / 36 Gunas").FontSize(14).Bold().FontColor("#64748B");
                            });

                            sc.Item().PaddingTop(6).AlignCenter().Text(t =>
                            {
                                t.Span("Recommendation: ").FontSize(11).FontColor("#475569");
                                t.Span(result.Conclusion).FontSize(12).Bold().FontColor(result.TotalScore >= 18 ? "#059669" : "#DC2626");
                            });

                            // Critical Dosha Indicators
                            sc.Item().PaddingTop(12).Row(dr =>
                            {
                                dr.Spacing(8);

                                dr.RelativeItem().Background(result.HasNadiDosha ? "#FEE2E2" : "#ECFDF5")
                                    .Border(1).BorderColor(result.HasNadiDosha ? "#EF4444" : "#10B981")
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("Nadi Koota (8 Pts)").FontSize(8.5f).Bold()
                                            .FontColor(result.HasNadiDosha ? "#991B1B" : "#065F46");
                                        c.Item().AlignCenter().Text(result.HasNadiDosha ? "Nadi Dosha Present" : "Auspicious (No Dosha)")
                                            .FontSize(9).Bold().FontColor(result.HasNadiDosha ? "#DC2626" : "#059669");
                                    });

                                dr.RelativeItem().Background(result.HasBhakootDosha ? "#FEE2E2" : "#ECFDF5")
                                    .Border(1).BorderColor(result.HasBhakootDosha ? "#EF4444" : "#10B981")
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("Bhakoot Koota (7 Pts)").FontSize(8.5f).Bold()
                                            .FontColor(result.HasBhakootDosha ? "#991B1B" : "#065F46");
                                        c.Item().AlignCenter().Text(result.HasBhakootDosha ? "Bhakoot Dosha Present" : "Auspicious (No Dosha)")
                                            .FontSize(9).Bold().FontColor(result.HasBhakootDosha ? "#DC2626" : "#059669");
                                    });

                                dr.RelativeItem().Background(result.HasGanaDosha ? "#FEF3C7" : "#ECFDF5")
                                    .Border(1).BorderColor(result.HasGanaDosha ? "#F59E0B" : "#10B981")
                                    .Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("Gana Koota (6 Pts)").FontSize(8.5f).Bold()
                                            .FontColor(result.HasGanaDosha ? "#92400E" : "#065F46");
                                        c.Item().AlignCenter().Text(result.HasGanaDosha ? "Needs Temperament Harmony" : "Auspicious Match")
                                            .FontSize(9).Bold().FontColor(result.HasGanaDosha ? "#D97706" : "#059669");
                                    });
                            });

                            // Manglik Compatibility Row
                            if (!string.IsNullOrWhiteSpace(result.ManglikConclusion))
                            {
                                sc.Item().PaddingTop(8).Background(result.MaleManglik != result.FemaleManglik ? "#FEF2F2" : "#F0FDF4")
                                    .Border(1).BorderColor(result.MaleManglik != result.FemaleManglik ? "#F87171" : "#4ADE80")
                                    .Padding(8).AlignCenter().Text(t =>
                                    {
                                        t.Span("Manglik Compatibility: ").Bold().FontSize(9f).FontColor(result.MaleManglik != result.FemaleManglik ? "#991B1B" : "#166534");
                                        t.Span(result.ManglikConclusion).FontSize(9f).FontColor(result.MaleManglik != result.FemaleManglik ? "#B91C1C" : "#15803D");
                                    });
                            }
                        });

                        // Ashtakoot Guna Breakdown Table
                        col.Item().PaddingTop(18).Text("Ashtakoot (8 Dimensions) Detailed Breakdown")
                            .FontSize(13).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1.8f);
                                cd.RelativeColumn(4.0f);
                                cd.RelativeColumn(1.2f);
                                cd.RelativeColumn(1.2f);
                            });

                            table.Header(th =>
                            {
                                th.Cell().Background("#1E1B4B").Padding(6).Text("Koota (Guna)").Bold().FontColor(Colors.White).FontSize(9);
                                th.Cell().Background("#1E1B4B").Padding(6).Text("Significance in Marriage").Bold().FontColor(Colors.White).FontSize(9);
                                th.Cell().Background("#1E1B4B").Padding(6).AlignCenter().Text("Max Pts").Bold().FontColor(Colors.White).FontSize(9);
                                th.Cell().Background("#1E1B4B").Padding(6).AlignCenter().Text("Obtained").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            int rowIdx = 0;
                            foreach (var g in result.GunaBreakdown)
                            {
                                var bg = (rowIdx++ % 2 == 0) ? "#FFFFFF" : "#F8FAFC";

                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(g.GunaName).Bold().FontSize(9).FontColor("#1E1B4B");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).Text(g.Description).FontSize(8.5f).FontColor("#475569");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(g.MaxPoints.ToString("F0")).FontSize(9).FontColor("#64748B");
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(g.ObtainedPoints.ToString("F1")).Bold().FontSize(9)
                                    .FontColor(g.ObtainedPoints > 0 ? "#059669" : "#DC2626");
                            }

                            // Total Footer Row
                            table.Cell().ColumnSpan(2).Background("#FEF3C7").BorderTop(1.5f).BorderColor("#D97706").Padding(6).Text("TOTAL SCORE").Bold().FontSize(10).FontColor("#92400E");
                            table.Cell().Background("#FEF3C7").BorderTop(1.5f).BorderColor("#D97706").Padding(6).AlignCenter().Text("36.0").Bold().FontSize(10).FontColor("#92400E");
                            table.Cell().Background("#FEF3C7").BorderTop(1.5f).BorderColor("#D97706").Padding(6).AlignCenter().Text(result.TotalScore.ToString("F1")).Bold().FontSize(10)
                                .FontColor(result.TotalScore >= 18 ? "#059669" : "#DC2626");
                        });
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Platform — Authentic Vedic Astrology").FontSize(8.5f).FontColor("#94A3B8");
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                // Page 2: AI Verdict, Vedic Remedies & Planetary Wisdom
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);

                    page.Header().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Ashtakoot Matchmaking Report").Bold().FontColor("#1E1B4B").FontSize(9);
                        r.RelativeItem().AlignRight().Text($"Groom: {request.MaleName} & Bride: {request.FemaleName}").FontColor("#64748B").FontSize(9);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        // AI Astrological Synthesis
                        if (!string.IsNullOrWhiteSpace(result.AiVerdict))
                        {
                            col.Item().Background("#EEF2FF").Border(1).BorderColor("#6366F1").Padding(14).Column(aiCol =>
                            {
                                aiCol.Item().Row(ar =>
                                {
                                    ar.RelativeItem().Text("Gemini AI Astrological Verdict & Couple Dynamics")
                                        .FontSize(12).Bold().FontColor("#3730A3");
                                });

                                aiCol.Item().PaddingTop(8).Text(result.AiVerdict)
                                    .FontSize(9.5f).FontColor("#1E1B4B").LineHeight(1.4f);
                            });
                        }

                        // Detailed Vedic Interpretation of 8 Kootas
                        col.Item().PaddingTop(16).Text("Vedic Significance & Marital Harmony Guidelines")
                            .FontSize(13).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(8).Table(guideTable =>
                        {
                            guideTable.ColumnsDefinition(gcd =>
                            {
                                gcd.RelativeColumn();
                                gcd.RelativeColumn();
                            });

                            guideTable.Cell().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Nadi Koota (Genetic & Health Alignment)").Bold().FontSize(10).FontColor("#1E1B4B");
                                c.Item().PaddingTop(3).Text("Carries 8 points. Represents biological, hereditary and health compatibility, ensuring progeny and longevity.")
                                    .FontSize(8.5f).FontColor("#64748B");
                            });

                            guideTable.Cell().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Bhakoot Koota (Love & Family Prosperity)").Bold().FontSize(10).FontColor("#1E1B4B");
                                c.Item().PaddingTop(3).Text("Carries 7 points. Represents emotional bonding, financial prosperity and longevity of mutual romance.")
                                    .FontSize(8.5f).FontColor("#64748B");
                            });

                            guideTable.Cell().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Gana Koota (Temperament & Behavior)").Bold().FontSize(10).FontColor("#1E1B4B");
                                c.Item().PaddingTop(3).Text("Carries 6 points. Measures mental compatibility across Deva, Manushya, and Rakshasa temperaments.")
                                    .FontSize(8.5f).FontColor("#64748B");
                            });

                            guideTable.Cell().Padding(4).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Graha Maitri (Mental Harmony)").Bold().FontSize(10).FontColor("#1E1B4B");
                                c.Item().PaddingTop(3).Text("Carries 5 points. Evaluates natural friendship between planetary lords of both Moon signs.")
                                    .FontSize(8.5f).FontColor("#64748B");
                            });
                        });

                        // Recommended Vedic Remedies (Vivah Shanti)
                        col.Item().PaddingTop(16).Text("Recommended Vedic Remedies for Marital Bliss")
                            .FontSize(13).Bold().FontColor("#1E1B4B");

                        col.Item().PaddingTop(8).Table(remTable =>
                        {
                            remTable.ColumnsDefinition(rcd =>
                            {
                                rcd.RelativeColumn();
                                rcd.RelativeColumn();
                            });

                            remTable.Cell().Padding(4).Background("#FEF3C7").Border(1).BorderColor("#F59E0B").Padding(10).Column(c =>
                            {
                                c.Item().Text("Gauri-Shankar Puja & Abhishek").Bold().FontSize(10).FontColor("#92400E");
                                c.Item().PaddingTop(3).Text("Performing Shiva-Parvati Rudrabhishek with milk and honey brings immense mutual harmony and mitigates Bhakoot or Gana conflicts.")
                                    .FontSize(8.5f).FontColor("#78350F");
                            });

                            remTable.Cell().Padding(4).Background("#ECFDF5").Border(1).BorderColor("#10B981").Padding(10).Column(c =>
                            {
                                c.Item().Text("Maha Mrityunjaya Japa").Bold().FontSize(10).FontColor("#065F46");
                                c.Item().PaddingTop(3).Text("In case of Nadi Dosha, chanting or having verified Vedic pandits chant the Maha Mrityunjaya mantra wards off adverse health influences.")
                                    .FontSize(8.5f).FontColor("#047857");
                            });

                            remTable.Cell().Padding(4).Background("#FDF2F8").Border(1).BorderColor("#EC4899").Padding(10).Column(c =>
                            {
                                c.Item().Text("Navagraha Shanti & Daan").Bold().FontSize(10).FontColor("#9D174D");
                                c.Item().PaddingTop(3).Text("Performing Navagraha homa and donating yellow clothes, grains or ghee on Thursdays/Fridays strengthens auspicious planetary rays.")
                                    .FontSize(8.5f).FontColor("#831843");
                            });

                            remTable.Cell().Padding(4).Background("#F0FDF4").Border(1).BorderColor("#22C55E").Padding(10).Column(c =>
                            {
                                c.Item().Text("Consult Verified CYHAstro Astrologer").Bold().FontSize(10).FontColor("#166534");
                                c.Item().PaddingTop(3).Text("For complete chart analysis including Navamsha (D9) and Manglik Dosha matching, book a live call with an experienced Acharya.")
                                    .FontSize(8.5f).FontColor("#14532D");
                            });
                        });

                        col.Item().PaddingTop(20).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Column(disc =>
                        {
                            disc.Item().Text("Astrological Disclaimer").Bold().FontSize(8.5f).FontColor("#475569");
                            disc.Item().PaddingTop(2).Text("This Kundli Milan report calculates Ashtakoot Guna Milan strictly in accordance with traditional Vedic principles. Marriage success is also deeply influenced by mutual respect, understanding, and shared life values. This report provides divine planetary guidance.")
                                .FontSize(7.5f).FontColor("#64748B");
                        });
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text("CYHAstro Platform — https://cyhastro.patnaorthoclinic.com").FontSize(8.5f).FontColor("#94A3B8");
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
            _logger.LogError(ex, "Failed to generate Matchmaking PDF for Groom {Groom} and Bride {Bride}", request.MaleName, request.FemaleName);
            return Task.FromResult(Result<byte[]>.Failure($"PDF Generation failed: {ex.Message}"));
        }
    }
}
