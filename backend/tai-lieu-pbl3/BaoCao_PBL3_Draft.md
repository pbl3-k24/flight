# BÁO CÁO ĐỒ ÁN CÔNG NGHỆ PHẦN MỀM
## Đề tài: Xây dựng website đặt vé máy bay

---

## 1. PHẦN MỞ ĐẦU

### 1.1. Mục đích thực hiện đề tài

Trong bối cảnh công nghệ thông tin ngày càng phát triển, các dịch vụ trực tuyến đã trở thành một phần quan trọng trong đời sống. Đối với lĩnh vực hàng không, nhu cầu tìm kiếm chuyến bay, so sánh giá vé và đặt vé trực tuyến ngày càng phổ biến. Người dùng mong muốn có một hệ thống thuận tiện, dễ sử dụng, giúp tiết kiệm thời gian và hỗ trợ quản lý thông tin đặt vé một cách rõ ràng.

Xuất phát từ nhu cầu thực tế đó, đề tài “Xây dựng website đặt vé máy bay” được thực hiện nhằm vận dụng kiến thức của học phần Công nghệ phần mềm vào việc phân tích, thiết kế và xây dựng một hệ thống phần mềm có tính ứng dụng. Thông qua đề tài, sinh viên có thể tiếp cận quy trình phát triển phần mềm từ khảo sát yêu cầu, thiết kế hệ thống, xây dựng cơ sở dữ liệu, lập trình chức năng đến kiểm thử và triển khai thử nghiệm.

### 1.2. Mục tiêu đề tài

Mục tiêu của đề tài là xây dựng một website hỗ trợ người dùng đặt vé máy bay trực tuyến và hỗ trợ quản trị viên quản lý các dữ liệu liên quan đến hoạt động đặt vé.

Cụ thể, hệ thống hướng đến các mục tiêu sau:
* Cho phép người dùng đăng ký, đăng nhập và quản lý thông tin tài khoản.
* Cho phép tìm kiếm chuyến bay theo điểm đi, điểm đến, ngày bay, số lượng hành khách và hạng ghế.
* Hiển thị danh sách chuyến bay phù hợp với điều kiện tìm kiếm.
* Hỗ trợ người dùng đặt vé, nhập thông tin hành khách và theo dõi trạng thái đặt vé.
* Hỗ trợ quản trị viên quản lý sân bay, tuyến bay, máy bay, chuyến bay, lịch bay, người dùng, đơn đặt vé và khuyến mãi.
* Xây dựng hệ thống có cấu trúc rõ ràng, dễ bảo trì và có khả năng mở rộng.

### 1.3. Phạm vi và đối tượng nghiên cứu

Phạm vi của đề tài tập trung vào việc xây dựng một website đặt vé máy bay ở mức đồ án môn học. Hệ thống mô phỏng các chức năng cơ bản của một website đặt vé, không kết nối trực tiếp với hệ thống đặt chỗ thực tế của các hãng hàng không.

Các chức năng chính trong phạm vi đề tài gồm:
* Quản lý tài khoản người dùng.
* Tìm kiếm và hiển thị chuyến bay.
* Đặt vé và quản lý thông tin hành khách.
* Quản lý vé, đơn đặt vé và trạng thái thanh toán.
* Quản lý dữ liệu dành cho quản trị viên như sân bay, tuyến bay, máy bay, chuyến bay và khuyến mãi.

Đối tượng nghiên cứu của đề tài bao gồm quy trình đặt vé máy bay trực tuyến, người dùng có nhu cầu đặt vé, quản trị viên hệ thống và các công nghệ được sử dụng để xây dựng ứng dụng web.

### 1.4. Phương pháp nghiên cứu

Đề tài được thực hiện dựa trên các phương pháp sau:
* Khảo sát và phân tích quy trình đặt vé máy bay trực tuyến.
* Phân tích yêu cầu chức năng và phi chức năng của hệ thống.
* Thiết kế mô hình dữ liệu, giao diện và luồng xử lý nghiệp vụ.
* Xây dựng hệ thống theo mô hình client-server.
* Kiểm thử các chức năng chính như đăng nhập, tìm kiếm chuyến bay, đặt vé và quản lý dữ liệu.
* Triển khai thử nghiệm hệ thống bằng môi trường phát triển cục bộ và Docker.

### 1.5. Cấu trúc của đồ án môn học

Báo cáo đồ án gồm các nội dung chính sau:

* **Chương 1: Phần mở đầu**  
  Trình bày mục đích thực hiện đề tài, mục tiêu, phạm vi, đối tượng nghiên cứu, phương pháp nghiên cứu và cấu trúc báo cáo.
* **Chương 2: Tổng quan đề tài và cơ sở lý thuyết**  
  Trình bày tổng quan về bài toán đặt vé máy bay trực tuyến, ý tưởng xây dựng hệ thống, yêu cầu hoặc wireframe tổng quát, cùng các kỹ thuật và công nghệ sử dụng.
* **Chương 3: Phân tích và thiết kế hệ thống**  
  Trình bày các mô hình phân tích, thiết kế cơ sở dữ liệu, thiết kế chức năng, thiết kế giao diện và thiết kế API.
* **Chương 4: Cài đặt và kiểm thử**  
  Trình bày quá trình cài đặt hệ thống, một số giao diện chính, kết quả thực hiện và các trường hợp kiểm thử tiêu biểu.
* **Chương 5: Kết luận và hướng phát triển**  
  Tổng kết kết quả đạt được, nêu hạn chế của hệ thống và đề xuất hướng phát triển trong tương lai.

