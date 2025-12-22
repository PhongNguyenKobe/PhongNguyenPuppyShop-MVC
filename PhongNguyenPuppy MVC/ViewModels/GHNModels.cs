namespace PhongNguyenPuppy_MVC.Models
{
    // Request model để tính phí vận chuyển
    public class GHNCalculateFeeRequest
    {
        public int service_type_id { get; set; } = 2; // 1: Express, 2: Standard
        public int? insurance_value { get; set; }
        public string? coupon { get; set; }
        public int from_district_id { get; set; }
        public int to_district_id { get; set; }
        public string to_ward_code { get; set; } = string.Empty; // Đổi từ int sang string
        public int height { get; set; }
        public int length { get; set; }
        public int weight { get; set; }
        public int width { get; set; }
    }

    // Response model từ GHN API
    public class GHNCalculateFeeResponse
    {
        public int code { get; set; }
        public string? message { get; set; }
        public GHNFeeData? data { get; set; }
    }

    public class GHNFeeData
    {
        public int total { get; set; }
        public int service_fee { get; set; }
        public int insurance_fee { get; set; }
        public int pick_station_fee { get; set; }
        public int coupon_value { get; set; }
    }

    // Model để lấy danh sách tỉnh/thành phố
    public class GHNProvinceResponse
    {
        public int code { get; set; }
        public string? message { get; set; }
        public List<GHNProvince>? data { get; set; }
    }

    public class GHNProvince
    {
        public int ProvinceID { get; set; }
        public string? ProvinceName { get; set; }
        public int CountryID { get; set; }
        public string? Code { get; set; }
    }

    // Model để lấy danh sách quận/huyện
    public class GHNDistrictResponse
    {
        public int code { get; set; }
        public string? message { get; set; }
        public List<GHNDistrict>? data { get; set; }
    }

    public class GHNDistrict
    {
        public int DistrictID { get; set; }
        public int ProvinceID { get; set; }
        public string? DistrictName { get; set; }
        public string? Code { get; set; }
    }

    // Model để lấy danh sách phường/xã
    public class GHNWardResponse
    {
        public int code { get; set; }
        public string? message { get; set; }
        public List<GHNWard>? data { get; set; }
    }

    public class GHNWard
    {
        public string? WardCode { get; set; }
        public int DistrictID { get; set; }
        public string? WardName { get; set; }
    }

    // Request tạo đơn GHN
    public class GHNCreateOrderRequest
    {
        public int to_district_id { get; set; }
        public string to_ward_code { get; set; } = "";
        public string to_name { get; set; } = "";
        public string to_phone { get; set; } = "";
        public string to_address { get; set; } = "";
        public int cod_amount { get; set; } // Tiền thu hộ COD
        public string content { get; set; } = "Hàng hóa";
        public int weight { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int service_type_id { get; set; } = 2;
        public int payment_type_id { get; set; } = 1; // 1: Người gửi trả phí, 2: Người nhận trả phí
        public string? note { get; set; }
        public string required_note { get; set; } = "KHONGCHOXEMHANG"; // Giá trị mặc định
        public List<GHNOrderItem> items { get; set; } = new();
    }

    public class GHNOrderItem
    {
        public string name { get; set; } = "";
        public int quantity { get; set; }
        public int weight { get; set; }
    }

    // Response từ GHN
    public class GHNCreateOrderResponse
    {
        public int code { get; set; }
        public string? message { get; set; }
        public GHNOrderData? data { get; set; }
    }

    public class GHNOrderData
    {
        public string? order_code { get; set; } // Mã vận đơn GHN
        public string? sort_code { get; set; }
        public string? trans_type { get; set; }
        public int total_fee { get; set; }
        public string? expected_delivery_time { get; set; }
    }
}