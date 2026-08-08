<template>
  <div class="landing-container">
    <!-- Navbar -->
    <header class="navbar">
      <div class="nav-content">
        <div class="logo">
          <experiment-outlined class="logo-icon" />
          <span class="logo-text">LabManagement</span>
        </div>
        <div class="nav-actions">
          <a-button type="primary" size="large" class="login-btn btn-primary" @click="$router.push('/login')">
            Đăng nhập <arrow-right-outlined />
          </a-button>
        </div>
      </div>
    </header>

    <!-- Hero Section (Cream) -->
    <section class="hero-section">
      <div class="hero-container">
        <div class="hero-content">
          <div class="badge">Nền tảng LabManagement 2.0</div>
          <h1 class="hero-title">Chuyển đổi số toàn diện <br><span class="highlight">không gian phòng lab</span></h1>
          <p class="hero-subtitle">Nền tảng tiên phong giúp tối ưu hóa quản lý thiết bị, kiểm soát quy trình mượn trả và tự động hóa bảo trì. Đưa phòng thí nghiệm của bạn vận hành chuyên nghiệp, thông minh và hiệu quả hơn bao giờ hết.</p>
          <div class="hero-cta">
            <a-button type="primary" size="large" class="cta-btn btn-primary primary-cta" @click="$router.push('/login')">
              Vào hệ thống
            </a-button>
            <a-button size="large" class="cta-btn secondary-cta" @click="scrollToFeatures">
              Xem tính năng
            </a-button>
          </div>
        </div>
        <div class="hero-illustration">
          <img src="/hero.png" alt="Lab Management Dashboard" class="floating-img" />
        </div>
      </div>
    </section>

    <!-- Statistics Section (Dark Navy) -->
    <section class="stats-section">
      <div class="stats-grid">
        <div class="stat-item">
          <h2>500+</h2>
          <p>Tài sản quản lý</p>
        </div>
        <div class="stat-item">
          <h2>1.200+</h2>
          <p>Lượt mượn trả</p>
        </div>
        <div class="stat-item">
          <h2>100%</h2>
          <p>Minh bạch trạng thái</p>
        </div>
        <div class="stat-item">
          <h2>24/7</h2>
          <p>Nhắc hạn tự động</p>
        </div>
      </div>
    </section>

    <!-- Features Section (Cream) -->
    <section class="features-section" id="features">
      <div class="section-header">
        <h2 class="serif-title">Hệ sinh thái quản lý toàn diện</h2>
        <p>Được thiết kế chuyên biệt cho môi trường học thuật và nghiên cứu, giúp tự động hóa và số hóa toàn bộ các quy trình nghiệp vụ thủ công.</p>
      </div>
      
      <div class="features-grid">
        <div class="feature-card">
          <div class="feature-icon-wrapper">
            <desktop-outlined class="feature-icon" />
          </div>
          <h3>Kiểm soát tài sản thông minh</h3>
          <p>Lưu trữ chi tiết thông số kỹ thuật, vị trí lưu trữ, lịch sử vòng đời thiết bị và tự động cảnh báo khi đến hạn bảo hành.</p>
        </div>
        
        <div class="feature-card">
          <div class="feature-icon-wrapper">
            <history-outlined class="feature-icon" />
          </div>
          <h3>Quy trình mượn trả khép kín</h3>
          <p>Tích hợp phê duyệt đa tầng, quét mã QR nhận diện thiết bị siêu tốc và hệ thống nhắc nhở lịch trả hoàn toàn tự động.</p>
        </div>
        
        <div class="feature-card">
          <div class="feature-icon-wrapper">
            <tool-outlined class="feature-icon" />
          </div>
          <h3>Đánh giá & Bảo trì tự động</h3>
          <p>Ghi nhận chính xác tình trạng thiết bị sau mỗi lần sử dụng. Phân luồng xử lý sửa chữa, bảo hành minh bạch và rõ ràng.</p>
        </div>
      </div>
    </section>

    <!-- Split Section (Cream) -->
    <section class="split-section">
      <div class="split-container">
        <div class="split-image">
          <img src="/feature.png" alt="Lab Equipment Interface" class="shadow-img" />
        </div>
        <div class="split-content">
          <h2 class="serif-title">Quản trị dữ liệu tập trung, quyết định chính xác</h2>
          <p class="split-desc">Triển khai hệ thống phân loại linh hoạt từ thiết bị IoT, AI đến vật tư tiêu hao. Mọi lịch sử tác động đều được số hóa, lưu vết hoàn chỉnh và minh bạch tuyệt đối.</p>
          
          <ul class="benefit-list">
            <li>
              <check-circle-filled class="check-icon" />
              <div>
                <strong>Cấu trúc linh hoạt.</strong> Tùy biến không gian lưu trữ và phân nhóm tài sản theo đặc thù của từng phòng nghiên cứu.
              </div>
            </li>
            <li>
              <check-circle-filled class="check-icon" />
              <div>
                <strong>Phân quyền đa lớp.</strong> Phân bổ quyền hạn chi tiết từ Ban quản lý đến Sinh viên, đảm bảo tính toàn vẹn của dữ liệu.
              </div>
            </li>
            <li>
              <check-circle-filled class="check-icon" />
              <div>
                <strong>Quy trình chuẩn hóa.</strong> Giám sát chặt chẽ vòng đời thiết bị từ lúc nhập kho đến khi thanh lý, tối đa hóa tuổi thọ tài sản.
              </div>
            </li>
          </ul>
        </div>
      </div>
    </section>

    <!-- CTA Band Coral -->
    <section class="cta-band-coral">
      <div class="cta-band-content">
        <h2 class="serif-title">Sẵn sàng để tối ưu hóa phòng lab của bạn?</h2>
        <p>Bắt đầu trải nghiệm nền tảng quản lý chuyên nghiệp và hiệu quả ngay hôm nay.</p>
        <a-button size="large" class="cta-btn btn-cream" @click="$router.push('/login')">
          Trải nghiệm ngay
        </a-button>
      </div>
    </section>

    <!-- Footer (Dark Navy) -->
    <footer class="footer">
      <div class="footer-content">
        <div class="footer-brand">
          <experiment-outlined class="footer-logo" />
          <span>LabManagement</span>
        </div>
        <div class="footer-copy">
          © 2026 LabManagement. Quản lý phòng thí nghiệm và tài sản.
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { 
  ExperimentOutlined, 
  ArrowRightOutlined, 
  DesktopOutlined, 
  HistoryOutlined, 
  ToolOutlined,
  CheckCircleFilled
} from '@ant-design/icons-vue'

