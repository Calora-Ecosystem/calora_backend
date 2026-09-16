namespace Core.Services.Dashboard.Contracts;

/// <summary>
/// AI (food_recognition) xizmatidan foydalanish ko'rsatkichlari:
/// so'rovlar, foydalanuvchilar, min/max miqdorlar, tokenlar sarfi, xarajatlar va premium konversiyasi.
/// </summary>
public record GetAiStatisticsDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Days { get; set; }

    /// <summary>Tanlangan davrda AI ga murojaat qilgan unikal foydalanuvchilar soni</summary>
    public int TotalAiUsers { get; set; }

    /// <summary>AI ga yuborilgan jami so'rovlar soni</summary>
    public int TotalRequests { get; set; }

    /// <summary>Muvaffaqiyatli yakunlangan so'rovlar soni</summary>
    public int SuccessRequests { get; set; }

    /// <summary>Xatolik bilan tugagan so'rovlar soni</summary>
    public int ErrorRequests { get; set; }

    /// <summary>Foydalanuvchi boshiga o'rtacha so'rovlar soni (TotalRequests / TotalAiUsers)</summary>
    public double AvgRequestsPerUser { get; set; }

    /// <summary>Kuniga o'rtacha so'rovlar soni (TotalRequests / Days)</summary>
    public double AvgRequestsPerDay { get; set; }

    /// <summary>Bir foydalanuvchi yuborgan minimal so'rovlar soni</summary>
    public int MinRequestsPerUser { get; set; }

    /// <summary>Bir foydalanuvchi yuborgan maksimal so'rovlar soni</summary>
    public int MaxRequestsPerUser { get; set; }

    /// <summary>Bir kunda yuborilgan eng kam so'rovlar soni</summary>
    public int MinRequestsPerDay { get; set; }

    /// <summary>Bir kunda yuborilgan eng ko'p so'rovlar soni</summary>
    public int MaxRequestsPerDay { get; set; }

    /// <summary>O'rtacha so'rov bajarilish vaqti (ms)</summary>
    public double? AvgDurationMs { get; set; }

    /// <summary>Jami prompt (kiruvchi rasm + prompt) tokenlari soni</summary>
    public long TotalPromptTokens { get; set; }

    /// <summary>Jami candidate (javob) tokenlari soni</summary>
    public long TotalCandidateTokens { get; set; }

    /// <summary>Jami sarflangan tokenlar (Prompt + Candidate)</summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Jami sarflangan mablag' (USD).
    /// Gemini 2.5 Flash tariflari asosida:
    /// Prompt: $0.30 / 1M token ($0.00000030/token)
    /// Candidate: $2.50 / 1M token ($0.00000250/token)
    /// </summary>
    public double TotalCostUsd { get; set; }

    /// <summary>Bir AI foydalanuvchisiga to'g'ri keladigan o'rtacha xarajat (USD)</summary>
    public double CostPerUserUsd { get; set; }

    /// <summary>Bitta so'rovga to'g'ri keladigan o'rtacha xarajat (USD)</summary>
    public double CostPerRequestUsd { get; set; }

    /// <summary>Jami premium foydalanuvchilar soni</summary>
    public int TotalPremiumUsers { get; set; }

    /// <summary>AI dan foydalangan premium foydalanuvchilar soni</summary>
    public int ActiveAiPremiumUsers { get; set; }

    /// <summary>Premium foydalanuvchilar orasida AI dan foydalanish foizi</summary>
    public double AiAdoptionRatePercent { get; set; }

    /// <summary>Kunlik dinamika va taqsimot</summary>
    public List<DailyAiStatDto> DailyTrend { get; set; } = [];

    /// <summary>Eng ko'p so'rov yuborgan foydalanuvchilar (Top 10)</summary>
    public List<TopAiUserDto> TopUsers { get; set; } = [];
}

public record DailyAiStatDto
{
    public DateTime Date { get; set; }
    public int Requests { get; set; }
    public int Users { get; set; }
    public double CostUsd { get; set; }
    public long PromptTokens { get; set; }
    public long CandidateTokens { get; set; }
}

public record TopAiUserDto
{
    public long UserId { get; set; }
    public int RequestCount { get; set; }
    public double CostUsd { get; set; }
}
