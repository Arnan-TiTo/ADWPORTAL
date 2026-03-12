using System.Net.Http.Headers;
using System.Net.Http.Json;
using adwportal.Dtos;

namespace adwportal.Services;

public class ShopGroupService
{
    private readonly IHttpClientFactory _factory;
    public ShopGroupService(IHttpClientFactory factory) => _factory = factory;

    private static string NormalizeToken(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return "";
        t = t.Trim().Trim('"');
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) t = t[7..].Trim();
        return t;
    }

    private HttpClient Create(string token)
    {
        var http = _factory.CreateClient("IdwApiBaseUrl");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", NormalizeToken(token));
        return http;
    }

    // ============================================================
    //  IDW Users (สำหรับ dropdown ใน ShopGroupEdit)
    // ============================================================

    public record IdwUserDto(int Id, string Username, string Role, bool IsActive);

    /// <summary>
    /// ดึง users ทั้งหมดจาก IDW API (GET /api/users) — Admin only
    /// </summary>
    public async Task<List<IdwUserDto>> GetIdwUsersAsync(
        string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync("api/users", ct);
        if (!res.IsSuccessStatusCode) return new();
        return await res.Content.ReadFromJsonAsync<List<IdwUserDto>>(ct) ?? new();
    }

    // ============================================================
    //  ★ USER-CENTRIC (ใช้ใน pages ที่มี shop dropdown)
    // ============================================================

    /// <summary>
    /// ดึง shops ทั้งหมดที่ user สามารถเข้าถึงได้ (aggregate จากทุก active group)
    /// ใช้ userId ของ IDW
    /// </summary>
    public async Task<List<UserEffectiveShopDtos>> GetUserEffectiveShopsAsync(
        string token, int userId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync($"api/shopgroups/user/{userId}/shops", ct);
        if (!res.IsSuccessStatusCode) return new();
        return await res.Content.ReadFromJsonAsync<List<UserEffectiveShopDtos>>(ct) ?? new();
    }

    /// <summary>
    /// ดึง shops ทั้งหมดที่ user สามารถเข้าถึงได้ — ค้นจาก username
    /// ใช้เมื่อ caller มี username (จาก miniApp login) แต่ไม่มี IDW userId
    /// </summary>
    public async Task<List<UserEffectiveShopDtos>> GetUserEffectiveShopsByUsernameAsync(
        string token, string username, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync($"api/shopgroups/user/by-username/{Uri.EscapeDataString(username)}/shops", ct);
        if (!res.IsSuccessStatusCode) return new();
        return await res.Content.ReadFromJsonAsync<List<UserEffectiveShopDtos>>(ct) ?? new();
    }

    /// <summary>
    /// ดึง groups ที่ user เป็นสมาชิก (แสดงใน UserEdit)
    /// </summary>
    public async Task<List<UserGroupInfoDtos>> GetUserGroupsAsync(
        string token, int userId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync($"api/shopgroups/user/{userId}/groups", ct);
        if (!res.IsSuccessStatusCode) return new();
        return await res.Content.ReadFromJsonAsync<List<UserGroupInfoDtos>>(ct) ?? new();
    }

    // ============================================================
    //  GROUP CRUD (Admin pages)
    // ============================================================

    public async Task<List<ShopGroupDtos>> GetGroupsAsync(
        string token, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync("api/shopgroups", ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"GetGroups failed {(int)res.StatusCode}: {body}");
        }
        return await res.Content.ReadFromJsonAsync<List<ShopGroupDtos>>(ct) ?? new();
    }

    public async Task<ShopGroupDetailDtos?> GetGroupDetailAsync(
        string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.GetAsync($"api/shopgroups/{id}", ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ShopGroupDetailDtos>(ct);
    }

    public async Task<ShopGroupDtos?> CreateGroupAsync(
        string token, ShopGroupUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PostAsJsonAsync("api/shopgroups", dto, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"CreateGroup failed {(int)res.StatusCode}: {body}");
        }
        return await res.Content.ReadFromJsonAsync<ShopGroupDtos>(ct);
    }

    public async Task UpdateGroupAsync(
        string token, int id, ShopGroupUpsertDtos dto, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PutAsJsonAsync($"api/shopgroups/{id}", dto, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"UpdateGroup failed {(int)res.StatusCode}: {body}");
        }
    }

    public async Task DeleteGroupAsync(
        string token, int id, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.DeleteAsync($"api/shopgroups/{id}", ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"DeleteGroup failed {(int)res.StatusCode}: {body}");
        }
    }

    // ============================================================
    //  GROUP USERS
    // ============================================================

    public async Task AddUserToGroupAsync(
        string token, int groupId, int userId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PostAsJsonAsync($"api/shopgroups/{groupId}/users", new { UserId = userId }, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"AddUserToGroup failed {(int)res.StatusCode}: {body}");
        }
    }

    public async Task RemoveUserFromGroupAsync(
        string token, int groupId, int userId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.DeleteAsync($"api/shopgroups/{groupId}/users/{userId}", ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"RemoveUser failed {(int)res.StatusCode}: {body}");
        }
    }

    public async Task ReplaceGroupUsersAsync(
        string token, int groupId, int[] userIds, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PutAsJsonAsync($"api/shopgroups/{groupId}/users", new { UserIds = userIds }, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"ReplaceGroupUsers failed {(int)res.StatusCode}: {body}");
        }
    }

    // ============================================================
    //  GROUP SHOPS
    // ============================================================

    public async Task AddShopToGroupAsync(
        string token, int groupId, long shopId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PostAsJsonAsync($"api/shopgroups/{groupId}/shops", new { ShopId = shopId }, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"AddShopToGroup failed {(int)res.StatusCode}: {body}");
        }
    }

    public async Task RemoveShopFromGroupAsync(
        string token, int groupId, long shopId, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.DeleteAsync($"api/shopgroups/{groupId}/shops/{shopId}", ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"RemoveShop failed {(int)res.StatusCode}: {body}");
        }
    }

    public async Task ReplaceGroupShopsAsync(
        string token, int groupId, long[] shopIds, CancellationToken ct = default)
    {
        using var http = Create(token);
        var res = await http.PutAsJsonAsync($"api/shopgroups/{groupId}/shops", new { ShopIds = shopIds }, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"ReplaceGroupShops failed {(int)res.StatusCode}: {body}");
        }
    }
}
