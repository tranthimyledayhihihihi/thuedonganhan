# Hướng dẫn tải hình ảnh cho dự án

## Cách 1: Sử dụng URL trực tiếp từ Unsplash (Đang dùng)

Dự án hiện đang sử dụng URL trực tiếp từ Unsplash API. Điều này:
- ✅ Miễn phí và hợp pháp
- ✅ Không cần tải về
- ✅ Tự động tối ưu hóa
- ✅ CDN nhanh
- ⚠️ Cần internet để hiển thị

## Cách 2: Tải về và lưu local (Khuyến nghị cho production)

### Bước 1: Truy cập Unsplash
1. Vào https://unsplash.com
2. Tìm kiếm với từ khóa:
   - "laptop student"
   - "macbook desk"
   - "camera photography"
   - "graduation gown"
   - "tablet studying"
   - "headphones"
   - "bicycle"
   - "backpack student"

### Bước 2: Tải hình ảnh
1. Click vào hình ảnh bạn thích
2. Click nút "Download" (màu xanh)
3. Chọn kích thước phù hợp (Medium hoặc Large)

### Bước 3: Đổi tên và lưu
Lưu vào các thư mục tương ứng:

```
WEB/wwwroot/images/
├── products/
│   ├── macbook-air.jpg
│   ├── sony-camera.jpg
│   ├── graduation-gown.jpg
│   ├── ipad-pro.jpg
│   ├── headphones.jpg
│   └── bicycle.jpg
├── categories/
│   ├── electronics.jpg
│   ├── books.jpg
│   ├── fashion.jpg
│   └── photography.jpg
└── banners/
    ├── hero-banner.jpg
    └── library.jpg
```

### Bước 4: Cập nhật ImageHelper.cs
Thay đổi từ:
```csharp
public const string MacbookAir = "https://images.unsplash.com/...";
```

Thành:
```csharp
public const string MacbookAir = "/images/products/macbook-air.jpg";
```

## Cách 3: Sử dụng Pexels API

### Đăng ký API Key
1. Vào https://www.pexels.com/api/
2. Đăng ký tài khoản miễn phí
3. Lấy API key

### Cài đặt package
```bash
dotnet add package PexelsDotNetSDK
```

### Sử dụng trong code
```csharp
var client = new PexelsClient("YOUR_API_KEY");
var result = await client.SearchPhotosAsync("laptop student", pageSize: 10);
```

## Danh sách hình ảnh cần thiết

### Sản phẩm (Products)
1. ✅ MacBook Air - Laptop cao cấp
2. ✅ Sony Camera - Máy ảnh mirrorless
3. ✅ Lễ phục tốt nghiệp - Áo tốt nghiệp
4. ✅ iPad Pro - Máy tính bảng
5. ✅ Sony Headphones - Tai nghe chống ồn
6. ✅ Xe đạp thể thao - Bicycle
7. 📚 Sách giáo trình - Textbooks
8. 🎒 Ba lô sinh viên - Backpack
9. ⌨️ Bàn phím cơ - Mechanical keyboard
10. 🖱️ Chuột gaming - Gaming mouse

### Danh mục (Categories)
1. ✅ Điện tử - Electronics workspace
2. ✅ Sách vở - Books and study
3. ✅ Thời trang - Fashion items
4. ✅ Nhiếp ảnh - Photography equipment

### Banner
1. ✅ Thư viện - Library interior
2. 🎓 Khuôn viên trường - Campus
3. 📖 Bàn học - Study desk

## Tối ưu hóa hình ảnh

### Kích thước khuyến nghị
- Product cards: 800x600px
- Category cards: 600x400px
- Banners: 1400x600px
- Thumbnails: 400x300px

### Công cụ tối ưu
1. **TinyPNG** - https://tinypng.com
   - Nén ảnh không mất chất lượng
   - Giảm 50-70% dung lượng

2. **Squoosh** - https://squoosh.app
   - Công cụ của Google
   - Nhiều định dạng (WebP, AVIF)

3. **ImageOptim** (Mac) hoặc **FileOptimizer** (Windows)
   - Tối ưu hàng loạt

### Format khuyến nghị
- WebP: Tốt nhất cho web (nhỏ, chất lượng cao)
- JPEG: Tương thích tốt
- PNG: Cho hình có nền trong suốt

## Lưu ý bản quyền

### ✅ Được phép
- Sử dụng cho mục đích thương mại
- Chỉnh sửa, crop, resize
- Không cần xin phép
- Không cần trả phí

### ❌ Không được phép
- Bán lại hình ảnh gốc
- Tạo dịch vụ tương tự Unsplash
- Sử dụng để train AI (một số trường hợp)

### 💡 Nên làm
- Ghi credit cho photographer (không bắt buộc)
- Thêm attribution trong footer
- Ví dụ: "Photos by Unsplash contributors"

## Kiểm tra sau khi cập nhật

1. ✅ Tất cả hình ảnh hiển thị đúng
2. ✅ Không có broken images
3. ✅ Tốc độ tải trang < 3 giây
4. ✅ Responsive trên mobile
5. ✅ Alt text cho accessibility

## Liên hệ hỗ trợ

Nếu cần hỗ trợ thêm về hình ảnh:
- Unsplash Support: https://unsplash.com/contact
- Pexels Help: https://help.pexels.com
