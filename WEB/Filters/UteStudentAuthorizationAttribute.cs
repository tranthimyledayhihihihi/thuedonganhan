using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WEB.Filters
{
    /// <summary>
    /// Custom authorization attribute để kiểm tra:
    /// 1. Người dùng đã đăng nhập chưa
    /// 2. Email có đuôi @sv.ute.udn.vn (sinh viên UTE) hoặc admin@ute.udn.vn
    /// </summary>
    public class UteStudentAuthorizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            
            // Kiểm tra đã đăng nhập chưa
            var token = httpContext.Session.GetString("JWTToken");
            var userEmail = httpContext.Session.GetString("UserEmail");
            
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userEmail))
            {
                // Chưa đăng nhập -> chuyển đến trang đăng nhập
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = httpContext.Request.Path });
                return;
            }
            
            // Kiểm tra email có phải sinh viên UTE không
            if (!IsUteStudent(userEmail))
            {
                // Không phải sinh viên UTE -> hiển thị thông báo lỗi
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "Chỉ sinh viên UTE mới có thể sử dụng chức năng này!";
                }
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }
            
            base.OnActionExecuting(context);
        }
        
        private bool IsUteStudent(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            // Cho phép admin hoặc sinh viên UTE
            return email.EndsWith("@sv.ute.udn.vn", StringComparison.OrdinalIgnoreCase) 
                   || email.Equals("admin@ute.udn.vn", StringComparison.OrdinalIgnoreCase);
        }
    }
}
