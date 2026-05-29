using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.ViewModel
{
    [Keyless]
    public class RecommendedCarViewModel
    {
        [Column("car_id")]
        public int CarId { get; set; }

        [Column("make")]
        public string Make { get; set; } = null!;

        [Column("model")]
        public string Model { get; set; } = null!;

        [Column("year")]
        public int Year { get; set; }

        [Column("listing_price")]
        public decimal ListingPrice { get; set; }

        [Column("city")]
        public string? City { get; set; }

        // FIX: Binds PascalCase C# parameter directly to lowercase snake_case database outputs
        [Column("body_condition")]
        public string? BodyCondition { get; set; }

        // FIX: Direct mapping for raw technical assessment notes column notes
        [Column("description_score")]
        public string? DescriptionScore { get; set; }

        [Column("trust_score")]
        public decimal TrustScore { get; set; }

        [Column("DynamicMatchScore")]
        public int DynamicMatchScore { get; set; }
    }
}