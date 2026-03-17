using System.Net.Http.Headers;

namespace adwportal.Services;

public class MemberAdminService
{
    private readonly IHttpClientFactory _factory;
    public MemberAdminService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Create(string token)
    {
        var http = _factory.CreateClient("MdwApiBaseUrl");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token?.Trim().Trim('"').Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));
        return http;
    }

    // ── Members ──
    public async Task<List<MemberSummaryVm>> SearchMembersAsync(string token, string? keyword = null, int page = 1, CancellationToken ct = default)
    {
        using var http = Create(token);
        var url = $"api/admin/member/search?page={page}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await http.GetFromJsonAsync<List<MemberSummaryVm>>(url, ct) ?? new();
    }

    public async Task<MemberDetailVm?> GetMemberAsync(string token, long memberId, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<MemberDetailVm>($"api/admin/member/{memberId}", ct);
    }

    // ── Mapping Requests ──
    public async Task<List<MappingRequestVm>> GetPendingRequestsAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<MappingRequestVm>>("api/admin/member/mapping/pending", ct) ?? new();
    }

    public async Task ApproveMappingAsync(string token, long requestId, string? note = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync($"api/admin/member/mapping/request/{requestId}/approve",
            new { ReviewNote = note }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task RejectMappingAsync(string token, long requestId, string? note = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync($"api/admin/member/mapping/request/{requestId}/reject",
            new { ReviewNote = note }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<BulkActionResult>> BulkApproveMappingAsync(string token, List<long> requestIds, string? note = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/member/mapping/bulk-approve",
            new { RequestIds = requestIds, ReviewNote = note }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<BulkActionResult>>(ct) ?? new();
    }

    public async Task<List<BulkActionResult>> BulkRejectMappingAsync(string token, List<long> requestIds, string? note = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/member/mapping/bulk-reject",
            new { RequestIds = requestIds, ReviewNote = note }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<BulkActionResult>>(ct) ?? new();
    }

    // ── Points ──
    public async Task AdjustPointsAsync(string token, long memberId, string adjustType, int points, string reason, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/member/points/adjust", new
        {
            MemberId = memberId,
            AdjustType = adjustType,
            Points = points,
            Reason = reason
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<PointHistoryVm>> GetPointHistoryAsync(string token, long memberId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<PointHistoryVm>>($"api/admin/member/{memberId}/points/history?page={page}&pageSize={pageSize}", ct) ?? new();
    }

    // ── Platform Direct Link ──
    public async Task DirectPlatformLinkAsync(string token, long memberId, string platformType, string accountKey, string? accountName = null, long? shopId = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync($"api/admin/member/{memberId}/platform-link", new
        {
            PlatformType = platformType,
            PlatformAccountKey = accountKey,
            PlatformAccountName = accountName,
            ShopId = shopId
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task RemovePlatformLinkAsync(string token, long memberId, long platformAccountId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/admin/member/{memberId}/platform-link/{platformAccountId}", ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Rewards ──
    public async Task<List<RewardVm>> GetRewardsAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<RewardVm>>("api/admin/rewards", ct) ?? new();
    }

    public async Task CreateRewardAsync(string token, RewardCreateVm dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/rewards", dto, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task ToggleRewardActiveAsync(string token, int rewardId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PatchAsync($"api/admin/rewards/{rewardId}/toggle-active", null, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<RewardCodeVm>> GetRewardCodesAsync(string token, int rewardId, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<RewardCodeVm>>($"api/admin/rewards/{rewardId}/codes", ct) ?? new();
    }

    public async Task AddRewardCodesAsync(string token, int rewardId, List<string> codes, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync($"api/admin/rewards/{rewardId}/codes", codes, ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Redemptions ──
    public async Task<List<RedemptionVm>> GetRedemptionsAsync(string token, string? status = null, int page = 1, CancellationToken ct = default)
    {
        using var http = Create(token);
        var url = $"api/admin/member/redemptions?page={page}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
        return await http.GetFromJsonAsync<List<RedemptionVm>>(url, ct) ?? new();
    }

    public async Task CancelRedemptionAsync(string token, long redemptionId, string? reason = null, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync($"api/admin/member/redemptions/{redemptionId}/cancel",
            new { Reason = reason }, ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Point Policies ──
    public async Task<List<PointPolicyVm>> GetPoliciesAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<PointPolicyVm>>("api/admin/point-policies", ct) ?? new();
    }

    public async Task CreatePolicyAsync(string token, PointPolicyVm policy, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/point-policies", new
        {
            policy.PolicyName,
            policy.PlatformType,
            policy.EarnFormula,
            policy.EarnRate,
            policy.MinOrderAmount,
            policy.EligibleStatuses,
            policy.EffectiveFrom,
            policy.EffectiveTo
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdatePolicyAsync(string token, int policyId, PointPolicyVm policy, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/admin/point-policies/{policyId}", new
        {
            policy.PolicyName,
            policy.PlatformType,
            policy.EarnFormula,
            policy.EarnRate,
            policy.MinOrderAmount,
            policy.EligibleStatuses,
            policy.EffectiveFrom,
            policy.EffectiveTo
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task TogglePolicyAsync(string token, int policyId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PatchAsync($"api/admin/point-policies/{policyId}/toggle", null, ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Earn Formulas ──
    public async Task<List<EarnFormulaVm>> GetFormulasAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<EarnFormulaVm>>("api/admin/earn-formulas", ct) ?? new();
    }

    public async Task CreateFormulaAsync(string token, EarnFormulaVm formula, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/earn-formulas", new
        {
            formula.FormulaCode,
            formula.FormulaName,
            formula.Expression,
            formula.Description,
            formula.Variables
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdateFormulaAsync(string token, int formulaId, EarnFormulaVm formula, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/admin/earn-formulas/{formulaId}", new
        {
            formula.FormulaCode,
            formula.FormulaName,
            formula.Expression,
            formula.Description,
            formula.Variables
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteFormulaAsync(string token, int formulaId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/admin/earn-formulas/{formulaId}", ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── LINE OA Configs ──
    public async Task<List<LineOaConfigVm>> GetLineOaConfigsAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<List<LineOaConfigVm>>("api/admin/line-oa-configs", ct) ?? new();
    }

    public async Task<LineOaConfigVm?> GetLineOaConfigAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        return await http.GetFromJsonAsync<LineOaConfigVm?>($"api/admin/line-oa-configs/{id}", ct);
    }

    public async Task CreateLineOaConfigAsync(string token, LineOaConfigVm config, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsJsonAsync("api/admin/line-oa-configs", new
        {
            config.CompanysId, config.LineOaName, config.LoginChannelId,
            config.LoginChannelSecret, config.LoginCallbackUrl,
            config.MsgChannelSecret, config.MsgChannelToken, config.LiffId, config.IsActive
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdateLineOaConfigAsync(string token, int id, LineOaConfigVm config, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PutAsJsonAsync($"api/admin/line-oa-configs/{id}", new
        {
            config.CompanysId, config.LineOaName, config.LoginChannelId,
            config.LoginChannelSecret, config.LoginCallbackUrl,
            config.MsgChannelSecret, config.MsgChannelToken, config.LiffId, config.IsActive
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteLineOaConfigAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.DeleteAsync($"api/admin/line-oa-configs/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task ToggleLineOaConfigAsync(string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PatchAsync($"api/admin/line-oa-configs/{id}/toggle", null, ct);
        resp.EnsureSuccessStatusCode();
    }

    // ── Member Summary ──
    public async Task<List<MemberSummaryStatsVm>> GetMemberSummaryAsync(string token, string? keyword = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        using var http = Create(token);
        var url = $"api/admin/member/summary?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await http.GetFromJsonAsync<List<MemberSummaryStatsVm>>(url, ct) ?? new();
    }

    public async Task<EarnResultVm> TriggerEarnAsync(string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        var resp = await http.PostAsync("api/admin/member/trigger-earn", null, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<EarnResultVm>(ct) ?? new();
    }
}

// ── View Models ──
public class MemberSummaryVm
{
    public long MemberId { get; set; }
    public string MemberCode { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Status { get; set; } = "";
    public int PlatformAccountCount { get; set; }
    public int AvailablePoints { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class MemberDetailVm
{
    public long MemberId { get; set; }
    public string MemberCode { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
    public List<MemberIdentityVm> Identities { get; set; } = new();
    public List<PlatformAccountVm> PlatformAccounts { get; set; } = new();
    public PointBalanceVm? PointBalance { get; set; }
}

public class MemberIdentityVm
{
    public string ProviderType { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? PictureUrl { get; set; }
    public bool IsActive { get; set; }
}

public class PlatformAccountVm
{
    public long MemberPlatformAccountId { get; set; }
    public string PlatformType { get; set; } = "";
    public string PlatformAccountKey { get; set; } = "";
    public string? PlatformAccountName { get; set; }
    public string VerifiedStatus { get; set; } = "";
}

public class PointBalanceVm
{
    public int AvailablePoints { get; set; }
    public int ReservedPoints { get; set; }
    public int TotalEarned { get; set; }
    public int TotalBurned { get; set; }
    public int TotalExpired { get; set; }
}

public class PointHistoryVm
{
    public long LedgerId { get; set; }
    public string TxnType { get; set; } = "";
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string? RefType { get; set; }
    public string? RefId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class MappingRequestVm
{
    public long RequestId { get; set; }
    public long MemberId { get; set; }
    public string? MemberDisplayName { get; set; }
    public string PlatformType { get; set; } = "";
    public string PlatformAccountKey { get; set; } = "";
    public string? PlatformAccountName { get; set; }
    public string SourceType { get; set; } = "";
    public string RequestStatus { get; set; } = "";
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RewardVm
{
    public int RewardId { get; set; }
    public string RewardName { get; set; } = "";
    public string? Description { get; set; }
    public string RewardType { get; set; } = "";
    public int PointsCost { get; set; }
    public int StockRemaining { get; set; }
    public bool IsActive { get; set; }
}

public class RewardCreateVm
{
    public string RewardName { get; set; } = "";
    public string? Description { get; set; }
    public string RewardType { get; set; } = "GIFT";
    public int PointsCost { get; set; }
    public int StockTotal { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class RedemptionVm
{
    public long RedemptionId { get; set; }
    public long MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public int RewardId { get; set; }
    public string RewardName { get; set; } = "";
    public int PointsSpent { get; set; }
    public string? Code { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReservedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class BulkActionResult
{
    public long RequestId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class RewardCodeVm
{
    public long RewardCodeId { get; set; }
    public string Code { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? IssuedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public long? RedemptionId { get; set; }
}

public class PointPolicyVm
{
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = "";
    public string PlatformType { get; set; } = "ALL";
    public string EarnFormula { get; set; } = "AMOUNT_DIV_100";
    public decimal EarnRate { get; set; } = 1.0m;
    public decimal? MinOrderAmount { get; set; }
    public string? EligibleStatuses { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Now;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MemberSummaryStatsVm
{
    public long MemberId { get; set; }
    public string MemberCode { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
    public int PlatformAccountCount { get; set; }
    public string? PlatformTypes { get; set; }
    public int AvailablePoints { get; set; }
    public int TotalEarned { get; set; }
    public int TotalBurned { get; set; }
    public int ReservedPoints { get; set; }
    public int LinkedOrderCount { get; set; }
}

public class EarnResultVm
{
    public int Linked { get; set; }
    public int Earned { get; set; }
    public string? Message { get; set; }
}

public class EarnFormulaVm
{
    public int FormulaId { get; set; }
    public string FormulaCode { get; set; } = "";
    public string FormulaName { get; set; } = "";
    public string Expression { get; set; } = "";
    public string? Description { get; set; }
    public string? Variables { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LineOaConfigVm
{
    public int LineOaConfigId { get; set; }
    public int CompanysId { get; set; }
    public string LineOaName { get; set; } = "";
    public string? LoginChannelId { get; set; }
    public string? LoginChannelSecret { get; set; }
    public string? LoginCallbackUrl { get; set; }
    public string? MsgChannelSecret { get; set; }
    public string MsgChannelToken { get; set; } = "";
    public string? LiffId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
