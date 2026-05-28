# 🌟 UTE Rent - Hệ thống Thuê đồ Ngắn hạn Sinh viên UTE

Chào mừng bạn đến với **UTE Rent**, nền tảng kinh tế chia sẻ (sharing economy) hiện đại, cao cấp được phát triển dành riêng cho cộng đồng sinh viên trường **Đại học Sư phạm Kỹ thuật - Đại học Đà Nẵng (UTE)**. 

Hệ thống cho phép sinh viên dễ dàng đăng tin cho thuê các thiết bị học tập dư thừa (laptop, máy ảnh, giáo trình, lễ phục tốt nghiệp, dụng cụ thể thao...) để kiếm thêm thu nhập thụ động, đồng thời hỗ trợ người thuê tìm kiếm và thuê nhanh các vật dụng cần thiết với chi phí cực kỳ ưu đãi.

---

## 🛠 Công Nghệ Sử Dụng

Hệ thống được phát triển theo kiến trúc tách biệt **Backend API** và **Frontend Web Client** nhằm nâng cao hiệu năng, khả năng mở rộng và bảo mật:

*   **Backend (API Service)**:
    *   ASP.NET Core 8.0 Web API
    *   Entity Framework Core (EF Core) - Code First / Database First
    *   SQL Server / SQLEXPRESS (Cơ sở dữ liệu quan hệ)
    *   JWT (JSON Web Token) Authentication
    *   BCrypt.Net (Mã hóa mật khẩu sinh học bảo mật cao)
*   **Frontend (Web Client)**:
    *   ASP.NET Core 8.0 MVC (Model-View-Controller)
    *   Razor Pages & HTML5/CSS3/JavaScript (Vanilla CSS & Bootstrap 5)
    *   FontAwesome 6 (Hệ thống icon động)
    *   Chart.js (Biểu đồ doanh thu trực quan)

---

## ✨ Các Tính Năng Nổi Bật

### 1. Xác thực & Đăng ký Sinh viên Chính chủ
*   Ràng buộc đăng ký tài khoản bằng **mã số sinh viên (MSSV)** và **Email trường** (`@sv.ute.udn.vn`).
*   Xác thực OTP 6 số gửi qua Email trước khi cho phép tài khoản hoạt động.
*   Bảo mật tuyệt đối thông qua mã hóa Hash mật khẩu bằng thuật toán **BCrypt**.

### 2. Đăng tin & Hệ thống Kiểm duyệt Sản phẩm (Product Moderation)
*   Người cho thuê (Owner) đăng sản phẩm dễ dàng, hỗ trợ tải ảnh và định vị vị trí.
*   **Cơ chế kiểm duyệt (New)**: Sản phẩm mới đăng sẽ mặc định ở trạng thái `Chờ duyệt` và tự động ẩn khỏi danh sách hiển thị chung.
*   Trạng thái này được hiển thị rõ ràng bằng nhãn Badge màu vàng `Chờ duyệt` trong trang Dashboard của người cho thuê.
*   Admin có toàn quyền duyệt riêng lẻ từng sản phẩm hoặc **Duyệt toàn bộ yêu cầu** chỉ với 1 click chuột trên trang quản trị.

### 3. Đặt thuê đa dạng gói & Tính tiền thông minh
*   Hỗ trợ cấu hình giá linh hoạt theo nhiều đơn vị thời gian: **Giờ, Ngày, Tuần, Tháng**.
*   Giao diện chọn gói trực quan, tự động nhân số lượng thời gian tương ứng và cộng thêm tiền cọc của thiết bị để tính tổng hóa đơn thanh toán chính xác theo thời gian thực.
*   Ràng buộc lịch trống thông minh: Không cho phép thuê vào những ngày trong quá khứ hoặc những ngày đã có người đặt trước.

