using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miniApp.Web.Pages
{
    public class ProfileModel : PageModel
    {
        public string Username { get; set; } = "bb";
        public string RoleDescription { get; set; } = "Staff";
        public string Email { get; set; } = "bb@minichic.com";
        public string Phone { get; set; } = "0812345678";

        public void OnGet()
        {
            // ดึงข้อมูลจริงจาก session หรือ API ได้
        }
    }
}
