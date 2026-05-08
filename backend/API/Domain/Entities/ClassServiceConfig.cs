namespace API.Domain.Entities;

public class ClassServiceConfig
{
    public int Id { get; set; }

    public int SeatClassId { get; set; }

    public int AdditionalServiceId { get; set; }

    public bool IsIncluded { get; set; }

    // Navigation properties
    public virtual SeatClass SeatClass { get; set; } = null!;

    public virtual AdditionalService AdditionalService { get; set; } = null!;
}
