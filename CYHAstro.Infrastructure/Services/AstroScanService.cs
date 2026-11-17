using System.Text.Json;
using System.Text.RegularExpressions;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Scans;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class AstroScanService : IAstroScanService
{
    private readonly IAstroScanRepository _scanRepo;
    private readonly IAiAstrologyService _aiAstrologyService;
    private readonly IBirthProfileRepository _birthProfileRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IHostEnvironment _env;
    private readonly ILogger<AstroScanService> _logger;

    public AstroScanService(
        IAstroScanRepository scanRepo,
        IAiAstrologyService aiAstrologyService,
        IBirthProfileRepository birthProfileRepo,
        IWalletRepository walletRepo,
        IHostEnvironment env,
        ILogger<AstroScanService> logger)
    {
        _scanRepo = scanRepo;
        _aiAstrologyService = aiAstrologyService;
        _birthProfileRepo = birthProfileRepo;
        _walletRepo = walletRepo;
        _env = env;
        _logger = logger;
    }

    public async Task<Result<AstroScanDetailDto>> ProcessPalmScanAsync(
        long userId,
        Stream imageStream,
        string fileName,
        string contentType,
        string handType = "RIGHT",
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default)
    {
        var settings = await _scanRepo.GetSettingsAsync(ct);
        if (!settings.IsPalmScanEnabled)
        {
            return Result<AstroScanDetailDto>.Failure("हस्तरेखा स्कैन सेवा वर्तमान में मेंटेनेंस के लिए बंद है। कृपया कुछ समय बाद प्रयास करें।");
        }

        // 1. Check Wallet & Pricing
        decimal costToCharge = 0.00m;
        int completedScans = await _scanRepo.GetUserScanCountAsync(userId, "PALM", ct);
        if (completedScans >= settings.FreeScansPerUser && settings.PalmScanPrice > 0)
        {
            costToCharge = settings.PalmScanPrice;
            var wallet = await _walletRepo.GetByUserIdAsync(userId, ct);
            if (wallet == null || (wallet.PaidBalance + wallet.BonusBalance) < costToCharge)
            {
                return Result<AstroScanDetailDto>.Failure($"आपके वॉलेट में पर्याप्त बैलेंस नहीं है। स्कैन के लिए ₹{costToCharge} आवश्यक है। कृपया वॉलेट रिचार्ज करें।");
            }
        }

        // 2. Read and save image
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, ct);
        var imageBytes = memoryStream.ToArray();

        if (imageBytes.Length == 0)
        {
            return Result<AstroScanDetailDto>.Failure("कृपया वैध हथेली की फोटो अपलोड करें।");
        }

        var publicId = Guid.NewGuid().ToString("N");
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var savedFileName = $"palm_{publicId}{ext}";
        var relativeUrl = $"/uploads/scans/{savedFileName}";
        
        await SaveImageToDiskAsync(imageBytes, savedFileName, ct);

        // 3. Gather Kundli Context if BirthProfileId is provided
        string kundliContext = string.Empty;
        if (birthProfileId.HasValue && birthProfileId.Value > 0)
        {
            var profile = await _birthProfileRepo.GetByIdAsync(birthProfileId.Value, ct);
            if (profile != null)
            {
                kundliContext = $"User Kundli Details: Name: {profile.Name}, DOB: {profile.DateOfBirth:yyyy-MM-dd}, TOB: {profile.TimeOfBirth}, Place: {profile.BirthPlace}.";
            }
        }

        // 4. Build AI Multimodal Prompt
        var systemInstruction = !string.IsNullOrWhiteSpace(settings.PalmSystemPrompt)
            ? settings.PalmSystemPrompt
            : "You are a master Vedic Palmistry (Hastarekha) and Samudrika Shastra expert. Provide a highly detailed, authentic, and culturally respectful Vedic palm analysis.";

        var prompt = BuildPalmPrompt(handType, kundliContext, language);

        // 5. Call Gemini Vision API
        var visionResult = await _aiAstrologyService.AnalyzeVisionAsync(
            systemInstruction,
            prompt,
            imageBytes,
            contentType,
            settings.GeminiModel,
            ct);

        if (!visionResult.IsSuccess)
        {
            _logger.LogError("Gemini Vision Palm Scan failed for user {UserId}: {Error}", userId, visionResult.Error);
            return Result<AstroScanDetailDto>.Failure("हथेली की फोटो का विश्लेषण करने में तकनीकी त्रुटि हुई। कृपया स्पष्ट प्रकाश में ली गई फोटो के साथ पुनः प्रयास करें।");
        }

        // 6. Parse JSON Report
        var cleanJson = CleanJsonResponse(visionResult.Value ?? string.Empty);
        AstroScanReportDto report;
        try
        {
            report = JsonSerializer.Deserialize<AstroScanReportDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? FallbackPalmReport(language);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse structured JSON from Gemini Vision, using raw text fallback");
            report = FallbackPalmReport(language, visionResult.Value);
        }

        report.ScanType = "PALM";

        // 7. Debit Wallet if applicable
        if (costToCharge > 0)
        {
            var debitResult = await _walletRepo.DebitAsync(
                userId,
                costToCharge,
                "ASTROSCAN",
                0,
                $"हस्तरेखा स्कैन (ID: {publicId})",
                $"SCAN_{publicId}",
                ct);

            if (!debitResult.IsSuccess)
            {
                _logger.LogWarning("Failed to debit wallet for user {UserId} after scan: {Error}", userId, debitResult.Error);
            }
        }

        // 8. Save Scan Record in DB
        var extractedJson = JsonSerializer.Serialize(report.PrimaryFeatures);
        var reportJson = JsonSerializer.Serialize(report);

        await _scanRepo.InsertScanAsync(
            publicId,
            userId,
            birthProfileId,
            "PALM",
            handType,
            relativeUrl,
            "COMPLETED",
            (decimal?)report.OverallAuspiciousnessScore,
            extractedJson,
            reportJson,
            costToCharge,
            tokensUsed: 1200,
            language,
            ct);

        var resultDto = await _scanRepo.GetByPublicIdAsync(publicId, ct);
        return resultDto != null
            ? Result<AstroScanDetailDto>.Success(resultDto)
            : Result<AstroScanDetailDto>.Failure("स्कैन सेव करने में त्रुटि हुई।");
    }

    public async Task<Result<AstroScanDetailDto>> ProcessHeadScanAsync(
        long userId,
        Stream imageStream,
        string fileName,
        string contentType,
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default)
    {
        var settings = await _scanRepo.GetSettingsAsync(ct);
        if (!settings.IsHeadScanEnabled)
        {
            return Result<AstroScanDetailDto>.Failure("मस्तक एवं सामुद्रिक शास्त्र स्कैन सेवा वर्तमान में मेंटेनेंस के लिए बंद है।");
        }

        // 1. Check Wallet & Pricing
        decimal costToCharge = 0.00m;
        int completedScans = await _scanRepo.GetUserScanCountAsync(userId, "HEAD", ct);
        if (completedScans >= settings.FreeScansPerUser && settings.HeadScanPrice > 0)
        {
            costToCharge = settings.HeadScanPrice;
            var wallet = await _walletRepo.GetByUserIdAsync(userId, ct);
            if (wallet == null || (wallet.PaidBalance + wallet.BonusBalance) < costToCharge)
            {
                return Result<AstroScanDetailDto>.Failure($"आपके वॉलेट में पर्याप्त बैलेंस नहीं है। स्कैन के लिए ₹{costToCharge} आवश्यक है। कृपया वॉलेट रिचार्ज करें।");
            }
        }

        // 2. Read and save image
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, ct);
        var imageBytes = memoryStream.ToArray();

        if (imageBytes.Length == 0)
        {
            return Result<AstroScanDetailDto>.Failure("कृपया वैध चेहरे/माथे की फोटो अपलोड करें।");
        }

        var publicId = Guid.NewGuid().ToString("N");
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var savedFileName = $"head_{publicId}{ext}";
        var relativeUrl = $"/uploads/scans/{savedFileName}";

        await SaveImageToDiskAsync(imageBytes, savedFileName, ct);

        // 3. Kundli Context
        string kundliContext = string.Empty;
        if (birthProfileId.HasValue && birthProfileId.Value > 0)
        {
            var profile = await _birthProfileRepo.GetByIdAsync(birthProfileId.Value, ct);
            if (profile != null)
            {
                kundliContext = $"User Kundli Details: Name: {profile.Name}, DOB: {profile.DateOfBirth:yyyy-MM-dd}, TOB: {profile.TimeOfBirth}, Place: {profile.BirthPlace}.";
            }
        }

        // 4. Build AI Multimodal Prompt for Samudrika Shastra
        var systemInstruction = !string.IsNullOrWhiteSpace(settings.HeadSystemPrompt)
            ? settings.HeadSystemPrompt
            : "You are an authority on Vedic Samudrika Shastra (Face & Forehead reading). Provide deep physiognomic and planetary insight grounded in classical Indian traditions.";

        var prompt = BuildHeadPrompt(kundliContext, language);

        // 5. Call Gemini Vision API
        var visionResult = await _aiAstrologyService.AnalyzeVisionAsync(
            systemInstruction,
            prompt,
            imageBytes,
            contentType,
            settings.GeminiModel,
            ct);

        if (!visionResult.IsSuccess)
        {
            _logger.LogError("Gemini Vision Head Scan failed for user {UserId}: {Error}", userId, visionResult.Error);
            return Result<AstroScanDetailDto>.Failure("मस्तक की फोटो का विश्लेषण करने में तकनीकी त्रुटि हुई। कृपया उचित प्रकाश में सीधे चेहरे की फोटो लें।");
        }

        // 6. Parse JSON Report
        var cleanJson = CleanJsonResponse(visionResult.Value ?? string.Empty);
        AstroScanReportDto report;
        try
        {
            report = JsonSerializer.Deserialize<AstroScanReportDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? FallbackHeadReport(language);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse structured JSON from Gemini Vision for head scan");
            report = FallbackHeadReport(language, visionResult.Value);
        }

        report.ScanType = "HEAD";

        // 7. Debit Wallet if applicable
        if (costToCharge > 0)
        {
            await _walletRepo.DebitAsync(
                userId,
                costToCharge,
                "ASTROSCAN",
                0,
                $"सामुद्रिक मस्तक स्कैन (ID: {publicId})",
                $"SCAN_{publicId}",
                ct);
        }

        // 8. Save to DB
        var extractedJson = JsonSerializer.Serialize(report.PrimaryFeatures);
        var reportJson = JsonSerializer.Serialize(report);

        await _scanRepo.InsertScanAsync(
            publicId,
            userId,
            birthProfileId,
            "HEAD",
            null,
            relativeUrl,
            "COMPLETED",
            (decimal?)report.OverallAuspiciousnessScore,
            extractedJson,
            reportJson,
            costToCharge,
            tokensUsed: 1200,
            language,
            ct);

        var resultDto = await _scanRepo.GetByPublicIdAsync(publicId, ct);
        return resultDto != null
            ? Result<AstroScanDetailDto>.Success(resultDto)
            : Result<AstroScanDetailDto>.Failure("स्कैन सेव करने में त्रुटि हुई।");
    }

    public async Task<Result<AstroScanDetailDto>> GetUserScanAsync(long userId, string publicId, CancellationToken ct = default)
    {
        var scan = await _scanRepo.GetByPublicIdAsync(publicId, ct);
        if (scan == null || scan.UserId != userId)
        {
            return Result<AstroScanDetailDto>.Failure("स्कैन रिपोर्ट नहीं मिली।");
        }
        return Result<AstroScanDetailDto>.Success(scan);
    }

    public async Task<Result<List<AstroScanListItemDto>>> GetUserScanHistoryAsync(long userId, CancellationToken ct = default)
    {
        var list = await _scanRepo.GetUserScansAsync(userId, ct);
        return Result<List<AstroScanListItemDto>>.Success(list);
    }

    public async Task<Result<AstroScanSettingsDto>> GetPublicSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _scanRepo.GetSettingsAsync(ct);
        return Result<AstroScanSettingsDto>.Success(settings);
    }

    public async Task<Result<PagedResult<AstroScanDetailDto>>> AdminGetAllScansAsync(
        string? scanType,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var result = await _scanRepo.AdminGetScansAsync(scanType, search, page, pageSize, ct);
        return Result<PagedResult<AstroScanDetailDto>>.Success(result);
    }

    public async Task<Result<AstroScanDetailDto>> AdminGetScanDetailAsync(string publicId, CancellationToken ct = default)
    {
        var scan = await _scanRepo.GetByPublicIdAsync(publicId, ct);
        if (scan == null)
        {
            return Result<AstroScanDetailDto>.Failure("स्कैन रिकॉर्ड नहीं मिला।");
        }
        return Result<AstroScanDetailDto>.Success(scan);
    }

    public async Task<Result<bool>> AdminUpdateReviewNotesAsync(
        string publicId,
        string adminNotes,
        string reviewerName,
        CancellationToken ct = default)
    {
        var updated = await _scanRepo.UpdateAdminReviewAsync(publicId, adminNotes, ct);
        return updated
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("नोट्स अपडेट करने में विफल रहा।");
    }

    public async Task<Result<AstroScanSettingsDto>> AdminGetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _scanRepo.GetSettingsAsync(ct);
        return Result<AstroScanSettingsDto>.Success(settings);
    }

    public async Task<Result<bool>> AdminUpdateSettingsAsync(
        AstroScanSettingsDto settings,
        string adminName,
        CancellationToken ct = default)
    {
        var updated = await _scanRepo.UpdateSettingsAsync(settings, adminName, ct);
        return Result<bool>.Success(updated);
    }

    private async Task SaveImageToDiskAsync(byte[] imageBytes, string fileName, CancellationToken ct)
    {
        var webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "scans");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var fullPath = Path.Combine(uploadsDir, fileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes, ct);
    }

    private static string CleanJsonResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return "{}";
        var cleaned = response.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }
        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }
        return cleaned.Trim();
    }

    private static string BuildPalmPrompt(string handType, string kundliContext, string language)
    {
        return $@"Analyze this {handType} palm photo using authentic Vedic Hastarekha Shastra (Palmistry).
{(string.IsNullOrWhiteSpace(kundliContext) ? "" : "User Kundli Context: " + kundliContext)}
Language required: {(language == "hi" ? "Hindi (हिंदी)" : "English")}.

Examine the image thoroughly and return STRICT JSON matching this schema:
{{
  ""title"": ""वैदिक हस्तरेखा विस्तृत विश्लेषण"",
  ""handOrFaceType"": ""हाथ का तत्व प्रकार (जैसे पृथ्वी तत्व / अग्नि तत्व हथेली)"",
  ""overviewSummary"": ""3-4 वाक्यों में हथेली का समग्र सारांश और व्यक्तित्व"",
  ""overallAuspiciousnessScore"": 88,
  ""primaryFeatures"": [
    {{
      ""name"": ""जीवन रेखा (Life Line)"",
      ""sanskritName"": ""आयुष्य रेखा"",
      ""status"": ""गहरी, सुस्पष्ट एवं अखंडित"",
      ""significance"": ""दीर्घायु, उत्तम स्वास्थ्य और ऊर्जा का सूचक"",
      ""details"": ""आपकी जीवन रेखा शुक्र पर्वत को गोलाई में घेरती हुई स्पष्ट है, जो मजबूत जीवन शक्ति दर्शाती है।"",
      ""strengthScore"": 90,
      ""auspiciousSign"": ""शुभ त्रिकोण चिह्न""
    }},
    {{
      ""name"": ""मस्तिष्क रेखा (Head Line)"",
      ""sanskritName"": ""मातृ रेखा / धी रेखा"",
      ""status"": ""लंबी और चंद्र पर्वत की ओर झुकी हुई"",
      ""significance"": ""कुशाग्र बुद्धि, तार्किक क्षमता और रचनात्मकता"",
      ""details"": ""मस्तिष्क रेखा का झुकाव गहन चिंतन और कलात्मक दृष्टिकोण को दर्शाता है।"",
      ""strengthScore"": 85,
      ""auspiciousSign"": null
    }},
    {{
      ""name"": ""हृदय रेखा (Heart Line)"",
      ""sanskritName"": ""पितृ रेखा / हृदय रेखा"",
      ""status"": ""गुरु पर्वत तक फैली हुई"",
      ""significance"": ""उच्च आदर्श, भावनात्मक संतुलन और प्रेम में निष्ठा"",
      ""details"": ""हृदय रेखा का गुरु पर्वत तक पहुंचना दयालु स्वभाव और समाज में सम्मान दिलाता है।"",
      ""strengthScore"": 88,
      ""auspiciousSign"": null
    }},
    {{
      ""name"": ""भाग्य रेखा (Fate Line)"",
      ""sanskritName"": ""कर्म रेखा / भाग्य रेखा"",
      ""status"": ""मणिबंध से शनि पर्वत तक सीधी"",
      ""significance"": ""करियर में निरंतर प्रगति और स्वावलंबन"",
      ""details"": ""32 वर्ष की आयु के बाद भाग्य रेखा में विशेष उभार दिखाई देता है।"",
      ""strengthScore"": 82,
      ""auspiciousSign"": ""मत्स्य (Fish) चिह्न""
    }}
  ],
  ""secondaryFeatures"": [
    {{
      ""name"": ""गुरु पर्वत (Jupiter Mount)"",
      ""sanskritName"": ""बृहस्पति क्षेत्र"",
      ""status"": ""उभरा हुआ और पुष्ट"",
      ""significance"": ""ज्ञान, नेतृत्व क्षमता और आध्यात्मिक रुझान"",
      ""details"": ""गुरु पर्वत पर क्रॉस या शुभ उभार उच्च पद प्राप्ति का संकेत देता है।"",
      ""strengthScore"": 86,
      ""auspiciousSign"": ""स्वास्तिक या क्रॉस""
    }},
    {{
      ""name"": ""सूर्य पर्वत एवं रेखा (Sun Mount)"",
      ""sanskritName"": ""रवि क्षेत्र"",
      ""status"": ""स्पष्ट सूर्य रेखा के साथ"",
      ""significance"": ""यश, सरकारी मान-सम्मान और प्रसिद्धि"",
      ""details"": ""सूर्य पर्वत की मजबूती समाज में नाम और प्रतिष्ठा दिलाती है।"",
      ""strengthScore"": 84,
      ""auspiciousSign"": null
    }},
    {{
      ""name"": ""शुक्र पर्वत (Venus Mount)"",
      ""sanskritName"": ""शुक्र क्षेत्र"",
      ""status"": ""सुडौल एवं आभा युक्त"",
      ""significance"": ""आकर्षण, भौतिक सुख-सुविधाएं एवं दांपत्य सुख"",
      ""details"": ""विलासिता और कला के प्रति प्रेम का स्पष्ट संकेत।"",
      ""strengthScore"": 85,
      ""auspiciousSign"": null
    }}
  ],
  ""lifeAspects"": [
    {{
      ""category"": ""Career"",
      ""hindiTitle"": ""करियर, व्यापार एवं पद-प्रतिष्ठा"",
      ""prediction"": ""शनि और सूर्य पर्वत के बल से स्वतंत्र व्यवसाय या प्रशासनिक भूमिका में सफलता के प्रबल योग हैं। 30वें वर्ष के बाद विशेष उन्नति होगी।"",
      ""auspiciousAgeRange"": ""29 - 42 वर्ष"",
      ""favorabilityScore"": 88
    }},
    {{
      ""category"": ""Wealth"",
      ""hindiTitle"": ""धन, संपत्ति एवं आर्थिक स्थिरता"",
      ""prediction"": ""हथेली में बन रहा 'धन की कोठरी' (Money Triangle) निरंतर धन संचय का संकेत देता है। अचल संपत्ति के अच्छे योग हैं।"",
      ""auspiciousAgeRange"": ""33 - 50 वर्ष"",
      ""favorabilityScore"": 85
    }},
    {{
      ""category"": ""Health"",
      ""hindiTitle"": ""स्वास्थ्य, आयु एवं जीवनी शक्ति"",
      ""prediction"": ""जीवन रेखा दोषमुक्त होने से उत्तम स्वास्थ्य रहेगा। पाचन और मौसमी तनाव से सतर्क रहने की सलाह दी जाती है।"",
      ""auspiciousAgeRange"": ""दीर्घायु योग"",
      ""favorabilityScore"": 90
    }},
    {{
      ""category"": ""Love & Marriage"",
      ""hindiTitle"": ""प्रेम, वैवाहिक जीवन एवं संबंध"",
      ""prediction"": ""हृदय रेखा पर गुरु का प्रभाव होने से जीवनसाथी समझदार, संस्कारी और सहयोगी प्राप्त होगा।"",
      ""auspiciousAgeRange"": ""25 - 28 वर्ष"",
      ""favorabilityScore"": 84
    }}
  ],
  ""kundliCorrelationAnalysis"": ""हथेली के शुभ सूर्य व गुरु पर्वत का प्रभाव कुंडली के लग्न व दशम भाव के शुभ ग्रहों के साथ पूर्ण सामंजस्य बनाता है।"",
  ""vedicRemedies"": [
    {{
      ""title"": ""सूर्य देव को जल अर्घ्य एवं आदित्य हृदय स्तोत्र"",
      ""description"": ""तांबे के लोटे में रोली और लाल फूल डालकर नित्य सूर्योदय के समय अर्घ्य दें।"",
      ""recommendedGemstone"": ""माणिक्य (Ruby) अथवा 12 मुखी रुद्राक्ष"",
      ""mantra"": ""ॐ घृणि सूर्याय नमः"",
      ""daanDonation"": ""रविवार के दिन गेहूं, गुड़ और तांबे के पात्र का दान करें।""
    }},
    {{
      ""title"": ""गुरुवार को पीले वस्त्र एवं चने की दाल का दान"",
      ""description"": ""बृहस्पति देव की कृपा हेतु गुरुवार को केले के वृक्ष का पूजन करें।"",
      ""recommendedGemstone"": ""पुखराज (Yellow Sapphire) अथवा 5 मुखी रुद्राक्ष"",
      ""mantra"": ""ॐ बृं बृहस्पतये नमः"",
      ""daanDonation"": ""हल्दी, बेसन के लड्डू व धार्मिक पुस्तकों का दान।""
    }}
  ],
  ""keyHighlights"": [
    ""हस्तरेखा में राजयोग कारक भाग्य रेखा मौजूद है"",
    ""सूर्य पर्वत की स्थिति मान-सम्मान और प्रसिद्धि दिलाएगी"",
    ""धन त्रिकोण बनने से आकस्मिक लाभ और संचित धन में वृद्धि होगी"",
    ""32वें वर्ष के उपरांत जीवन में स्थायी स्थिरता का संकेत""
  ]
}}
Only return valid JSON without commentary.";
    }

    private static string BuildHeadPrompt(string kundliContext, string language)
    {
        return $@"Analyze this forehead/head/face photo using classical Vedic Samudrika Shastra (सामुद्रिक शास्त्र - मुख एवं मस्तक लक्षण विज्ञान).
{(string.IsNullOrWhiteSpace(kundliContext) ? "" : "User Kundli Context: " + kundliContext)}
Language required: {(language == "hi" ? "Hindi (हिंदी)" : "English")}.

Examine the forehead curvature, forehead planetary lines (Saturn line, Jupiter line, Mars line, Sun line, Venus line, Mercury line, Moon line), brow structure, and facial Tejas (luster).
Return STRICT JSON matching this schema:
{{
  ""title"": ""सामुद्रिक शास्त्र मस्तक एवं मुखाकृति विश्लेषण"",
  ""handOrFaceType"": ""चौड़ा एवं तेजस्वी मस्तक (Vedic Broad Forehead)"",
  ""overviewSummary"": ""सामुद्रिक शास्त्र के अनुसार मस्तक की बनावट उच्च बौद्धिक स्तर, दार्शनिक सोच और स्वाभिमानी व्यक्तित्व को दर्शाती है।"",
  ""overallAuspiciousnessScore"": 86,
  ""primaryFeatures"": [
    {{
      ""name"": ""बृहस्पति रेखा (Guru Rekha - Forehead Line of Jupiter)"",
      ""sanskritName"": ""गुरु मस्तक रेखा"",
      ""status"": ""स्पष्ट, सीधी एवं निर्दोष"",
      ""significance"": ""धर्म, उच्च ज्ञान, सत्यनिष्ठा और प्रशासनिक दक्षता"",
      ""details"": ""माथे के मध्य भाग में स्पष्ट रेखा व्यक्ति को गंभीर, विचारशील और न्यायप्रिय बनाती है।"",
      ""strengthScore"": 88,
      ""auspiciousSign"": ""शुभ धनुषाकार""
    }},
    {{
      ""name"": ""सूर्य रेखा (Surya Rekha - Forehead Line of Sun)"",
      ""sanskritName"": ""सूर्य मस्तक रेखा"",
      ""status"": ""प्रकाशमान एवं स्पष्ट"",
      ""significance"": ""प्रतिष्ठा, तीव्र स्मरण शक्ति और शासकीय कार्य में लाभ"",
      ""details"": ""सूर्य रेखा का स्पष्ट दर्शन जीवन में मान-सम्मान और आत्मनिर्भरता दिलाता है।"",
      ""strengthScore"": 85,
      ""auspiciousSign"": null
    }},
    {{
      ""name"": ""शनि रेखा (Shani Rekha - Top Forehead Line)"",
      ""sanskritName"": ""शनि मस्तक रेखा"",
      ""status"": ""ऊपरी मस्तक पर संतुलित रेखा"",
      ""significance"": ""धैर्य, अनुभव से सीखना और गहन दार्शनिक दृष्टिकोण"",
      ""details"": ""संघर्ष के पश्चात स्थाई सफलता और दूरदर्शिता प्रदान करती है।"",
      ""strengthScore"": 82,
      ""auspiciousSign"": null
    }}
  ],
  ""secondaryFeatures"": [
    {{
      ""name"": ""भौंहों की बनावट (Eyebrow Architecture)"",
      ""sanskritName"": ""धनुष भ्रू लक्षण"",
      ""status"": ""सुव्यवस्थित एवं जुड़ी नहीं"",
      ""significance"": ""शीघ्र निर्णय क्षमता और स्पष्ट वैचारिक दृष्टि"",
      ""details"": ""सामुद्रिक शास्त्र में ऐसी भौहें निष्कपट और विश्वसनीय स्वभाव का प्रतीक हैं।"",
      ""strengthScore"": 84,
      ""auspiciousSign"": null
    }},
    {{
      ""name"": ""मुख तेज एवं आभा मंडल (Facial Tejas & Aura)"",
      ""sanskritName"": ""ओजस एवं कांति"",
      ""status"": ""सकारात्मक आभा युक्त"",
      ""significance"": ""आत्मविश्वास, आकर्षण और समाज को प्रभावित करने की शक्ति"",
      ""details"": ""चेहरे की सौम्यता और आंखों की चमक मानसिक दृढ़ता दर्शाती है।"",
      ""strengthScore"": 87,
      ""auspiciousSign"": null
    }}
  ],
  ""lifeAspects"": [
    {{
      ""category"": ""Intellect & Wisdom"",
      ""hindiTitle"": ""बुद्धि, स्मरण शक्ति एवं विचार शैली"",
      ""prediction"": ""विशाल मस्तक तीक्ष्ण बुद्धि और दीर्घकालिक योजना बनाने में निपुणता का प्रमाण है। कठिन विषयों को सहजता से समझने की क्षमता है।"",
      ""auspiciousAgeRange"": ""सदा प्रभावी"",
      ""favorabilityScore"": 90
    }},
    {{
      ""category"": ""Leadership & Career"",
      ""hindiTitle"": ""नेतृत्व क्षमता, पद एवं करियर"",
      ""prediction"": ""गुरु व सूर्य रेखाओं की प्रबलता से टीम का मार्गदर्शन करने और नेतृत्व सम्भालने के अवसर मिलेंगे।"",
      ""auspiciousAgeRange"": ""31 वर्ष से विशेष प्रभाव"",
      ""favorabilityScore"": 87
    }},
    {{
      ""category"": ""Health & Peace of Mind"",
      ""hindiTitle"": ""मानसिक शांति एवं स्वास्थ्य"",
      ""prediction"": ""विचारों की अधिकता से कभी-कभी मानसिक थकान संभव है। प्राणायाम व ध्यान से असीम ऊर्जा बनी रहेगी।"",
      ""auspiciousAgeRange"": ""दीर्घायु योग"",
      ""favorabilityScore"": 85
    }}
  ],
  ""kundliCorrelationAnalysis"": ""सामुद्रिक लक्षणों में दिखने वाला सूर्य व गुरु का तेज जातक की जन्म कुंडली के लग्न भाव को अत्यधिक बल प्रदान करता है।"",
  ""vedicRemedies"": [
    {{
      ""title"": ""प्रतिदिन मस्तक पर चंदन या केसर का तिलक"",
      ""description"": ""आज्ञा चक्र (भ्रूमध्य) पर श्वेत चंदन अथवा केसर का तिलक लगाने से मानसिक शांति और एकाग्रता में वृद्धि होती है।"",
      ""recommendedGemstone"": ""पंचमुखी रुद्राक्ष अथवा मोती"",
      ""mantra"": ""ॐ नमो भगवते वासुदेवाय"",
      ""daanDonation"": ""सोमवार या पूर्णिमा को दूध, चावल अथवा श्वेत वस्तुओं का दान।""
    }},
    {{
      ""title"": ""प्राणायाम एवं गायत्री मंत्र जप"",
      ""description"": ""प्रातःकाल 10 मिनट भ्रामरी प्राणायाम और गायत्री मंत्र का 11 बार जप चेहरे के तेज और मस्तक को ऊर्जावान बनाता है।"",
      ""recommendedGemstone"": ""रुद्राक्ष माला"",
      ""mantra"": ""ॐ भूर्भुवः स्वः तत्सवितुर्वरेण्यं भर्गो देवस्य धीमहि धियो यो नः प्रचोदयात्"",
      ""daanDonation"": ""पक्षियों को दाना और गाय को हरा चारा खिलाएं।""
    }}
  ],
  ""keyHighlights"": [
    ""सामुद्रिक शास्त्र अनुसार मस्तक पर शुभ राजयोग के लक्षण हैं"",
    ""गुरु व सूर्य रेखाओं का स्पष्ट प्रभाव समाज में उच्च पद दिलाता है"",
    ""वाणी और निर्णय क्षमता में प्रखरता रहेगी"",
    ""मस्तक पर तिलक धारण करने से भाग्य में विशेष वृद्धि होगी""
  ]
}}
Only return valid JSON without commentary.";
    }

    private static AstroScanReportDto FallbackPalmReport(string language, string? rawText = null)
    {
        return new AstroScanReportDto
        {
            ScanType = "PALM",
            Title = language == "hi" ? "हस्तरेखा वैदिक विश्लेषण" : "Vedic Palm Analysis",
            HandOrFaceType = language == "hi" ? "पृथ्वी तत्व हथेली" : "Earth Hand",
            OverviewSummary = rawText ?? (language == "hi" 
                ? "आपकी हथेली में जीवन रेखा, मस्तिष्क रेखा और हृदय रेखा सुस्पष्ट हैं। गुरु और सूर्य पर्वत पर सकारात्मक प्रभाव आपके मान-सम्मान और करियर में सफलता का संकेत देते हैं।"
                : "Your palm exhibits clear Life, Head, and Heart lines. Favorable influences on Jupiter and Sun mounts indicate strong career prospects and respect."),
            OverallAuspiciousnessScore = 85
        };
    }

    private static AstroScanReportDto FallbackHeadReport(string language, string? rawText = null)
    {
        return new AstroScanReportDto
        {
            ScanType = "HEAD",
            Title = language == "hi" ? "सामुद्रिक मस्तक लक्षण विश्लेषण" : "Samudrika Shastra Forehead Reading",
            HandOrFaceType = language == "hi" ? "चौड़ा एवं ओजस्वी मस्तक" : "Broad Forehead",
            OverviewSummary = rawText ?? (language == "hi"
                ? "मस्तक की बनावट और ललाट रेखाएं तीव्र बुद्धि, विवेकशीलता और स्वाभिमानी स्वभाव को दर्शाती हैं। गुरु और सूर्य रेखाओं का शुभ प्रभाव नेतृत्व क्षमता प्रदान करता है।"
                : "Forehead contours and facial features reveal deep intellect, wisdom, and strong leadership traits with auspicious planetary alignments."),
            OverallAuspiciousnessScore = 85
        };
    }
}
