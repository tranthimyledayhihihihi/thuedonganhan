using System;

namespace WEB.Models
{
    public class ComplaintViewModel
    {
        public int ComplaintId { get; set; }
        public int RentalId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public RentalViewModel? Rental { get; set; }
    }

    public class CreateComplaintRequest
    {
        public int RentalId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsOwner { get; set; } // Flag to know if they are the owner when rendering the view
        public Microsoft.AspNetCore.Http.IFormFile? EvidenceFile { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? RentingOutEvidenceFile { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? RetrievalEvidenceFile { get; set; }
    }
}
