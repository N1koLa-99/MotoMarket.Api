using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly IBlobImageService _blobImageService;

    public AdminService(IAdminRepository adminRepository, IBlobImageService blobImageService)
    {
        _adminRepository = adminRepository;
        _blobImageService = blobImageService;
    }

    public async Task<PagedResultResponse<AdminUserListItemResponse>> GetUsersAsync(string? searchTerm, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        var totalCount = await _adminRepository.CountUsersAsync(searchTerm);
        var items = await _adminRepository.GetUsersAsync(searchTerm, page, pageSize);

        return BuildPaged(items, page, pageSize, totalCount);
    }

    public async Task<AdminUserDetailsResponse> GetUserDetailsAsync(long userId)
    {
        var user = await _adminRepository.GetUserDetailsAsync(userId)
            ?? throw new InvalidOperationException("Потребителят не е намерен.");

        if (user.AccountType == "PRIVATE")
        {
            user.FreeUploadsRemainingNow = Math.Max(0, 3 - user.PrivateFreeUsedCount);
            user.OverFreeLimitCount = Math.Max(0, user.PublishedListingsTotalCount - 3);
            return user;
        }

        if (user.CompanyStarterFreeUsedCount < 30)
        {
            user.FreeUploadsRemainingNow = 30 - user.CompanyStarterFreeUsedCount;
            user.OverFreeLimitCount = 0;
            return user;
        }

        var now = DateTime.UtcNow;
        var isCurrentMonth = user.CompanyMonthlyQuotaYear == now.Year
                             && user.CompanyMonthlyQuotaMonth == now.Month;

        var monthlyUsed = isCurrentMonth ? user.CompanyMonthlyFreeUsedCount : 0;
        user.FreeUploadsRemainingNow = Math.Max(0, 10 - monthlyUsed);
        user.OverFreeLimitCount = Math.Max(0, monthlyUsed - 10);

        return user;
    }

    public async Task<PagedResultResponse<ProfileListingCardResponse>> GetUserListingsAsync(long userId, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        var exists = await _adminRepository.GetUserByIdAsync(userId);
        if (exists == null)
            throw new InvalidOperationException("Потребителят не е намерен.");

        var totalCount = await _adminRepository.CountUserListingsAsync(userId);
        var items = await _adminRepository.GetUserListingsAsync(userId, page, pageSize);

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.MainPhotoBlobName))
            {
                item.MainPhotoUrl = _blobImageService.GetReadUrl(item.MainPhotoBlobName);
            }
        }

        return BuildPaged(items, page, pageSize, totalCount);
    }

    public async Task<PagedResultResponse<PaymentHistoryItemResponse>> GetPaymentsAsync(string? searchTerm, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        var totalCount = await _adminRepository.CountPaymentsAsync(searchTerm);
        var items = await _adminRepository.GetPaymentsAsync(searchTerm, page, pageSize);

        return BuildPaged(items, page, pageSize, totalCount);
    }

    public async Task<PagedResultResponse<AdminPendingActionResponse>> GetPendingActionsAsync(string? status, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        var totalCount = await _adminRepository.CountPendingActionsAsync(status);
        var items = await _adminRepository.GetPendingActionsAsync(status, page, pageSize);

        return BuildPaged(items, page, pageSize, totalCount);
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0)
            throw new InvalidOperationException("Невалидна страница.");

        if (pageSize <= 0 || pageSize > 100)
            throw new InvalidOperationException("Невалиден pageSize.");
    }

    private static PagedResultResponse<T> BuildPaged<T>(List<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultResponse<T>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        };
    }
}