---

## 2. CƠ SỞ LÝ THUYẾT

### 2.1. Ý tưởng

Ý tưởng của đề tài là xây dựng một website đặt vé máy bay trực tuyến có giao diện rõ ràng, dễ sử dụng và phù hợp với quy trình đặt vé thông thường. Người dùng có thể tìm kiếm chuyến bay theo nhu cầu, lựa chọn chuyến bay phù hợp, nhập thông tin hành khách và xác nhận đặt vé. Bên cạnh đó, hệ thống cung cấp khu vực quản trị để quản lý dữ liệu nền như sân bay, tuyến bay, máy bay, chuyến bay, người dùng và đơn đặt vé.

Về phía khách hàng, hệ thống cần hỗ trợ các thao tác chính gồm:
* Đăng ký và đăng nhập tài khoản.
* Tìm kiếm chuyến bay theo điểm đi, điểm đến, ngày bay, số lượng hành khách và hạng ghế.
* Xem danh sách chuyến bay phù hợp với điều kiện tìm kiếm.
* Xem thông tin chi tiết của chuyến bay như thời gian khởi hành, thời gian đến, giá vé, hạng ghế và số ghế còn lại.
* Thực hiện đặt vé bằng cách nhập thông tin người liên hệ và thông tin hành khách.
* Theo dõi trạng thái đơn đặt vé và xem lại lịch sử đặt vé.

Về phía quản trị viên, hệ thống cần hỗ trợ các thao tác chính gồm:
* Quản lý thông tin sân bay.
* Quản lý tuyến bay.
* Quản lý máy bay và hạng ghế.
* Quản lý chuyến bay, lịch bay và số lượng ghế.
* Quản lý người dùng và đơn đặt vé.
* Quản lý khuyến mãi hoặc các dịch vụ bổ sung nếu có.

Quy trình đặt vé của khách hàng được mô tả như sau:
1. Người dùng truy cập website.
2. Người dùng nhập thông tin tìm kiếm chuyến bay.
3. Hệ thống hiển thị danh sách chuyến bay phù hợp.
4. Người dùng chọn chuyến bay mong muốn.
5. Người dùng nhập thông tin liên hệ và thông tin hành khách.
6. Hệ thống hiển thị thông tin tổng hợp của đơn đặt vé.
7. Người dùng xác nhận đặt vé.
8. Hệ thống lưu đơn đặt vé và cập nhật trạng thái.

Wireframe tổng quát của hệ thống có thể được mô tả theo các màn hình chính sau:

#### Trang chủ:
* Thanh điều hướng gồm logo, trang chủ, chuyến bay, đơn đặt vé, đăng nhập và đăng ký.
* Khu vực tìm kiếm chuyến bay gồm điểm đi, điểm đến, ngày đi, ngày về, số lượng hành khách, hạng ghế và nút tìm kiếm.
* Khu vực hiển thị một số tuyến bay hoặc thông tin nổi bật.

#### Trang kết quả tìm kiếm:
* Khu vực bộ lọc theo giá vé, thời gian bay, hạng ghế hoặc điểm đến.
* Danh sách chuyến bay gồm mã chuyến bay, thời gian khởi hành, thời gian đến, thời lượng bay, số ghế còn lại và giá vé.
* Nút chọn chuyến bay để chuyển sang bước đặt vé.

#### Trang đặt vé:
* Hiển thị thông tin chuyến bay đã chọn.
* Form nhập thông tin người liên hệ.
* Form nhập thông tin từng hành khách.
* Khu vực tổng kết chi phí.
* Nút xác nhận đặt vé.

#### Trang quản lý đơn đặt vé:
* Hiển thị danh sách các đơn đặt vé của khách hàng.
* Mỗi đơn gồm mã đơn, ngày đặt, hành trình, tổng tiền và trạng thái.
* Cho phép xem chi tiết vé hoặc hủy đơn nếu điều kiện cho phép.

#### Trang quản trị:
* Thanh menu quản trị gồm các mục quản lý sân bay, tuyến bay, máy bay, chuyến bay, người dùng, đơn đặt vé và khuyến mãi.
* Khu vực bảng dữ liệu dùng để xem danh sách bản ghi.
* Các form thêm, sửa, xóa và tìm kiếm dữ liệu.

### 2.2. Kỹ thuật và công nghệ

Hệ thống được xây dựng theo mô hình client-server. Frontend đóng vai trò là phía giao diện, tiếp nhận thao tác của người dùng và gửi yêu cầu đến backend. Backend chịu trách nhiệm xử lý nghiệp vụ, xác thực người dùng, truy xuất cơ sở dữ liệu và trả kết quả cho frontend. Cơ sở dữ liệu lưu trữ toàn bộ thông tin liên quan đến người dùng, chuyến bay, đặt vé, thanh toán và quản trị hệ thống.

#### Frontend:
* **ReactJS** được sử dụng để xây dựng giao diện người dùng. Đây là thư viện JavaScript phổ biến, hỗ trợ xây dựng giao diện theo hướng component. Việc chia giao diện thành các component giúp mã nguồn dễ quản lý, dễ tái sử dụng và thuận tiện khi mở rộng chức năng.
* **Vite** được sử dụng làm công cụ phát triển frontend. Vite giúp khởi chạy dự án nhanh, hỗ trợ cập nhật giao diện trong quá trình lập trình và phù hợp với các ứng dụng React hiện đại.
* **CSS và Tailwind CSS** được sử dụng để xây dựng bố cục, màu sắc và giao diện người dùng. Các công nghệ này giúp giao diện có tính nhất quán, dễ điều chỉnh và hỗ trợ xây dựng các màn hình có bố cục rõ ràng.

