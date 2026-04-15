namespace API.Application.Dtos.Admin;

public class BookingManagementResponse
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string OutboundFlight { get; set; } = null!;

    public string? ReturnFlight { get; set; }

    public int PassengerCount { get; set; }

    public decimal Amount { get; set; }

    public int BookingStatus { get; set; } // 0=Pending, 1=Confirmed, 2=CheckedIn, 3=Cancelled

    public string BookingStatusName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class CancelBookingAdminDto
{
    public string Reason { get; set; } = null!;

    public bool FullRefund { get; set; } = true;
}

public class BookingSearchFilterDto
{
    public string? BookingCode { get; set; }

    public string? UserEmail { get; set; }

    public int? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class RefundManagementResponse
{
    public int RefundId { get; set; }

    public string BookingCode { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public decimal BookingAmount { get; set; }

    public decimal RefundAmount { get; set; }

    public int RefundStatus { get; set; } // 0=Pending, 1=Approved, 2=Processed, 3=Rejected

    public string RefundStatusName { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}

public class ApproveRefundDto
{
    public bool Approved { get; set; }

    public string? Reason { get; set; }
}
