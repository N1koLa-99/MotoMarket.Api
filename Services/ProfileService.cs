using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IBlobImageService _blobImageService;

    public ProfileService(IProfileRepository profileRepository, IBlobImageService blobImageService)
    {
        _profileRepository = profileRepository;
        _blobImageService = blobImageService;
    }

    public async Task<ProfileDashboardResponse> GetDashboardAsync(long userId)
    {
        var user = await GetActiveUser(userId);

        var activeListingsCount = await _profileRepository.GetActiveListingsCountAsync(userId);
        var favoritesCount = await _profileRepository.GetFavoritesCountAsync(userId);
        var paidListingActionsCount = await _profileRepository.GetPaidListingActionsCountAsync(userId);
        var totalPaymentsCount = await _profileRepository.GetPaymentsCountAsync(userId);
        var totalPaidAmount = await _profileRepository.GetTotalPaidAmountAsync(userId);

        var now = DateTime.UtcNow;

        var dashboard = new ProfileDashboardResponse
        {
            UserId = user.Id,
            RoleName = user.RoleName,
            AccountType = user.AccountType,
            FullName = user.AccountType == "PRIVATE"
                ? $"{user.FirstName} {user.LastName}".Trim()
                : null,
            CompanyName = user.CompanyName,
            Email = user.Email,
            Phone = user.Phone,

            PublishedListingsTotalCount = user.PublishedListingsTotalCount,
            ActiveListingsCount = activeListingsCount,
            FavoritesCount = favoritesCount,

            PaidListingActionsCount = paidListingActionsCount,
            TotalPaymentsCount = totalPaymentsCount,
            TotalPaidAmountEUR = totalPaidAmount,

            PrivateFreeLimitLifetime = 3,
            PrivateFreeUsedLifetime = user.PrivateFreeUsedCount,

            CompanyStarterFreeLimitLifetime = 30,
            CompanyStarterFreeUsedLifetime = user.CompanyStarterFreeUsedCount,

            CompanyMonthlyFreeLimit = 10,
            CompanyMonthlyFreeUsedCurrentMonth = 0,
            CompanyMonthlyFreeRemainingCurrentMonth = 0,

            FreeUploadsRemainingNow = 0,
            OverFreeLimitCount = 0,
            IsInMonthlyCompanyQuotaMode = false
        };

        if (user.AccountType == "PRIVATE")
        {
            dashboard.FreeUploadsRemainingNow = Math.Max(0, 3 - user.PrivateFreeUsedCount);
            dashboard.OverFreeLimitCount = Math.Max(0, user.PublishedListingsTotalCount - 3);
            return dashboard;
        }

        if (user.CompanyStarterFreeUsedCount < 30)
        {
            dashboard.FreeUploadsRemainingNow = 30 - user.CompanyStarterFreeUsedCount;
            dashboard.CompanyMonthlyFreeUsedCurrentMonth = 0;
            dashboard.CompanyMonthlyFreeRemainingCurrentMonth = 10;
            dashboard.IsInMonthlyCompanyQuotaMode = false;
            dashboard.OverFreeLimitCount = 0;
            return dashboard;
        }

        var isCurrentMonth = user.CompanyMonthlyQuotaYear == now.Year
                             && user.CompanyMonthlyQuotaMonth == now.Month;

        var monthlyUsed = isCurrentMonth ? user.CompanyMonthlyFreeUsedCount : 0;
        var monthlyRemaining = Math.Max(0, 10 - monthlyUsed);

        dashboard.CompanyMonthlyFreeUsedCurrentMonth = monthlyUsed;
        dashboard.CompanyMonthlyFreeRemainingCurrentMonth = monthlyRemaining;
        dashboard.FreeUploadsRemainingNow = monthlyRemaining;
        dashboard.IsInMonthlyCompanyQuotaMode = true;

        // Тук показваме колко платени listing action-а е имал след free лимитите.
        dashboard.OverFreeLimitCount = paidListingActionsCount;

        return dashboard;
    }

    public async Task<PagedResultResponse<ProfileListingCardResponse>> GetMyListingsAsync(long userId, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);
        await GetActiveUser(userId);

        var totalCount = await _profileRepository.CountOwnListingsAsync(userId);
        var items = await _profileRepository.GetOwnListingsAsync(userId, page, pageSize);
        ResolveBlobUrls(items);

        return BuildPagedResult(items, page, pageSize, totalCount);
    }

    public async Task<PagedResultResponse<ProfileListingCardResponse>> GetMyFavoritesAsync(long userId, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);
        await GetActiveUser(userId);

        var totalCount = await _profileRepository.CountFavoriteListingsAsync(userId);
        var items = await _profileRepository.GetFavoriteListingsAsync(userId, page, pageSize);
        ResolveBlobUrls(items);

        return BuildPagedResult(items, page, pageSize, totalCount);
    }

    public async Task<PagedResultResponse<PaymentHistoryItemResponse>> GetMyPaymentsAsync(long userId, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);
        await GetActiveUser(userId);

        var totalCount = await _profileRepository.CountPaymentsAsync(userId);
        var items = await _profileRepository.GetPaymentsAsync(userId, page, pageSize);

        return BuildPagedResult(items, page, pageSize, totalCount);
    }

    public async Task AddFavoriteAsync(long userId, long listingId)
    {
        await GetActiveUser(userId);

        var exists = await _profileRepository.ListingExistsAsync(listingId);
        if (!exists)
            throw new InvalidOperationException("Обявата не е намерена.");

        await _profileRepository.AddFavoriteAsync(userId, listingId);
    }

    public async Task RemoveFavoriteAsync(long userId, long listingId)
    {
        await GetActiveUser(userId);
        await _profileRepository.RemoveFavoriteAsync(userId, listingId);
    }

    public async Task<bool> IsFavoriteAsync(long userId, long listingId)
    {
        await GetActiveUser(userId);
        return await _profileRepository.IsFavoriteAsync(userId, listingId);
    }

    private async Task<User> GetActiveUser(long userId)
    {
        return await _profileRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");
    }

    private void ResolveBlobUrls(List<ProfileListingCardResponse> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.MainPhotoBlobName))
            {
                item.MainPhotoUrl = _blobImageService.GetReadUrl(item.MainPhotoBlobName);
            }
        }
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0)
            throw new InvalidOperationException("Невалидна страница.");

        if (pageSize <= 0 || pageSize > 100)
            throw new InvalidOperationException("Невалиден pageSize.");
    }

    private static PagedResultResponse<T> BuildPagedResult<T>(List<T> items, int page, int pageSize, int totalCount)
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