#### Backend:
* **ASP.NET Core** được sử dụng để xây dựng hệ thống API phía server. Công nghệ này phù hợp với các ứng dụng web cần xử lý nghiệp vụ, xác thực, phân quyền và giao tiếp với cơ sở dữ liệu.
* **C#** là ngôn ngữ lập trình chính của backend. Ngôn ngữ này hỗ trợ lập trình hướng đối tượng, xử lý bất đồng bộ và tổ chức mã nguồn rõ ràng.
* **Entity Framework Core** được sử dụng để làm việc với cơ sở dữ liệu theo mô hình ORM. Thay vì viết toàn bộ câu lệnh SQL thủ công, hệ thống có thể thao tác với dữ liệu thông qua các entity, giúp mã nguồn dễ đọc và dễ bảo trì hơn.
* **RESTful API** được áp dụng để thiết kế các endpoint phục vụ frontend. Các API được sử dụng cho những chức năng như đăng nhập, đăng ký, tìm kiếm chuyến bay, đặt vé, quản lý người dùng và quản lý dữ liệu hệ thống.
* **JWT (JSON Web Token)** được sử dụng cho xác thực và phân quyền. Sau khi đăng nhập thành công, người dùng nhận được token để gửi kèm trong các request cần xác thực.

#### Cơ sở dữ liệu:
* **PostgreSQL** được sử dụng làm hệ quản trị cơ sở dữ liệu quan hệ. Công nghệ này phù hợp với bài toán đặt vé máy bay vì dữ liệu trong hệ thống có nhiều quan hệ giữa các bảng như người dùng, chuyến bay, đơn đặt vé, hành khách, vé và thanh toán.

#### Công cụ hỗ trợ:
* **Docker** được sử dụng để đóng gói và chạy các thành phần của hệ thống trong container, giúp môi trường chạy chương trình ổn định hơn giữa các máy khác nhau.
* **Docker Compose** được sử dụng để khởi động nhiều dịch vụ cùng lúc, bao gồm backend API, PostgreSQL, Redis và pgAdmin. Điều này giúp quá trình chạy thử và triển khai hệ thống thuận tiện hơn.
* **Redis** có thể được sử dụng làm bộ nhớ đệm hoặc hỗ trợ các tác vụ cần truy xuất nhanh. 
* **pgAdmin** được sử dụng để quản lý và kiểm tra dữ liệu PostgreSQL thông qua giao diện trực quan.
* **Git** được sử dụng để quản lý mã nguồn, theo dõi lịch sử thay đổi và hỗ trợ quá trình làm việc nhóm.

---

## 3. PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG

### 3.1. Phát biểu yêu cầu

Hệ thống website đặt vé máy bay được xây dựng nhằm hỗ trợ khách hàng thực hiện quá trình tìm kiếm và đặt vé trực tuyến, đồng thời hỗ trợ quản trị viên quản lý các dữ liệu cần thiết cho hoạt động vận hành hệ thống. Các yêu cầu của hệ thống được xác định dựa trên hai nhóm người dùng chính là khách hàng và quản trị viên.

Đối với khách hàng, hệ thống cần cho phép người dùng tạo tài khoản, đăng nhập, tìm kiếm chuyến bay, lựa chọn chuyến bay phù hợp, nhập thông tin hành khách, đặt vé và theo dõi trạng thái đơn đặt vé. Đối với quản trị viên, hệ thống cần hỗ trợ quản lý các dữ liệu như sân bay, tuyến bay, máy bay, chuyến bay, hạng ghế, số lượng ghế, người dùng, đơn đặt vé, thanh toán và khuyến mãi.

Các yêu cầu đầu vào và đầu ra của một số chức năng chính được mô tả như sau:

#### Chức năng đăng ký tài khoản:
* **Input:** Họ tên, email, số điện thoại, mật khẩu và các thông tin cá nhân cần thiết.
* **Output:** Tài khoản người dùng được tạo mới hoặc thông báo lỗi nếu dữ liệu không hợp lệ, email đã tồn tại hoặc mật khẩu không đáp ứng yêu cầu.

#### Chức năng đăng nhập:
* **Input:** Email hoặc tên đăng nhập và mật khẩu.
* **Output:** Thông tin người dùng, token xác thực và quyền truy cập nếu đăng nhập thành công; thông báo lỗi nếu thông tin đăng nhập không chính xác.

#### Chức năng tìm kiếm chuyến bay:
* **Input:** Điểm đi, điểm đến, ngày khởi hành, ngày về nếu có, số lượng hành khách và hạng ghế.
* **Output:** Danh sách chuyến bay phù hợp, bao gồm mã chuyến bay, thời gian khởi hành, thời gian đến, thời lượng bay, hạng ghế, số ghế còn lại và giá vé.

#### Chức năng xem chi tiết chuyến bay:
* **Input:** Mã chuyến bay hoặc định danh chuyến bay.
* **Output:** Thông tin chi tiết của chuyến bay, bao gồm tuyến bay, sân bay đi, sân bay đến, thời gian bay, máy bay khai thác, hạng ghế, giá vé và số lượng ghế còn lại.

#### Chức năng đặt vé:
* **Input:** Thông tin chuyến bay được chọn, thông tin người liên hệ, danh sách hành khách, hạng ghế, số lượng vé và thông tin thanh toán.
* **Output:** Đơn đặt vé được tạo, mã đặt vé, tổng tiền, trạng thái đơn đặt vé và thông tin vé tương ứng.

