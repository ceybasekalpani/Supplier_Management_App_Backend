namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class FertilizerItemConfigurationsDto
    {
        public List<FertilizerItemConfigurationRowDto> Fertilizers { get; set; } = new();
        public List<FertilizerItemConfigurationRowDto> Items { get; set; } = new();
    }

    public class FertilizerItemConfigurationRowDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateFertilizerItemConfigurationDto
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateFertilizerItemConfigurationDto
    {
        public string Name { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }
}
