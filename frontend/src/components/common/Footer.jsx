export default function Footer() {
  return (
    <footer className="mt-auto border-t border-slate-200 bg-slate-50 py-8 px-4 text-center text-slate-500">
      <div className="mx-auto max-w-7xl px-4 md:px-8">
        <div className="grid grid-cols-1 gap-8 md:grid-cols-3 text-left mb-8">
          <div>
            <span className="title-font block text-lg font-extrabold tracking-tight text-brand-primary mb-3">
              FlyNow<span className="text-brand-tertiary">.vn</span>
            </span>
            <p className="text-xs text-slate-400 leading-relaxed">
              Trải nghiệm dịch vụ hàng không thế hệ mới. Đặt vé nhanh chóng, tiện lợi, thanh toán linh hoạt và an toàn tuyệt đối.
            </p>
          </div>
          <div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-800 mb-3">Liên hệ hỗ trợ</h3>
            <ul className="text-xs space-y-2 text-slate-500">
              <li>📞 Tổng đài 24/7: <span className="font-semibold text-slate-700">1900 6000</span></li>
              <li>📧 Email hỗ trợ: <span className="font-semibold text-slate-700">support@flynow.vn</span></li>
              <li>🏢 Trụ sở chính: Quận 1, Thành phố Hồ Chí Minh</li>
            </ul>
          </div>
          <div>
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-800 mb-3">Thông tin pháp lý</h3>
            <ul className="text-xs space-y-2">
              <li><a href="#terms" className="hover:text-brand-primary transition">Điều khoản sử dụng</a></li>
              <li><a href="#privacy" className="hover:text-brand-primary transition">Chính sách bảo mật</a></li>
              <li><a href="#regulations" className="hover:text-brand-primary transition">Quy lệ vận chuyển</a></li>
            </ul>
          </div>
        </div>
        
        <div className="border-t border-slate-200/80 pt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-xs">
          <p>© {new Date().getFullYear()} FlyNow.vn. Bảo lưu mọi quyền.</p>
          <div className="flex gap-4">
            <span className="text-slate-400">Thiết kế bởi Đội ngũ Công nghệ hàng không</span>
          </div>
        </div>
      </div>
    </footer>
  )
}