const scrollToFeatures = () => {
  const el = document.getElementById('features')
  if (el) {
    el.scrollIntoView({ behavior: 'smooth' })
  }
}
</script>

<style scoped>
.landing-container {
  font-family: var(--font-sans);
  background-color: var(--color-canvas-cream);
  color: var(--color-ink);
  overflow-x: hidden;
}

/* Base Typography */
.serif-title {
  font-family: var(--font-serif);
  font-weight: 400;
  letter-spacing: -0.02em;
}

/* Button Overrides for Claude Style */
.btn-primary, .login-btn {
  background-color: var(--color-primary) !important;
  border-color: var(--color-primary) !important;
  color: #fff !important;
}
.btn-primary:hover, .login-btn:hover {
  background-color: var(--color-primary-hover) !important;
  border-color: var(--color-primary-hover) !important;
}

/* Navbar */
.navbar {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  background: rgba(242, 240, 237, 0.9); /* canvas-cream semi-transparent */
  backdrop-filter: blur(12px);
  z-index: 1000;
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}
.nav-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 16px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.logo {
  display: flex;
  align-items: center;
  gap: 12px;
}
.logo-icon {
  font-size: 24px;
  color: var(--color-primary);
}
.logo-text {
  font-size: 22px;
  font-weight: 800;
  letter-spacing: -0.5px;
  color: var(--color-ink);
}
.login-btn {
  border-radius: 24px;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(217, 119, 87, 0.2);
  transition: all 0.3s ease;
}
.login-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(217, 119, 87, 0.3);
}

/* Hero Section */
.hero-section {
  position: relative;
  padding: 160px 24px 80px;
  background: var(--color-canvas-cream);
  overflow: hidden;
}
.hero-container {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 40px;
  position: relative;
  z-index: 10;
}
.hero-content {
  flex: 1;
  text-align: left;
}
.badge {
  display: inline-block;
  padding: 6px 16px;
  background: var(--color-surface-dark);
  color: var(--color-on-dark);
  border-radius: 20px;
  font-weight: 500;
  font-size: 13px;
  margin-bottom: 24px;
}
.hero-title {
  font-family: var(--font-serif);
  font-size: clamp(40px, 4.5vw, 64px);
  font-weight: 400;
  letter-spacing: -0.02em;
  line-height: 1.1;
  margin-bottom: 24px;
  color: var(--color-ink);
}
.highlight {
  color: var(--color-primary);
}
.hero-subtitle {
  font-size: 18px;
  color: #4b5563;
  line-height: 1.6;
  margin-bottom: 40px;
  max-width: 540px;
}
.hero-cta {
  display: flex;
  gap: 16px;
}
.cta-btn {
  height: 52px;
  padding: 0 32px;
  font-size: 16px;
  border-radius: 26px;
  font-weight: 500;
}
.primary-cta {
  box-shadow: 0 8px 24px rgba(217, 119, 87, 0.2);
}
.primary-cta:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 32px rgba(217, 119, 87, 0.3);
}
.secondary-cta {
  background: transparent;
  color: var(--color-ink);
  border: 1px solid rgba(17, 24, 39, 0.2);
}
.secondary-cta:hover {
  color: var(--color-primary);
  border-color: var(--color-primary);
  background: transparent;
}
.hero-illustration {
  flex: 1;
  display: flex;
  justify-content: center;
}
.floating-img {
  width: 100%;
  max-width: 500px;
  border-radius: 20px;
  animation: float 6s ease-in-out infinite;
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.08);
}
@keyframes float {
  0% { transform: translateY(0px); }
  50% { transform: translateY(-20px); }
  100% { transform: translateY(0px); }
}

