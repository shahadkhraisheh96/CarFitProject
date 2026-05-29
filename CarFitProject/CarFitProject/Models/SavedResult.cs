using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class SavedResult
{
    public int UserId { get; set; }

    public int CarId { get; set; }

    public DateTime? SavedAt { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
