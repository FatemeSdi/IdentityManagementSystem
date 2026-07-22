namespace IdentityManagementSystem.API.Models.ViewModels
{
    public class ValidateRequestViewModel
    {
        public long RequestId { get; set; }
        public bool ValidateByExpert { get; set; }
        public string? Description { get; set; }
    }
}
