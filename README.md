# ⚙️ Zalo Update Blocker (Terminal Edition)

> **Công cụ chặn Zalo PC tự động cập nhật phiên bản mới một cách an toàn, nhanh chóng và gọn nhẹ.**

---

## 👨‍💻 Tác giả & Ghi nhận (Credits & Attribution)

* 🛠️ **Người fork & phát triển bản Terminal (Fork & CLI Developer)**: **Minji No Support**
* 💡 **Tác giả dự án gốc (Original Creator)**: [**ThanhNguyenVN93**](https://github.com/ThanhNguyenVN93)
* 🔗 **Kho lưu trữ gốc (Original Repository)**: [https://github.com/ThanhNguyenVN93/Zalo-Update-Blocker](https://github.com/ThanhNguyenVN93/Zalo-Update-Blocker)

> *Xin chân thành gửi lời cảm ơn sâu sắc đến tác giả **ThanhNguyenVN93** vì đã chia sẻ giải pháp mã nguồn mở hữu ích, tạo nền tảng để **Minji No Support** fork và tối ưu hóa phiên bản Terminal Edition này! ❤️*

---

## 🌟 Điểm nổi bật của bản Terminal Edition

* 🚀 **Cực kỳ nhẹ**: Ứng dụng độc lập (standalone) chỉ ~181 KB, khởi chạy tức thì.
* 🛡️ **An toàn tuyệt đối**:
  * Tự động sao lưu file `config.json` ra Desktop trước mỗi lần sửa đổi cấu hình.
  * Chuẩn hóa mã hóa **UTF-8 No BOM** (chống lỗi JavaScript `SyntaxError` của Electron/Node.js).
* 🎮 **Linh hoạt**: Hỗ trợ cả giao diện menu số trực quan và tham số dòng lệnh (CLI flags).
* ⚡ **Không phụ thuộc thư viện**: Có thể chạy ngay trên Windows mà không cần cài thêm bất kỳ runtime hay thư viện nào.

---

## 🚀 Hướng dẫn chạy file chi tiết

### Cách 1: Chạy bằng Menu tương tác (Khuyên dùng cho người mới)

1. Nhấp đúp chuột trực tiếp vào file **`ZaloBlocker.exe`** (hoặc mở Terminal/CMD/PowerShell tại thư mục này và gõ `.\ZaloBlocker.exe`).
2. Chương trình sẽ mở lên và kiểm tra ngay trạng thái Zalo hiện tại của bạn:

```text
  ╔════════════════════════════════════════════════════════════════╗
  ║             ⚡ ZALO UPDATE BLOCKER [TERMINAL v2.0] ⚡          ║
  ║             Tool chặn Zalo PC tự động cập nhật bản mới         ║
  ║  Forked by: Minji No Support | Original: ThanhNguyenVN93       ║
  ╚════════════════════════════════════════════════════════════════╝

  [TRẠNG THÁI HIỆN TẠI HỆ THỐNG]
  • Tiến trình Zalo       : ĐÃ TẮT (Không chạy)
  • Tự động cập nhật Zalo : 🛡️  ĐÃ CHẶN (BLOCKED - An toàn)
  • File cấu hình         : C:\Users\...\AppData\Roaming\ZaloData\config.json
  • Quyền Administrator   : Có (Administrator)

  [DANH MỤC THAO TÁC]
  ------------------------------------------------
   [1] Chặn tự động cập nhật Zalo (Block Auto-Update)
   [2] Mở lại cập nhật Zalo (Unblock Auto-Update)
   [3] Sao lưu cấu hình config.json (Backup)
   [4] Khôi phục cấu hình từ bản sao lưu (Restore)
   [5] Đóng toàn bộ tiến trình Zalo (Kill Zalo)
   [6] Xem nội dung file cấu hình config.json
   [7] Kiểm tra quyền Administrator & Khởi động lại
   [0] Thoát chương trình (Exit)
  ------------------------------------------------
  👉 Nhập lựa chọn của bạn [0-7]:
```

3. **Thao tác đơn giản**:
   * Nhấn phím **`1`** và bấm **Enter** để **Chặn tự động cập nhật**. Tool sẽ tự động tắt Zalo nếu đang chạy, tạo 1 bản backup, và ghi đè cờ chặn update.
   * Nhấn phím **`2`** và bấm **Enter** để **Mở lại cập nhật** khi bạn muốn Zalo update lên phiên bản mới.
   * Nhấn phím **`3`** để tạo bản sao lưu thủ công.
   * Nhấn phím **`4`** để xem danh sách các bản sao lưu cũ và khôi phục khi cần.
   * Nhấn phím **`0`** để thoát.

---

### Cách 2: Chạy bằng tham số dòng lệnh (CLI Flags)

Thích hợp cho bạn nào thích thao tác bằng phím, gõ lệnh trong Windows Terminal, CMD, PowerShell, hoặc tích hợp vào các script tự động hóa:

| Lệnh | Mô tả chi tiết |
| :--- | :--- |
| `.\ZaloBlocker.exe -b` *(hoặc `--block`)* | Tự đóng Zalo, tự động sao lưu và kích hoạt chặn cập nhật ngay lập tức |
| `.\ZaloBlocker.exe -u` *(hoặc `--unblock`)* | Tự đóng Zalo, gỡ bỏ cờ chặn để Zalo cập nhật bình thường |
| `.\ZaloBlocker.exe -s` *(hoặc `--status`)* | Kiểm tra trạng thái tiến trình Zalo và cấu hình hiện tại |
| `.\ZaloBlocker.exe -k` *(hoặc `--kill`)* | Đóng ngay tất cả các tiến trình `Zalo.exe` đang chạy |
| `.\ZaloBlocker.exe --backup` | Tạo nhanh 1 file sao lưu `config.json` ra Desktop |
| `.\ZaloBlocker.exe --restore <đường_dẫn>` | Khôi phục cấu hình từ file sao lưu chỉ định |
| `.\ZaloBlocker.exe -v` *(hoặc `--view`)* | Xem nội dung định dạng JSON của file cấu hình |
| `.\ZaloBlocker.exe -q` *(hoặc `--quiet`)* | Chạy ở chế độ im lặng (không hiển thị menu, trả mã 0 hoặc 1) |
| `.\ZaloBlocker.exe -h` *(hoặc `--help`)* | Hiển thị bảng trợ giúp lệnh |

**Ví dụ thực tế:**
```powershell
# Xem trạng thái:
.\ZaloBlocker.exe -s

# Chặn cập nhật nhanh:
.\ZaloBlocker.exe -b

# Mở lại cập nhật:
.\ZaloBlocker.exe -u
```

---

## 🛠️ Biên dịch lại mã nguồn (Build from Source)

Nếu bạn có chỉnh sửa file mã nguồn `Program.cs`, bạn có thể build lại file thực thi `ZaloBlocker.exe` bất kỳ lúc nào:

* **Cách 1 (Nhanh nhất - 1 click)**: Nhấp đúp chuột vào file `build.bat`. Script sẽ tự động gọi trình biên dịch `csc.exe` tích hợp sẵn trong Windows để tạo file `ZaloBlocker.exe`.
* **Cách 2 (Visual Studio / MSBuild)**:
  ```powershell
  msbuild ZaloBlocker.csproj /p:Configuration=Release
  ```

---

## 📜 Giấy phép (License)

Dự án được phân phối dưới giấy phép **MIT License**. Xem chi tiết tại file [LICENSE](LICENSE).