#### Chức năng quản lý đơn đặt vé:
* **Input:** Mã người dùng, mã đơn đặt vé hoặc điều kiện lọc theo trạng thái.
* **Output:** Danh sách đơn đặt vé, chi tiết đơn đặt vé, trạng thái thanh toán, thông tin hành khách và thông tin vé.

#### Chức năng quản lý chuyến bay:
* **Input:** Mã chuyến bay, tuyến bay, máy bay, thời gian khởi hành, thời gian đến, trạng thái chuyến bay, hạng ghế và số lượng ghế.
* **Output:** Chuyến bay được thêm mới, cập nhật hoặc xóa; danh sách chuyến bay sau khi xử lý.

#### Chức năng quản lý sân bay và tuyến bay:
* **Input:** Thông tin sân bay như mã sân bay, tên sân bay, thành phố, quốc gia; thông tin tuyến bay như sân bay đi và sân bay đến.
* **Output:** Danh sách sân bay, tuyến bay được thêm mới, cập nhật hoặc xóa theo thao tác của quản trị viên.

#### Chức năng quản lý thanh toán:
* **Input:** Mã đơn đặt vé, số tiền thanh toán, phương thức thanh toán và thông tin giao dịch.
* **Output:** Trạng thái thanh toán được cập nhật, thông tin giao dịch được lưu lại và đơn đặt vé được chuyển sang trạng thái phù hợp.

#### Chức năng quản lý khuyến mãi:
* **Input:** Mã khuyến mãi, tên chương trình, giá trị giảm giá, thời gian áp dụng, số lượng sử dụng và trạng thái hoạt động.
* **Output:** Khuyến mãi được tạo mới, cập nhật, vô hiệu hóa hoặc áp dụng vào đơn đặt vé nếu thỏa điều kiện.

### 3.2. Các biểu đồ thiết kế

Trong quá trình phân tích và thiết kế hệ thống, các biểu đồ thiết kế được sử dụng nhằm mô tả chức năng, luồng xử lý và mối quan hệ giữa các thành phần trong hệ thống. Các biểu đồ chính gồm biểu đồ use case, biểu đồ hoạt động và biểu đồ tuần tự.

#### 3.2.1. Biểu đồ use case tổng quát

Biểu đồ use case tổng quát mô tả các tác nhân và chức năng chính của hệ thống. Hệ thống gồm hai tác nhân chính:
* Khách hàng.
* Quản trị viên.

**Các use case của khách hàng:**
* Đăng ký tài khoản.
* Đăng nhập.
* Tìm kiếm chuyến bay.
* Xem chi tiết chuyến bay.
* Đặt vé.
* Thanh toán.
* Xem lịch sử đặt vé.
* Xem chi tiết vé.
* Hủy đơn đặt vé nếu điều kiện cho phép.

**Các use case của quản trị viên:**
* Đăng nhập hệ thống quản trị.
* Quản lý người dùng.
* Quản lý sân bay.
* Quản lý tuyến bay.
* Quản lý máy bay.
* Quản lý chuyến bay.
* Quản lý hạng ghế và số lượng ghế.
* Quản lý đơn đặt vé.
* Quản lý thanh toán.
* Quản lý khuyến mãi.

*Mô tả tổng quát biểu đồ use case:*  
Khách hàng tương tác với hệ thống thông qua các chức năng phục vụ quá trình đặt vé. Quản trị viên tương tác với hệ thống thông qua các chức năng quản lý dữ liệu. Cả hai nhóm người dùng đều cần thực hiện đăng nhập đối với các chức năng yêu cầu xác thực.

#### 3.2.2. Biểu đồ hoạt động chức năng đặt vé

Biểu đồ hoạt động của chức năng đặt vé mô tả luồng xử lý từ khi người dùng tìm kiếm chuyến bay đến khi đơn đặt vé được tạo. Luồng xử lý chính gồm các bước:
1. Người dùng nhập thông tin tìm kiếm chuyến bay.
2. Hệ thống kiểm tra dữ liệu đầu vào.
3. Hệ thống truy vấn danh sách chuyến bay phù hợp.
4. Người dùng chọn chuyến bay.
5. Hệ thống hiển thị thông tin chuyến bay và giá vé.
6. Người dùng nhập thông tin người liên hệ và hành khách.
7. Hệ thống kiểm tra tính hợp lệ của thông tin.
8. Hệ thống kiểm tra số lượng ghế còn lại.
9. Hệ thống tạo đơn đặt vé.
10. Hệ thống cập nhật số lượng ghế tạm giữ hoặc đã bán tùy theo trạng thái thanh toán.
11. Hệ thống trả về mã đặt vé và trạng thái đơn đặt vé.

*Trường hợp ngoại lệ:*
* Nếu không tìm thấy chuyến bay phù hợp, hệ thống hiển thị thông báo không có kết quả.
* Nếu thông tin hành khách không hợp lệ, hệ thống yêu cầu người dùng kiểm tra lại.
* Nếu số lượng ghế không đủ, hệ thống thông báo không thể đặt vé.
* Nếu thanh toán thất bại, đơn đặt vé được giữ ở trạng thái chờ thanh toán hoặc bị hủy tùy theo quy định xử lý.

#### 3.2.3. Biểu đồ tuần tự chức năng tìm kiếm chuyến bay

Biểu đồ tuần tự chức năng tìm kiếm chuyến bay mô tả sự tương tác giữa người dùng, giao diện frontend, backend API và cơ sở dữ liệu.

