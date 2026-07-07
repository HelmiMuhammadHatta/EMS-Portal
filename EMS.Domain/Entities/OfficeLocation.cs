namespace EMS.Domain.Entities;

public class OfficeLocation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; }
}
