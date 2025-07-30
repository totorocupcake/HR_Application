namespace HR_Application.Models
{
    public class LocationDropDownItem
    {
        public int LocationId { get; set; }
        public string? StreetAddress { get; set; }
        public string? PostalCode { get; set; }
        public string City { get; set; } = string.Empty;
        public string? StateProvince { get; set; }
        public string? Country { get; set; }

    }
}
