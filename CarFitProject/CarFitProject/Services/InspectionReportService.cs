using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface IInspectionReportService
    {
        Task<InspectionReportFormViewModel?> LoadAsync(int carId);
        Task<bool> SaveAsync(int carId, InspectionReportFormViewModel vm);
    }

    public class InspectionReportService : IInspectionReportService
    {
        private readonly CarFitDbContext _context;
        private readonly IInspectionScoringService _scoring;

        public InspectionReportService(CarFitDbContext context, IInspectionScoringService scoring)
        {
            _context = context;
            _scoring = scoring;
        }

        public async Task<InspectionReportFormViewModel?> LoadAsync(int carId)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == carId);
            if (car == null) return null;

            var report = await _context.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.CarId == carId);
            var vm = new InspectionReportFormViewModel
            {
                CarId = carId,
                CarLabel = $"{car.Make} {car.Model} {car.Year}".Trim()
            };

            if (report != null)
            {
                vm.CenterName = report.CenterName;
                vm.InspectionDate = report.InspectionDate;
                vm.Chassis1Status = report.Chassis1Status ?? "جيد";
                vm.Chassis2Status = report.Chassis2Status ?? "جيد";
                vm.Chassis3Status = report.Chassis3Status ?? "جيد";
                vm.Chassis4Status = report.Chassis4Status ?? "جيد";
                vm.BodyCondition = report.BodyCondition;
                vm.RoofCondition = report.RoofCondition;
                vm.PaintStatus = report.PaintStatus;
                vm.PaintFillerStatus = report.PaintFillerStatus;
                vm.DescriptionScore = report.DescriptionScore;
                vm.EngineHealthPercent = report.EngineHealthPercent;
                vm.EngineSmoke = report.EngineSmoke ?? false;
                vm.GearboxStatus = report.GearboxStatus;
                vm.CarseerAttached = report.CarseerAttached ?? false;
                vm.OverallScore = report.OverallScore;
                vm.CalculatedTrustScore = report.CalculatedTrustScore;
            }

            return vm;
        }

        public async Task<bool> SaveAsync(int carId, InspectionReportFormViewModel vm)
        {
            var carExists = await _context.Cars.AnyAsync(c => c.Id == carId);
            if (!carExists) return false;

            var report = await _context.InspectionReports.FirstOrDefaultAsync(r => r.CarId == carId);
            if (report == null)
            {
                report = new InspectionReport { CarId = carId };
                _context.InspectionReports.Add(report);
            }

            report.CenterName = vm.CenterName;
            report.InspectionDate = vm.InspectionDate;
            report.Chassis1Status = vm.Chassis1Status;
            report.Chassis2Status = vm.Chassis2Status;
            report.Chassis3Status = vm.Chassis3Status;
            report.Chassis4Status = vm.Chassis4Status;
            report.BodyCondition = vm.BodyCondition;
            report.RoofCondition = vm.RoofCondition;
            report.PaintStatus = vm.PaintStatus;
            report.PaintFillerStatus = vm.PaintFillerStatus;
            report.DescriptionScore = vm.DescriptionScore;
            report.EngineHealthPercent = vm.EngineHealthPercent;
            report.EngineSmoke = vm.EngineSmoke;
            report.GearboxStatus = vm.GearboxStatus;
            report.CarseerAttached = vm.CarseerAttached;

            _scoring.ApplyTo(report);

            await _context.SaveChangesAsync();

            vm.OverallScore = report.OverallScore;
            vm.CalculatedTrustScore = report.CalculatedTrustScore;
            return true;
        }
    }
}