/* Statistics Section */
.stats-section {
  background: var(--color-surface-dark);
  color: var(--color-on-dark);
  padding: 80px 24px;
}
.stats-grid {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
  gap: 32px;
}
.stat-item {
  text-align: center;
}
.stat-item h2 {
  font-family: var(--font-serif);
  font-size: 56px;
  font-weight: 400;
  color: var(--color-primary);
  margin: 0 0 4px 0;
  line-height: 1;
}
.stat-item p {
  font-size: 16px;
  color: rgba(255,255,255,0.7);
  font-weight: 500;
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 1px;
}

/* Features Section */
.features-section {
  padding: 120px 24px;
  background: var(--color-canvas-cream);
  max-width: 1200px;
  margin: 0 auto;
}
.section-header {
  text-align: center;
  margin-bottom: 80px;
}
.section-header h2 {
  font-size: 40px;
  color: var(--color-ink);
  margin-bottom: 16px;
}
.section-header p {
  font-size: 18px;
  color: #6b7280;
  max-width: 600px;
  margin: 0 auto;
}
.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 32px;
}
.feature-card {
  background: #FFFFFF;
  border-radius: 16px;
  padding: 40px;
  text-align: left;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.02);
  border: 1px solid rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
}
.feature-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.06);
}
.feature-icon-wrapper {
  width: 56px;
  height: 56px;
  background: var(--color-canvas-cream);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 24px;
  color: var(--color-primary);
}
.feature-icon {
  font-size: 28px;
}
.feature-card h3 {
  font-size: 20px;
  font-weight: 600;
  color: var(--color-ink);
  margin-bottom: 12px;
}
.feature-card p {
  font-size: 15px;
  color: #6b7280;
  line-height: 1.6;
  margin: 0;
}

/* Split Section */
.split-section {
  background: var(--color-canvas-cream);
  padding: 40px 24px 120px;
}
.split-container {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 80px;
}
.split-image {
  flex: 1;
}
.shadow-img {
  width: 100%;
  border-radius: 16px;
  box-shadow: 0 20px 40px rgba(0,0,0,0.06);
  border: 1px solid rgba(0,0,0,0.05);
}
.split-content {
  flex: 1;
}
.split-content h2 {
  font-size: 40px;
  color: var(--color-ink);
  margin-bottom: 24px;
}
.split-desc {
  font-size: 18px;
  color: #4b5563;
  margin-bottom: 40px;
  line-height: 1.6;
}
.benefit-list {
  list-style: none;
  padding: 0;
  margin: 0;
}
.benefit-list li {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 24px;
}
.check-icon {
  font-size: 24px;
  color: var(--color-primary);
  margin-top: 2px;
}
.benefit-list div {
  font-size: 16px;
  color: #4b5563;
  line-height: 1.6;
}
.benefit-list strong {
  color: var(--color-ink);
  font-weight: 600;
}

/* CTA Band Coral */
.cta-band-coral {
  background-color: var(--color-primary);
  color: #fff;
  padding: 80px 24px;
  text-align: center;
}
.cta-band-content {
  max-width: 800px;
  margin: 0 auto;
}
.cta-band-content h2 {
  font-size: 48px;
  color: #fff;
  margin-bottom: 16px;
}
.cta-band-content p {
  font-size: 20px;
  opacity: 0.9;
  margin-bottom: 40px;
}
.btn-cream {
  background-color: var(--color-canvas-cream) !important;
  color: var(--color-ink) !important;
  border-color: var(--color-canvas-cream) !important;
}
.btn-cream:hover {
  background-color: #fff !important;
  border-color: #fff !important;
  transform: translateY(-2px);
}

/* Footer */
.footer {
  background: var(--color-surface-dark);
  color: var(--color-on-dark);
  padding: 64px 24px;
}
.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 24px;
}
.footer-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 18px;
  font-weight: 600;
}
.footer-logo {
  font-size: 24px;
  color: var(--color-on-dark);
}
.footer-copy {
  color: rgba(255,255,255,0.6);
  font-size: 14px;
}

/* Responsive */
@media (max-width: 992px) {
  .hero-container, .split-container { flex-direction: column; text-align: center; }
  .hero-content { text-align: center; margin-bottom: 40px; }
  .hero-title { font-size: 40px; }
  .hero-subtitle { margin: 0 auto 40px; }
  .hero-cta { justify-content: center; }
  .benefit-list li { text-align: left; }
}
@media (max-width: 768px) {
  .hero-cta { flex-direction: column; }
  .cta-btn { width: 100%; }
  .footer-content { flex-direction: column; text-align: center; }
  .stat-item h2 { font-size: 40px; }
  .cta-band-content h2 { font-size: 32px; }
}
</style>
