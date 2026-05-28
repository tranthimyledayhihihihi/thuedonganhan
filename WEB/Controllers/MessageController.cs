using Microsoft.AspNetCore.Mvc;
using WEB.Filters;

namespace WEB.Controllers
{
    [UteStudentAuthorization] // Yêu cầu đăng nhập và là sinh viên UTE
    public class MessageController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            return View();
        }
    }
}