*Luồng xử lý:*
1. Người dùng nhập điều kiện tìm kiếm trên giao diện.
2. Frontend gửi request tìm kiếm chuyến bay đến backend API.
3. Backend kiểm tra dữ liệu đầu vào.
4. Backend gọi service xử lý tìm kiếm chuyến bay.
5. Service truy vấn repository để lấy dữ liệu từ cơ sở dữ liệu.
6. Cơ sở dữ liệu trả về danh sách chuyến bay phù hợp.
7. Backend chuyển đổi dữ liệu sang DTO và trả response cho frontend.
8. Frontend hiển thị danh sách chuyến bay cho người dùng.

#### 3.2.4. Biểu đồ tuần tự chức năng đặt vé

Biểu đồ tuần tự chức năng đặt vé mô tả quá trình phối hợp giữa các thành phần khi người dùng thực hiện đặt vé.

*Luồng xử lý:*
1. Người dùng chọn chuyến bay và nhập thông tin hành khách.
2. Frontend gửi request tạo đơn đặt vé đến backend API.
3. Backend xác thực người dùng.
4. Backend kiểm tra dữ liệu đặt vé.
5. BookingService kiểm tra chuyến bay, hạng ghế và số lượng ghế còn lại.
6. BookingService tính tổng tiền của đơn đặt vé.
7. Repository lưu thông tin đơn đặt vé, hành khách và vé vào cơ sở dữ liệu.
8. Hệ thống cập nhật số lượng ghế tương ứng.
9. Backend trả về thông tin đơn đặt vé cho frontend.
10. Frontend hiển thị kết quả đặt vé cho người dùng.

### 3.3. Biểu đồ cơ sở dữ liệu quan hệ

Cơ sở dữ liệu của hệ thống được thiết kế theo mô hình quan hệ. Các bảng dữ liệu được xây dựng nhằm lưu trữ thông tin người dùng, sân bay, tuyến bay, máy bay, chuyến bay, hạng ghế, đặt vé, hành khách, vé, thanh toán và khuyến mãi. Các bảng có quan hệ với nhau thông qua khóa chính và khóa ngoại, giúp đảm bảo tính toàn vẹn dữ liệu trong quá trình vận hành hệ thống.

#### 3.3.1. Danh sách các bảng chính

##### Bảng Users:
* **Chức năng:** Lưu thông tin tài khoản người dùng.
* **Các trường dữ liệu chính:** Id, FullName, Email, Phone, PasswordHash, Role, CreatedAt, UpdatedAt, IsDeleted.
* **Khóa chính:** Id.
* **Quan hệ:** Một người dùng có thể có nhiều đơn đặt vé.

##### Bảng Airports:
* **Chức năng:** Lưu thông tin sân bay.
* **Các trường dữ liệu chính:** Id, Code, Name, City, Country, CreatedAt, UpdatedAt.
* **Khóa chính:** Id.
* **Quan hệ:** Một sân bay có thể tham gia nhiều tuyến bay với vai trò sân bay đi hoặc sân bay đến.

##### Bảng Routes:
* **Chức năng:** Lưu thông tin tuyến bay giữa hai sân bay.
* **Các trường dữ liệu chính:** Id, Code, DepartureAirportId, ArrivalAirportId, Distance, CreatedAt, UpdatedAt.
* **Khóa chính:** Id.
* **Khóa ngoại:** DepartureAirportId, ArrivalAirportId tham chiếu đến Airports.
* **Quan hệ:** Một tuyến bay có thể có nhiều chuyến bay.

##### Bảng Aircrafts:
* **Chức năng:** Lưu thông tin máy bay.
* **Các trường dữ liệu chính:** Id, Code, Model, Manufacturer, TotalSeats, CreatedAt, UpdatedAt.
* **Khóa chính:** Id.
* **Quan hệ:** Một máy bay có thể được sử dụng cho nhiều chuyến bay.

##### Bảng SeatClasses:
* **Chức năng:** Lưu thông tin hạng ghế.
* **Các trường dữ liệu chính:** Id, Code, Name, Priority, RefundPercent, ChangeFee.
* **Khóa chính:** Id.
* **Quan hệ:** Một hạng ghế có thể xuất hiện trong nhiều bản ghi tồn kho ghế của chuyến bay.

##### Bảng Flights:
* **Chức năng:** Lưu thông tin chuyến bay cụ thể.
* **Các trường dữ liệu chính:** Id, FlightNumber, RouteId, AircraftId, DepartureTime, ArrivalTime, ArrivalOffsetDays, Status, CreatedAt, UpdatedAt.
* **Khóa chính:** Id.
* **Khóa ngoại:** RouteId tham chiếu đến Routes, AircraftId tham chiếu đến Aircrafts.
* **Quan hệ:** Một chuyến bay có nhiều bản ghi số lượng ghế và có thể xuất hiện trong nhiều đơn đặt vé.

##### Bảng FlightSeatInventories:
* **Chức năng:** Lưu thông tin số lượng ghế và giá vé theo từng chuyến bay và hạng ghế.
* **Các trường dữ liệu chính:** Id, FlightId, SeatClassId, TotalSeats, AvailableSeats, HeldSeats, SoldSeats, BasePrice, CurrentPrice.
* **Khóa chính:** Id.
* **Khóa ngoại:** FlightId tham chiếu đến Flights, SeatClassId tham chiếu đến SeatClasses.
* **Quan hệ:** Một bản ghi tồn kho ghế có thể được nhiều hành khách đặt trong các đơn khác nhau.

