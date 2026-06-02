using WEB.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WEB.Services
{
    public class ComplaintService
    {
        private readonly ApiService _apiService;

        public ComplaintService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ApiResponse<ComplaintViewModel>?> CreateComplaintAsync(CreateComplaintRequest request, string jwtToken)
        {
            return await _apiService.PostAsync<CreateComplaintRequest, ApiResponse<ComplaintViewModel>>("Complaint", request);
        }

        public async Task<ApiResponse<List<ComplaintViewModel>>?> GetUserComplaintsAsync()
        {
            return await _apiService.GetAsync<ApiResponse<List<ComplaintViewModel>>>("Complaint/user");
        }

        public async Task<ApiResponse<List<ComplaintViewModel>>?> GetAllComplaintsAsync()
        {
            return await _apiService.GetAsync<ApiResponse<List<ComplaintViewModel>>>("Complaint/admin");
        }

        public async Task<ApiResponse<ComplaintViewModel>?> UpdateComplaintStatusAsync(int id, string status)
        {
            return await _apiService.PutAsync<string, ApiResponse<ComplaintViewModel>>($"Complaint/admin/{id}", status);
        }
        public async Task<ApiResponse<ComplaintViewModel>?> ResolveComplaintAsync(int id, object requestData, string token)
        {
            return await _apiService.PostAsync<object, ApiResponse<ComplaintViewModel>>($"Complaint/admin/{id}/resolve", requestData);
        }
    }
}
