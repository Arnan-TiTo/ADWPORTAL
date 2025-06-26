using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miniApp.Web.Services;
using miniApp.Web.Models;
using System.Threading.Tasks;

namespace miniApp.Web.Pages
{
    public class LocationModel : PageModel
    {
        private readonly LocationService _locationService;
        public LocationModel(LocationService locationService)
        {
            _locationService = locationService;

        }

        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? Note { get; set; }

        [BindProperty]
        public float Latitude { get; set; }

        [BindProperty]
        public float Longitude { get; set; }

        [BindProperty]
        public IFormFile? Image { get; set; }

        public string Username => User.Identity?.Name ?? "Guest";
        public string RoleDescription => User.IsInRole("admin") ? "Administrator" : "Staff";
        public string CurrentDate => DateTime.Now.ToString("yyyy-MM-dd");
        public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var locationDto = new LocationDto
                {
                    Name = Name ?? string.Empty,
                    Note = Note,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Image = Image
                };

               var success = await _locationService.CreateLocationAsync(locationDto);

                if (!success)
                {
                    ModelState.AddModelError("", "Code Failed to submit location.");
                    return Page();

                }

                return RedirectToPage("/Locations");


            }
            catch (UnauthorizedAccessException)
            {
               return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "code catch Failed to submit location: " + ex.Message);
                Console.WriteLine($"Error details: {ex.ToString()}");
                return Page();

            }
        }
    }
}