##### Bảng Bookings:
* **Chức năng:** Lưu thông tin đơn đặt vé.
* **Các trường dữ liệu chính:** Id, BookingCode, UserId, OutboundFlightId, ReturnFlightId, TripType, TotalAmount, DiscountAmount, FinalAmount, Currency, Status, ContactEmail, ContactPhone, ExpiresAt, CreatedAt.
* **Khóa chính:** Id.
* **Khóa ngoại:** UserId tham chiếu đến Users, OutboundFlightId và ReturnFlightId tham chiếu đến Flights.
* **Quan hệ:** Một đơn đặt vé có nhiều hành khách, nhiều vé và có thể có thông tin thanh toán.

##### Bảng BookingPassengers:
* **Chức năng:** Lưu thông tin hành khách trong đơn đặt vé.
* **Các trường dữ liệu chính:** Id, BookingId, FirstName, LastName, FullName, Gender, DateOfBirth, Nationality, PassportNumber, NationalId, PassengerType, FlightSeatInventoryId, FareSnapshot, DocumentCheckStatus.
* **Khóa chính:** Id.
* **Khóa ngoại:** BookingId tham chiếu đến Bookings, FlightSeatInventoryId tham chiếu đến FlightSeatInventories.
* **Quan hệ:** Một hành khách thuộc một đơn đặt vé.

##### Bảng Tickets:
* **Chức năng:** Lưu thông tin vé được phát sinh từ đơn đặt vé.
* **Các trường dữ liệu chính:** Id, TicketNumber, BookingId, PassengerId, FlightId, SeatClassId, Status, IssuedAt.
* **Khóa chính:** Id.
* **Khóa ngoại:** BookingId tham chiếu đến Bookings, PassengerId tham chiếu đến BookingPassengers, FlightId tham chiếu đến Flights, SeatClassId tham chiếu đến SeatClasses.
* **Quan hệ:** Mỗi vé gắn với một hành khách và một chuyến bay.

##### Bảng Payments:
* **Chức năng:** Lưu thông tin thanh toán của đơn đặt vé.
* **Các trường dữ liệu chính:** Id, BookingId, Amount, PaymentMethod, TransactionId, Status, PaidAt, PaymentGatewayResponse.
* **Khóa chính:** Id.
* **Khóa ngoại:** BookingId tham chiếu đến Bookings.
* **Quan hệ:** Một đơn đặt vé có thể có một hoặc nhiều bản ghi thanh toán tùy theo cách xử lý giao dịch.

##### Bảng Promotions:
* **Chức năng:** Lưu thông tin chương trình khuyến mãi.
* **Các trường dữ liệu chính:** Id, Code, Name, Description, DiscountType, DiscountValue, StartDate, EndDate, UsageLimit, UsedCount, IsActive.
* **Khóa chính:** Id.
* **Quan hệ:** Khuyến mãi có thể được áp dụng cho nhiều đơn đặt vé nếu thỏa điều kiện.

##### Bảng PromotionUsages:
* **Chức năng:** Lưu lịch sử sử dụng khuyến mãi.
* **Các trường dữ liệu chính:** Id, PromotionId, BookingId, UserId, DiscountAmount, UsedAt.
* **Khóa chính:** Id.
* **Khóa ngoại:** PromotionId tham chiếu đến Promotions, BookingId tham chiếu đến Bookings, UserId tham chiếu đến Users.

#### 3.3.2. Biểu đồ tổng thể cơ sở dữ liệu

Biểu đồ cơ sở dữ liệu tổng thể có thể mô tả bằng các quan hệ chính sau:
* Users 1 - n Bookings
* Airports 1 - n Routes với vai trò sân bay đi
* Airports 1 - n Routes với vai trò sân bay đến
* Routes 1 - n Flights
* Aircrafts 1 - n Flights
* Flights 1 - n FlightSeatInventories
* SeatClasses 1 - n FlightSeatInventories
* Bookings 1 - n BookingPassengers
* Bookings 1 - n Tickets
* Bookings 1 - n Payments
* BookingPassengers 1 - 1 hoặc 1 - n Tickets tùy theo hành trình một chiều hoặc khứ hồi
* Promotions 1 - n PromotionUsages
* Users 1 - n PromotionUsages
* Bookings 1 - n PromotionUsages

*Mô tả tổng thể:*  
Người dùng tạo đơn đặt vé thông qua bảng Bookings. Mỗi đơn đặt vé gắn với một hoặc nhiều hành khách trong bảng BookingPassengers. Các hành khách được liên kết với hạng ghế và tồn kho ghế thông qua bảng FlightSeatInventories. Chuyến bay trong bảng Flights được xác định bởi tuyến bay và máy bay. Tuyến bay được tạo từ hai sân bay trong bảng Airports. Khi thanh toán được thực hiện, thông tin giao dịch được lưu trong bảng Payments. Nếu đơn đặt vé sử dụng khuyến mãi, thông tin sử dụng được ghi nhận trong bảng PromotionUsages.

### 3.4. Kiến trúc hệ thống

Hệ thống website đặt vé máy bay được thiết kế theo kiến trúc client-server, gồm ba lớp chính: lớp giao diện người dùng, lớp xử lý nghiệp vụ và lớp dữ liệu.

#### Lớp giao diện người dùng:
Lớp này được xây dựng bằng ReactJS và chạy trên trình duyệt của người dùng. Giao diện có nhiệm vụ hiển thị các màn hình như trang chủ, trang tìm kiếm chuyến bay, trang đặt vé, trang quản lý đơn đặt vé và trang quản trị. Khi người dùng thao tác trên giao diện, frontend sẽ gửi request đến backend thông qua các API.

