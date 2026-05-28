using Microsoft.AspNetCore.Mvc;
using WEB.Filters;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers
{
    [UteStudentAuthorization]
    public class WalletController : Controller
    {
        private readonly ApiService _apiService;

        public WalletController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Wallet
        public async Task<IActionResult> Index()
        {
            var walletModel = new WalletViewModel();

            // Fetch balance
            var balanceResponse = await _apiService.GetAsync<ApiResponse<decimal>>("Wallet/balance");
            if (balanceResponse != null && balanceResponse.Success)
            {
                walletModel.Balance = balanceResponse.Data;
            }
            else
            {
                ViewBag.Error = balanceResponse?.Message ?? "Không thể lấy thông tin số dư tài khoản";
            }

            // Fetch transactions
            var transactionsResponse = await _apiService.GetAsync<ApiResponse<List<TransactionViewModel>>>("Wallet/transactions");
            if (transactionsResponse != null && transactionsResponse.Success && transactionsResponse.Data != null)
            {
                walletModel.Transactions = transactionsResponse.Data;
            }
            else
            {
                ViewBag.ErrorTransactions = transactionsResponse?.Message ?? "Không thể lấy lịch sử giao dịch";
            }

            return View(walletModel);
        }

        // GET: /Wallet/GetBalance
        [HttpGet]
        public async Task<IActionResult> GetBalance()
        {
            var response = await _apiService.GetAsync<ApiResponse<decimal>>("Wallet/balance");
            if (response != null && response.Success)
            {
                return Json(new { success = true, data = response.Data });
            }
            return Json(new { success = false, message = response?.Message ?? "Không thể lấy số dư" });
        }

        // POST: /Wallet/Deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(decimal amount, string? description)
        {
            if (amount < 10000)
            {
                TempData["ErrorMessage"] = "Số tiền nạp tối thiểu là 10,000đ";
                return RedirectToAction(nameof(Index));
            }

            var request = new { Amount = amount, Description = description };
            var response = await _apiService.PostAsync<object, ApiResponse<decimal>>("Wallet/deposit", request);

            if (response != null && response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "Nạp tiền thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = response?.Message ?? "Nạp tiền thất bại, vui lòng thử lại!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Wallet/Withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(decimal amount, string? description)
        {
            if (amount < 10000)
            {
                TempData["ErrorMessage"] = "Số tiền rút tối thiểu là 10,000đ";
                return RedirectToAction(nameof(Index));
            }

            var request = new { Amount = amount, Description = description };
            var response = await _apiService.PostAsync<object, ApiResponse<decimal>>("Wallet/withdraw", request);

            if (response != null && response.Success)
            {
                TempData["SuccessMessage"] = response.Message ?? "Rút tiền thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = response?.Message ?? "Rút tiền thất bại, vui lòng thử lại!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
