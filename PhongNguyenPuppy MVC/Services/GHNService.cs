using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhongNguyenPuppy_MVC.Models;

namespace PhongNguyenPuppy_MVC.Services
{
    public interface IGHNService
    {
        Task<int> CalculateShippingFeeAsync(int toDistrictId, string toWardCode, int totalWeight);
        Task<List<GHNProvince>?> GetProvincesAsync();
        Task<List<GHNDistrict>?> GetDistrictsAsync(int provinceId);
        Task<List<GHNWard>?> GetWardsAsync(int districtId);
        Task<string?> CreateOrderAsync(GHNCreateOrderRequest request);

    }

    public class GHNService : IGHNService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly int _shopId;
        private readonly int _fromDistrictId;
        private readonly ILogger<GHNService> _logger;


        public GHNService(IConfiguration configuration, HttpClient httpClient, ILogger<GHNService> logger)
        {
            _httpClient = httpClient;
            _token = configuration["GHN:Token"] ?? throw new ArgumentNullException("GHN Token is missing");
            _shopId = int.Parse(configuration["GHN:ShopId"] ?? throw new ArgumentNullException("GHN ShopId is missing"));
            _fromDistrictId = int.Parse(configuration["GHN:FromDistrictId"] ?? "1542");
            _logger = logger;

            _httpClient.BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/shiip/public-api/");
            _httpClient.DefaultRequestHeaders.Add("Token", _token);
        }

        public async Task<int> CalculateShippingFeeAsync(int toDistrictId, string toWardCode, int totalWeight)
        {
            try
            {
                var request = new GHNCalculateFeeRequest
                {
                    service_type_id = 2,
                    from_district_id = _fromDistrictId,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    height = 20,
                    length = 30,
                    weight = totalWeight > 0 ? totalWeight : 500,
                    width = 20
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                _logger.LogInformation("[GHN] Calculating fee - Request: {Request}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/fee")
                {
                    Content = content
                };
                httpRequest.Headers.Add("ShopId", _shopId.ToString());

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("[GHN] Response Status: {StatusCode}, Body: {Response}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNCalculateFeeResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.code == 200 && result.data != null)
                    {
                        _logger.LogInformation("[GHN] Fee calculated successfully: {Fee} VNĐ", result.data.total);
                        return result.data.total;
                    }
                    else
                    {
                        _logger.LogWarning("[GHN] API returned error - Code: {Code}, Message: {Message}",
                            result?.code, result?.message);
                    }
                }

                _logger.LogWarning("[GHN] API call failed, returning default fee 30,000 VNĐ");
                return 30000;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GHN] Exception occurred while calculating shipping fee");
                return 30000;
            }
        }

        public async Task<List<GHNProvince>?> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("master-data/province");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNProvinceResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GHN] Error getting provinces");
                return null;
            }
        }

        public async Task<List<GHNDistrict>?> GetDistrictsAsync(int provinceId)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { province_id = provinceId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("master-data/district", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNDistrictResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GHN] Error getting districts for province {ProvinceId}", provinceId);
                return null;
            }
        }

        public async Task<List<GHNWard>?> GetWardsAsync(int districtId)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { district_id = districtId }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("master-data/ward", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNWardResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GHN] Error getting wards for district {DistrictId}", districtId);
                return null;
            }
        }
        // THÊM MỚI: Tạo đơn hàng trên GHN
        public async Task<string?> CreateOrderAsync(GHNCreateOrderRequest request)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                _logger.LogInformation("[GHN] Creating order - Request: {Request}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/create")
                {
                    Content = content
                };
                httpRequest.Headers.Add("ShopId", _shopId.ToString());

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("[GHN] Create Order Response Status: {StatusCode}, Body: {Response}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNCreateOrderResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result?.code == 200 && result.data != null)
                    {
                        _logger.LogInformation("[GHN] Order created successfully: {OrderCode}", result.data.order_code);
                        return result.data.order_code; // Trả về mã vận đơn
                    }
                    else
                    {
                        _logger.LogWarning("[GHN] Create order failed - Code: {Code}, Message: {Message}",
                            result?.code, result?.message);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GHN] Exception occurred while creating order");
                return null;
            }
        }
    }
}