#### Lớp xử lý nghiệp vụ:
Lớp này được xây dựng bằng ASP.NET Core. Backend tiếp nhận request từ frontend, kiểm tra dữ liệu đầu vào, xác thực người dùng, xử lý nghiệp vụ và truy xuất cơ sở dữ liệu. Các thành phần chính trong backend gồm:
* Controller: Tiếp nhận request và trả response.
* Service: Xử lý nghiệp vụ chính của hệ thống.
* Repository: Thực hiện truy xuất dữ liệu.
* DTO: Truyền dữ liệu giữa frontend và backend.
* Entity: Đại diện cho các bảng dữ liệu trong cơ sở dữ liệu.

#### Lớp dữ liệu:
Lớp dữ liệu sử dụng PostgreSQL để lưu trữ thông tin của hệ thống. Các dữ liệu được quản lý gồm người dùng, sân bay, tuyến bay, máy bay, chuyến bay, hạng ghế, tồn kho ghế, đơn đặt vé, hành khách, vé, thanh toán và khuyến mãi. Entity Framework Core được sử dụng để kết nối backend với cơ sở dữ liệu và hỗ trợ thao tác dữ liệu thông qua các entity.

*Bức tranh tổng thể của hệ thống được mô tả như sau:*
1. Người dùng thao tác trên giao diện website.
2. Frontend gửi HTTP request đến backend API.
3. Backend kiểm tra request, xác thực người dùng và gọi các service tương ứng.
4. Service xử lý nghiệp vụ, ví dụ tìm kiếm chuyến bay, tạo đơn đặt vé hoặc cập nhật trạng thái thanh toán.
5. Repository truy xuất hoặc cập nhật dữ liệu trong PostgreSQL.
6. PostgreSQL trả dữ liệu về backend.
7. Backend chuyển đổi dữ liệu sang DTO và trả response cho frontend.
8. Frontend hiển thị kết quả cho người dùng.

Ngoài các thành phần chính, hệ thống còn sử dụng một số công cụ hỗ trợ:
* Docker và Docker Compose dùng để chạy backend, cơ sở dữ liệu và các dịch vụ liên quan trong môi trường container.
* Redis có thể được sử dụng để hỗ trợ lưu trữ tạm hoặc tăng tốc truy xuất dữ liệu.
* pgAdmin hỗ trợ quản trị và kiểm tra dữ liệu PostgreSQL.

Kiến trúc này giúp hệ thống có sự tách biệt rõ ràng giữa giao diện, xử lý nghiệp vụ và dữ liệu. Nhờ đó, hệ thống dễ bảo trì, dễ kiểm thử và thuận lợi cho việc mở rộng các chức năng trong tương lai.

---

## 5. KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

### 5.1. Kết luận

Đề tài “Xây dựng website đặt vé máy bay” đã tập trung giải quyết bài toán đặt vé máy bay trực tuyến ở mức đồ án môn học. Hệ thống được xây dựng với các chức năng chính phục vụ hai nhóm người dùng là khách hàng và quản trị viên. Đối với khách hàng, hệ thống hỗ trợ các chức năng như đăng ký, đăng nhập, tra cứu chuyến bay, đặt vé, thanh toán trực tuyến, quản lý đơn đặt vé, xem vé và gửi yêu cầu hoàn tiền. Đối với quản trị viên, hệ thống hỗ trợ quản lý dữ liệu nền như sân bay, tuyến bay, máy bay, chuyến bay, lịch bay, người dùng, đơn đặt vé, khuyến mãi và báo cáo thống kê.

Về mặt phân tích và thiết kế, đề tài đã xác định được các yêu cầu chính của hệ thống, xây dựng các nhóm use case, mô tả quy trình xử lý nghiệp vụ và thiết kế cơ sở dữ liệu quan hệ phù hợp với bài toán đặt vé máy bay. Các bảng dữ liệu như Users, Airports, Routes, Aircrafts, Flights, FlightSeatInventories, Bookings, BookingPassengers, Tickets, Payments và Promotions phản ánh được các đối tượng quan trọng trong hệ thống. Mối quan hệ giữa các bảng giúp đảm bảo việc lưu trữ thông tin chuyến bay, hành khách, đơn đặt vé và thanh toán có tính liên kết và nhất quán.

Về mặt cài đặt, hệ thống được xây dựng theo mô hình client-server. Frontend sử dụng ReactJS để xây dựng giao diện người dùng, backend sử dụng ASP.NET Core để xử lý nghiệp vụ và cung cấp API, cơ sở dữ liệu sử dụng PostgreSQL để lưu trữ dữ liệu. Ngoài ra, Docker Compose được sử dụng để hỗ trợ chạy các thành phần của hệ thống trong môi trường phát triển. Cách tổ chức này giúp hệ thống có cấu trúc rõ ràng, tách biệt giữa giao diện, xử lý nghiệp vụ và lưu trữ dữ liệu.

Thông qua quá trình thực hiện đề tài, nhóm đã vận dụng được nhiều kiến thức của môn Công nghệ phần mềm như khảo sát yêu cầu, phân tích chức năng, thiết kế cơ sở dữ liệu, thiết kế kiến trúc hệ thống, xây dựng API, kiểm thử chức năng và triển khai thử nghiệm. Đề tài cũng giúp nhóm hiểu rõ hơn về quy trình nghiệp vụ của một hệ thống đặt vé máy bay, đặc biệt là các vấn đề liên quan đến tìm kiếm chuyến bay, quản lý số lượng ghế, tạo đơn đặt vé, xử lý thanh toán và quản lý trạng thái đơn.

