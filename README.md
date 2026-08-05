# 24CS-CMC-CNLTW-02

Website bán điện thoại **TECHBLUE** — đồ án môn Công nghệ lập trình Web.

# Link web demo: http://techblue.runasp.net/
**tài khoản demo:
admin
TechBlue@2026#Admin**

## Công nghệ

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQL Server
- Chatbot tư vấn RAG (truy xuất dữ liệu sản phẩm từ DB + LLM)

## Chức năng

**Khách hàng**
- Đăng ký / đăng nhập (tài khoản thường, Google, Facebook)
- Duyệt sản phẩm theo hãng, tìm kiếm, phân trang
- Xem chi tiết, chọn phiên bản màu / dung lượng
- Giỏ hàng, đặt hàng, thanh toán COD hoặc QR
- Theo dõi đơn hàng, đánh giá sản phẩm đã mua
- Chatbot AI tư vấn theo ngân sách và nhu cầu

**Quản trị**
- Quản lý sản phẩm, danh mục, khuyến mại
- Quản lý đơn hàng và trạng thái giao hàng
- Quản lý khách hàng, nhân viên, đánh giá
- Báo cáo doanh thu

## Chạy dự án

```bash
dotnet restore
dotnet run --project Web_ban_đt
```

Mặc định chạy tại `http://localhost:5178`.

## Cấu hình

Tạo file `.env` ở thư mục gốc (file này **không** được commit lên Git):

```
ConnectionStrings__DefaultConnection=<chuỗi kết nối SQL Server>
Chatbot__ApiKey=<API key của nhà cung cấp LLM>
Chatbot__ApiUrl=<endpoint LLM>
Chatbot__Model=<tên model>
Authentication__Google__ClientId=<client id>
Authentication__Google__ClientSecret=<client secret>
Authentication__Facebook__AppId=<app id>
Authentication__Facebook__AppSecret=<app secret>
SEED_ADMIN_PASSWORD=<mật khẩu admin khởi tạo>
```

Database được tạo tự động khi chạy lần đầu.
