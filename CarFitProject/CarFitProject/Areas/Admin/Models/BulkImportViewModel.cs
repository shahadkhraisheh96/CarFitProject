namespace CarFitProject.Areas.Admin.Models
{
    public class BulkImportViewModel
    {
        public bool IsSuccess { get; set; }
        public int InsertedCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