### 4. Giỏ hàng & Thanh toán Hàng loạt (Cart System)
*   Cho phép người thuê thêm nhiều món đồ vào Giỏ hàng để chuẩn bị cho các kế hoạch học tập trong tương lai.
*   Hỗ trợ chỉnh sửa thời gian thuê trực tiếp trong giỏ hàng.
*   Thực hiện đặt thuê riêng lẻ từng món hoặc **Đặt thuê tất cả** cực kỳ nhanh chóng.

### 5. Ví Điện Tử & Giao Dịch Không Tiền Mặt (Wallet System)
*   Mỗi sinh viên sở hữu một ví điện tử tích hợp trên hệ thống.
*   Tự động trừ tiền thuê, chuyển tiền cọc từ ví người thuê vào ví người cho thuê khi xác nhận bàn giao thành công.
*   Quản lý lịch sử giao dịch rõ ràng, hỗ trợ nạp tiền và yêu cầu rút tiền mặt về tài khoản ngân hàng.

### 6. Trang Quản Trị Admin chuyên nghiệp
*   Thống kê trực quan: Doanh thu, số lượng giao dịch, biểu đồ tăng trưởng.
*   Quản lý danh mục sản phẩm, quản lý danh sách sinh viên, xử lý yêu cầu rút tiền và kiểm duyệt bài đăng sản phẩm.

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

### 1. Khởi tạo Cơ sở dữ liệu
1. Mở SQL Server Management Studio (SSMS).
2. Mở file [THUEDONGANHAN_DB.sql](file:///e:/THUEDONGANHAN/THUEDONGANHAN/THUEDONGANHAN_DB.sql).
3. Nhấn **Execute** (hoặc nút F5) để khởi tạo toàn bộ cấu trúc bảng, stored procedures, triggers và dữ liệu mẫu (sản phẩm mẫu đã được duyệt sẵn).

### 2. Cấu hình Connection String
*   Mở file [appsettings.json](file:///e:/THUEDONGANHAN/THUEDONGANHAN/THUEDONGANHAN/appsettings.json) trong thư mục `THUEDONGANHAN` (Backend).
*   Chỉnh sửa dòng `DefaultConnection` khớp với tên SQL Server Instance trên máy của bạn (ví dụ: `Server=TÊN_MÁY\\SQLEXPRESS;...`).

### 3. Khởi chạy dự án
Mở hai cửa sổ dòng lệnh riêng biệt trong thư mục dự án và chạy các lệnh sau:

*   **Chạy Backend API** (Cổng mặc định: `https://localhost:7000` / `http://localhost:5000` hoặc tương đương):
    ```bash
    cd THUEDONGANHAN
    dotnet run
    ```
*   **Chạy Frontend MVC** (Cổng mặc định: `https://localhost:7001` / `http://localhost:5001` hoặc tương đương):
    ```bash
    cd WEB
    dotnet run
    ```

*   **Tự động đổ dữ liệu mẫu nhanh (Seed Endpoint)**:
    Truy cập đường dẫn: `GET http://localhost:5001/api/Seed/products` trên trình duyệt để tự động thêm các danh mục, người dùng thử nghiệm và 12 sản phẩm mẫu đã duyệt.

---

## 🔑 Tài Khoản Thử Nghiệm Sẵn Có

| Vai trò | Email đăng nhập | Mật khẩu mặc định |
| :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin@ute.udn.vn` | `Admin@123` |
| **Sinh viên A (Cho thuê & Thuê)** | `23115053122326@sv.ute.udn.vn` | `Student@123` |
| **Sinh viên B (Cho thuê & Thuê)** | `23115053122327@sv.ute.udn.vn` | `Student@123` |
| **Sinh viên C (Chỉ thuê đồ)** | `23115053122329@sv.ute.udn.vn` | `Student@123` |
| **Sinh viên D (Chỉ cho thuê đồ)** | `23115053122330@sv.ute.udn.vn` | `Student@123` |

---
*Chúc bạn có những trải nghiệm tuyệt vời cùng UTE Rent!* 🌟
