using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;
using System.Threading.Tasks;

namespace WEB.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly ComplaintService _complaintService;
        private readonly ProductService _productService;

        public ComplaintController(ComplaintService complaintService, ProductService productService)
        {
            _complaintService = complaintService;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int rentalId)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(token) || !userId.HasValue) return RedirectToAction("Login", "Account");

            bool isOwner = false;
            var rentalRes = await _productService.GetRentalByIdAsync(rentalId);
            if (rentalRes != null && rentalRes.Success && rentalRes.Data != null)
            {
                if (rentalRes.Data.Product != null && rentalRes.Data.Product.OwnerId == userId.Value)
                {
                    isOwner = true;
                }
            }

            var model = new CreateComplaintRequest { RentalId = rentalId, IsOwner = isOwner };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateComplaintRequest request)
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            if (request.IsOwner)
            {
                if (request.RentingOutEvidenceFile == null || request.RentingOutEvidenceFile.Length == 0)
                {
                    ModelState.AddModelError("RentingOutEvidenceFile", "Ảnh từ lúc cho thuê là bắt buộc đối với người cho thuê.");
                }
                if (request.RetrievalEvidenceFile == null || request.RetrievalEvidenceFile.Length == 0)
                {
                    ModelState.AddModelError("RetrievalEvidenceFile", "Ảnh hoặc video khi đi lấy đồ là bắt buộc đối với người cho thuê.");
                }
            }
            else
            {
                if (request.EvidenceFile == null || request.EvidenceFile.Length == 0)
                {
                    ModelState.AddModelError("EvidenceFile", "Bằng chứng hình ảnh/video là bắt buộc.");
                }
            }

            if (!ModelState.IsValid)
                return View(request);

            if (request.IsOwner)
            {
                string rentingOutPath = string.Empty;
                string retrievalPath = string.Empty;

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "complaints");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (request.RentingOutEvidenceFile != null && request.RentingOutEvidenceFile.Length > 0)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.RentingOutEvidenceFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.RentingOutEvidenceFile.CopyToAsync(fileStream);
                    }
                    rentingOutPath = "/uploads/complaints/" + uniqueFileName;
                }

                if (request.RetrievalEvidenceFile != null && request.RetrievalEvidenceFile.Length > 0)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.RetrievalEvidenceFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.RetrievalEvidenceFile.CopyToAsync(fileStream);
                    }
                    retrievalPath = "/uploads/complaints/" + uniqueFileName;
                }

                request.ImageUrl = rentingOutPath + ";" + retrievalPath;
            }
            else
            {
                if (request.EvidenceFile != null && request.EvidenceFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "complaints");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.EvidenceFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.EvidenceFile.CopyToAsync(fileStream);
                    }

                    request.ImageUrl = "/uploads/complaints/" + uniqueFileName;
                }
            }

            var response = await _complaintService.CreateComplaintAsync(request, token);
            if (response != null && response.Success)
            {
                TempData["SuccessMessage"] = "Đã gửi khiếu nại thành công!";
                return RedirectToAction("Index"); // Redirect to the complaint history
            }

            ModelState.AddModelError("", response?.Message ?? "Lỗi khi gửi khiếu nại");
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            var response = await _complaintService.GetUserComplaintsAsync();
            if (response != null && response.Success)
            {
                return View(response.Data);
            }

            return View(new List<ComplaintViewModel>());
        }
    }
}
