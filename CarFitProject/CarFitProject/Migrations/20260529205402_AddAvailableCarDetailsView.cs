using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableCarDetailsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER VIEW vw_AvailableCarDetails AS
SELECT
    c.id              AS CarId,
    c.scraped_id      AS scraped_id,
    c.make            AS make,
    c.model           AS model,
    c.trim            AS trim,
    c.year            AS year,
    c.kilometers      AS kilometers,
    c.body_type       AS body_type,
    c.seats           AS seats,
    c.fuel_type       AS fuel_type,
    c.transmission    AS transmission,
    c.engine_size     AS engine_size,
    c.exterior_color  AS exterior_color,
    c.interior_color  AS interior_color,
    c.regional_specs  AS regional_specs,
    c.price           AS price,
    c.interior_options    AS interior_options,
    c.exterior_options    AS exterior_options,
    c.technology_options  AS technology_options,
    c.images          AS images,
    cl.id             AS ListingId,
    cl.listing_price  AS listing_price,
    cl.payment_method_allowed AS payment_method_allowed,
    ISNULL(s.id, 0)   AS SellerId,
    ISNULL(s.name, N'') AS SellerName,
    s.city            AS city,
    s.neighborhood    AS neighborhood,
    ir.body_condition AS body_condition,
    ir.paint_status   AS paint_status,
    ir.description_score AS description_score,
    ISNULL(ir.calculated_trust_score, CAST(0 AS decimal(3,2))) AS TrustScore
FROM Cars c
INNER JOIN CarListings cl ON cl.car_id = c.id
LEFT JOIN Sellers s        ON s.id = cl.seller_id
LEFT JOIN InspectionReports ir ON ir.car_id = c.id
WHERE cl.available = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('vw_AvailableCarDetails', 'V') IS NOT NULL DROP VIEW vw_AvailableCarDetails;");
        }
    }
}
