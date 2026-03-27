using System;
using System.ComponentModel.DataAnnotations;

namespace SmartBorrowLK.ViewModels
{
    public class CreateBookingViewModel : IValidatableObject
    {
        public int ListingId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

        // Custom validation to ensure dates make sense
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.Date < DateTime.Today)
            {
                yield return new ValidationResult("Start date cannot be in the past.", new[] { nameof(StartDate) });
            }

            if (EndDate.Date <= StartDate.Date)
            {
                yield return new ValidationResult("End date must be at least one day after the start date.", new[] { nameof(EndDate) });
            }
        }
    }
}