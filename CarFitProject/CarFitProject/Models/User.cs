using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<RecommendationLog> RecommendationLogs { get; set; } = new List<RecommendationLog>();

    public virtual ICollection<SavedResult> SavedResults { get; set; } = new List<SavedResult>();
}
