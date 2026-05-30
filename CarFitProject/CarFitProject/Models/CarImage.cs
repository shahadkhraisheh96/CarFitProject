using System;

namespace CarFitProject.Models;

public partial class CarImage
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public virtual Car? Car { get; set; }
}