Tuy nhiên, do giới hạn về thời gian và phạm vi của đồ án, hệ thống vẫn còn một số hạn chế. Việc thanh toán mới dừng ở mức tích hợp hoặc mô phỏng theo luồng thanh toán trực tuyến, chưa xử lý đầy đủ mọi tình huống phát sinh như giao dịch bị treo, hoàn tiền tự động hoặc đối soát giao dịch. Dữ liệu chuyến bay trong hệ thống chủ yếu phục vụ mục đích thử nghiệm, chưa kết nối với dữ liệu thực tế từ các hãng hàng không. Một số chức năng nâng cao như chọn ghế trực quan, gửi thông báo thời gian thực, tối ưu tìm kiếm chuyến bay và phân tích doanh thu chuyên sâu vẫn có thể được cải thiện trong các phiên bản tiếp theo.

Nhìn chung, hệ thống đã đáp ứng được mục tiêu ban đầu là xây dựng một website đặt vé máy bay có các chức năng cơ bản, thể hiện được quy trình nghiệp vụ chính và có khả năng mở rộng. Đây là cơ sở để tiếp tục phát triển hệ thống theo hướng hoàn thiện hơn, gần với một sản phẩm thực tế hơn.

### 5.2. Hướng phát triển

Trong tương lai, hệ thống có thể được phát triển theo các hướng sau:

1. **Hoàn thiện chức năng thanh toán trực tuyến.** Hệ thống cần xử lý đầy đủ hơn các trạng thái giao dịch như thanh toán thành công, thất bại, đang chờ xử lý, quá hạn thanh toán và hoàn tiền. Ngoài ra, có thể bổ sung cơ chế đối soát giao dịch với cổng thanh toán để đảm bảo trạng thái thanh toán trong hệ thống luôn chính xác.
2. **Phát triển chức năng chọn ghế trực quan.** Hiện tại, hệ thống quản lý số lượng ghế theo chuyến bay và hạng ghế. Trong các phiên bản tiếp theo, có thể xây dựng sơ đồ ghế của từng máy bay, cho phép khách hàng chọn vị trí ghế cụ thể khi đặt vé. Chức năng này sẽ giúp trải nghiệm người dùng thực tế hơn và phù hợp với các hệ thống đặt vé hiện đại.
3. **Nâng cấp chức năng tìm kiếm và lọc chuyến bay.** Hệ thống có thể bổ sung các tiêu chí lọc nâng cao như khoảng giá, giờ khởi hành, thời lượng bay, loại hành trình, hạng ghế, số điểm dừng và hãng khai thác. Đồng thời, có thể tối ưu tốc độ truy vấn để việc tìm kiếm chuyến bay nhanh hơn khi dữ liệu tăng lên.
4. **Hoàn thiện quy trình hoàn vé và đổi vé.** Hệ thống có thể bổ sung chính sách hoàn hủy linh hoạt theo từng hạng vé, từng thời điểm trước giờ bay và từng chương trình khuyến mãi. Khi khách hàng gửi yêu cầu hoàn tiền hoặc đổi vé, hệ thống cần tự động tính phí, số tiền được hoàn và cập nhật lại tồn kho ghế.
5. **Bổ sung hệ thống thông báo.** Hệ thống có thể gửi email hoặc thông báo trong ứng dụng khi người dùng đặt vé thành công, thanh toán thành công, chuyến bay bị thay đổi, đơn bị hủy hoặc yêu cầu hoàn tiền được xử lý. Chức năng này giúp tăng tính tương tác và giúp khách hàng theo dõi thông tin kịp thời.
6. **Phát triển báo cáo và thống kê cho quản trị viên.** Ngoài các thống kê cơ bản, hệ thống có thể bổ sung biểu đồ doanh thu theo ngày, tháng, tuyến bay, hạng ghế; thống kê tỷ lệ hủy vé, tỷ lệ hoàn tiền, số lượng vé bán ra và hiệu quả của từng chương trình khuyến mãi. Các báo cáo này giúp quản trị viên có thêm dữ liệu để đánh giá hoạt động của hệ thống.
7. **Tăng cường bảo mật hệ thống.** Hệ thống có thể bổ sung xác thực hai lớp, giới hạn số lần đăng nhập sai, phân quyền chi tiết hơn cho từng nhóm quản trị viên và ghi log các thao tác quan trọng. Các thông tin nhạy cảm như mật khẩu, token và dữ liệu thanh toán cần được bảo vệ chặt chẽ hơn khi triển khai trong môi trường thực tế.
8. **Cải thiện khả năng triển khai và vận hành.** Hệ thống có thể được triển khai lên máy chủ thật hoặc nền tảng cloud, bổ sung cơ chế sao lưu cơ sở dữ liệu, giám sát log, giám sát hiệu năng và cảnh báo lỗi. Điều này giúp hệ thống ổn định hơn khi có nhiều người dùng truy cập.
9. **Kết nối với dữ liệu chuyến bay thực tế.** Nếu có điều kiện, hệ thống có thể tích hợp API từ các hãng hàng không hoặc nhà cung cấp dữ liệu chuyến bay để cập nhật lịch bay, giá vé và trạng thái chuyến bay theo thời gian thực. Đây là hướng phát triển quan trọng để hệ thống tiến gần hơn đến một sản phẩm có khả năng sử dụng trong thực tế.
