using System.ComponentModel;
using System.Runtime.CompilerServices;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public sealed class PoolCouponViewModel(
    IPoolCouponRepository couponRepository,
    IStryktipsSystemFactory systemFactory) : INotifyPropertyChanged
{
    private IReadOnlyList<PoolCoupon> _coupons = [];
    private PoolCouponType _selectedType = PoolCouponType.Stryktips;
    private CouponSystemTier _selectedTier = CouponSystemTier.Kr32;
    private CouponSystem? _system;
    private string _statusMessage = "Loading demo coupons…";
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<PoolCoupon> Coupons
    {
        get => _coupons;
        private set => SetField(ref _coupons, value);
    }

    public PoolCouponType SelectedType
    {
        get => _selectedType;
        private set => SetField(ref _selectedType, value);
    }

    public CouponSystemTier SelectedTier
    {
        get => _selectedTier;
        private set => SetField(ref _selectedTier, value);
    }

    public PoolCoupon? CurrentCoupon => Coupons.FirstOrDefault(coupon => coupon.Type == SelectedType);

    public CouponSystem? System
    {
        get => _system;
        private set => SetField(ref _system, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (Coupons.Count > 0)
            return;

        IsLoading = true;
        try
        {
            var result = await couponRepository.GetCouponsAsync(cancellationToken);
            Coupons = result.Data ?? [];
            StatusMessage = result.Message ?? "Pool coupon data unavailable.";
            RebuildSystem();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Coupons = [];
            System = null;
            StatusMessage = $"Pool coupon data unavailable: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CurrentCoupon));
        }
    }

    public void SelectCoupon(PoolCouponType type)
    {
        if (SelectedType == type)
            return;

        SelectedType = type;
        RebuildSystem();
        OnPropertyChanged(nameof(CurrentCoupon));
    }

    public void SelectTier(CouponSystemTier tier)
    {
        if (SelectedTier == tier)
            return;

        SelectedTier = tier;
        RebuildSystem();
    }

    private void RebuildSystem()
    {
        var coupon = CurrentCoupon;
        System = coupon is null ? null : systemFactory.Create(coupon, SelectedTier);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
