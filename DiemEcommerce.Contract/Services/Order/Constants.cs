namespace DiemEcommerce.Contract.Services.Order;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Shipping = 3,
    Delivered = 4,
    Cancelled = 5
}

public static class OrderStatusExtensions
{
    public static string ToDisplayString(this OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Confirmed => "Confirmed",
            OrderStatus.Preparing => "Preparing",
            OrderStatus.Shipping => "Shipping",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }
}