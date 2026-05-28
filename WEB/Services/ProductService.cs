using WEB.Models;
using System.Text.Json.Serialization;

namespace WEB.Services
{
    public class PaginatedProductResponse
    {
        [JsonPropertyName("items")]
        public List<ProductViewModel> Items { get; set; } = new List<ProductViewModel>();
    }

    public class ProductService
    {
        private readonly ApiService _apiService;

        public ProductService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ApiResponse<List<ProductViewModel>>?> GetAllProductsAsync()
        {
            var rawResponse = await _apiService.GetAsync<ApiResponse<PaginatedProductResponse>>("Product");
            if (rawResponse != null)
            {
                return new ApiResponse<List<ProductViewModel>>
                {
                    Success = rawResponse.Success,
                    Message = rawResponse.Message,
                    Data = rawResponse.Data?.Items ?? new List<ProductViewModel>(),
                    Errors = rawResponse.Errors
                };
            }
            return null;
        }

        public async Task<ApiResponse<List<ProductViewModel>>?> GetFilteredProductsAsync(string endpoint)
        {
            var rawResponse = await _apiService.GetAsync<ApiResponse<PaginatedProductResponse>>(endpoint);
            if (rawResponse != null)
            {
                return new ApiResponse<List<ProductViewModel>>
                {
                    Success = rawResponse.Success,
                    Message = rawResponse.Message,
                    Data = rawResponse.Data?.Items ?? new List<ProductViewModel>(),
                    Errors = rawResponse.Errors
                };
            }
            return null;
        }

        public async Task<ApiResponse<ProductViewModel>?> GetProductByIdAsync(int id)
        {
            return await _apiService.GetAsync<ApiResponse<ProductViewModel>>($"Product/{id}");
        }

        public async Task<ApiResponse<List<ProductViewModel>>?> SearchProductsAsync(string keyword)
        {
            return await _apiService.GetAsync<ApiResponse<List<ProductViewModel>>>($"Product/search?keyword={keyword}");
        }

        public async Task<ApiResponse<List<ProductViewModel>>?> GetProductsByCategoryAsync(int categoryId)
        {
            return await _apiService.GetAsync<ApiResponse<List<ProductViewModel>>>($"Product/category/{categoryId}");
        }

        public async Task<ApiResponse<List<ProductViewModel>>?> GetProductsByUserIdAsync(int userId)
        {
            return await _apiService.GetAsync<ApiResponse<List<ProductViewModel>>>($"Product/user/{userId}");
        }

        public async Task<ApiResponse<ProductViewModel>?> CreateProductAsync(CreateProductRequest request, string jwtToken)
        {
            return await _apiService.PostAsync<CreateProductRequest, ApiResponse<ProductViewModel>>("Product", request);
        }

        public async Task<ApiResponse<object>?> CreateRentalAsync(CreateRentalRequest request)
        {
            return await _apiService.PostAsync<CreateRentalRequest, ApiResponse<object>>("Rental", request);
        }

        public async Task<ApiResponse<List<RentalViewModel>>?> GetRentalsByOwnerAsync(int ownerId)
        {
            return await _apiService.GetAsync<ApiResponse<List<RentalViewModel>>>($"Rental/owner/{ownerId}");
        }

        public async Task<ApiResponse<RentalViewModel>?> GetRentalByIdAsync(int rentalId)
        {
            return await _apiService.GetAsync<ApiResponse<RentalViewModel>>($"Rental/{rentalId}");
        }

        public async Task<ApiResponse<ProductViewModel>?> UpdateProductAsync(int id, CreateProductRequest request, string jwtToken)
        {
            return await _apiService.PutAsync<CreateProductRequest, ApiResponse<ProductViewModel>>($"Product/{id}", request);
        }

        public async Task<ApiResponse<bool>?> DeleteProductAsync(int id, string jwtToken)
        {
            var result = await _apiService.DeleteAsync($"Product/{id}");
            return new ApiResponse<bool> { Success = result, Data = result };
        }

        public async Task<ApiResponse<object>?> GetBookedDatesAsync(int productId)
        {
            return await _apiService.GetAsync<ApiResponse<object>>($"Rental/product/{productId}/booked-dates");
        }

        public async Task<ApiResponse<List<CategoryViewModel>>?> GetAllCategoriesAsync()
        {
            return await _apiService.GetAsync<ApiResponse<List<CategoryViewModel>>>("Category");
        }
    }
}
