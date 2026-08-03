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
          <a-button type="primary" size="large" class="login-btn" @click="$router.push('/login')">
            Đăng nhập <arrow-right-outlined />
          </a-button>
        </div>
      </div>
    </header>

    <!-- Hero Section -->
    <section class="hero-section">
      <div class="hero-container">
        <div class="hero-content">
          <div class="badge">Hệ thống quản lý phòng thí nghiệm</div>
          <h1 class="hero-title">Quản lý thiết bị và tài sản<br><span class="highlight">minh bạch, dễ kiểm soát</span></h1>
          <p class="hero-subtitle">Theo dõi danh mục tài sản, yêu cầu mượn trả, bảo trì, vật tư và bồi thường trong một nền tảng thống nhất cho phòng thí nghiệm.</p>
          <div class="hero-cta">
            <a-button type="primary" size="large" class="cta-btn primary-cta" @click="$router.push('/login')">
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
      
      <!-- Background Shapes -->
      <div class="bg-shape shape-1"></div>
      <div class="bg-shape shape-2"></div>
    </section>

    <!-- Statistics Section -->
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

    <!-- Features Section -->
    <section class="features-section" id="features">
      <div class="section-header">
        <h2>Tính năng dành cho phòng thí nghiệm</h2>
        <p>Quản lý tài sản theo danh mục, theo dõi mượn trả, cấp phát vật tư và kiểm tra sau hoàn trả.</p>
      </div>
      
      <div class="features-grid">
        <div class="feature-card">
          <div class="feature-icon-wrapper blue">
            <desktop-outlined class="feature-icon" />
          </div>
          <h3>Quản lý tài sản</h3>
          <p>Lưu số seri, vị trí, người chịu trách nhiệm, ngày nhập, hạn bảo hành và số hóa đơn.</p>
        </div>
        
        <div class="feature-card">
          <div class="feature-icon-wrapper green">
            <history-outlined class="feature-icon" />
          </div>
          <h3>Mượn trả có phê duyệt</h3>
          <p>Người dùng gửi yêu cầu, giảng viên bảo lãnh khi cần, quản lý duyệt và theo dõi hạn trả.</p>
        </div>
        
        <div class="feature-card">
          <div class="feature-icon-wrapper red">
            <tool-outlined class="feature-icon" />
          </div>
          <h3>Kiểm tra và xử lý sau trả</h3>
          <p>Khi hoàn trả tài sản, hệ thống ghi nhận tình trạng, bảo hành, sửa chữa hoặc bồi thường nếu có.</p>
        </div>
      </div>
    </section>

    <!-- Split Section (How it works) -->
    <section class="split-section">
      <div class="split-container">
        <div class="split-image">
          <img src="/feature.png" alt="Lab Equipment Interface" class="shadow-img" />
        </div>
        <div class="split-content">
          <h2>Quy trình rõ ràng từ nhập kho đến hoàn trả</h2>
          <p class="split-desc">Tài sản được phân loại theo danh mục như IoT, AI, thiết bị đo hoặc linh kiện; mỗi lần mượn trả đều có trạng thái, người xử lý và lịch sử kiểm tra.</p>
          
          <ul class="benefit-list">
            <li>
              <check-circle-filled class="check-icon" />
              <div>
                <strong>Danh mục linh hoạt.</strong> Tạo nhóm tài sản phù hợp với từng phòng thí nghiệm.
              </div>
            </li>
            <li>
              <check-circle-filled class="check-icon" />
              <div>
          <strong>Phân quyền rõ.</strong> Admin, Trưởng lab, Phó lab, Giảng viên và Sinh viên dùng đúng vai trò.
              </div>
            </li>
            <li>
              <check-circle-filled class="check-icon" />
              <div>
                <strong>Hậu kiểm chặt.</strong> Trả tốt thì đưa về Rảnh, hỏng thì xét bảo hành hoặc bồi thường.
              </div>
            </li>
          </ul>
        </div>
      </div>
    </section>

    <!-- Footer -->
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
  font-family: 'Inter', sans-serif;
  background-color: #fafbfc;
  color: #1f2937;
  overflow-x: hidden;
}

/* Navbar */
.navbar {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  background: rgba(255, 255, 255, 0.9);
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
  color: #1890ff;
}
.logo-text {
  font-size: 22px;
  font-weight: 800;
  letter-spacing: -0.5px;
  background: linear-gradient(135deg, #001529 0%, #1890ff 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}
.login-btn {
  border-radius: 24px;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(24, 144, 255, 0.3);
  transition: all 0.3s ease;
}
.login-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(24, 144, 255, 0.4);
}

/* Hero Section */
.hero-section {
  position: relative;
  padding: 140px 24px 80px;
  background: linear-gradient(180deg, #f0f7ff 0%, #ffffff 100%);
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
  background: #e6f7ff;
  color: #1890ff;
  border-radius: 20px;
  font-weight: 600;
  font-size: 14px;
  margin-bottom: 24px;
  border: 1px solid #bae0ff;
}
.hero-title {
  font-size: 52px;
  font-weight: 900;
  line-height: 1.2;
  margin-bottom: 24px;
  color: #001529;
  letter-spacing: -1px;
}
.highlight {
  color: #1890ff;
}
.hero-subtitle {
  font-size: 18px;
  color: #4b5563;
  line-height: 1.6;
  margin-bottom: 40px;
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
  font-weight: 600;
}
.primary-cta {
  background: #1890ff;
  border: none;
  box-shadow: 0 8px 24px rgba(24, 144, 255, 0.3);
}
.primary-cta:hover {
  background: #096dd9;
  transform: translateY(-2px);
  box-shadow: 0 12px 32px rgba(24, 144, 255, 0.4);
}
.secondary-cta {
  background: white;
  color: #1890ff;
  border: 1px solid #1890ff;
}
.secondary-cta:hover {
  color: #096dd9;
  border-color: #096dd9;
  background: #f0f7ff;
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
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.1);
}
@keyframes float {
  0% { transform: translateY(0px); }
  50% { transform: translateY(-20px); }
  100% { transform: translateY(0px); }
}
.bg-shape {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  z-index: 1;
}
.shape-1 {
  width: 500px;
  height: 500px;
  background: rgba(24, 144, 255, 0.1);
  top: -100px;
  left: -150px;
}
.shape-2 {
  width: 400px;
  height: 400px;
  background: rgba(82, 196, 26, 0.08);
  bottom: 100px;
  right: -100px;
}

/* Statistics Section */
.stats-section {
  background: white;
  padding: 40px 24px;
  border-top: 1px solid #f0f0f0;
  border-bottom: 1px solid #f0f0f0;
}
.stats-grid {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
  gap: 24px;
}
.stat-item {
  text-align: center;
}
.stat-item h2 {
  font-size: 40px;
  font-weight: 800;
  color: #1890ff;
  margin: 0 0 8px 0;
}
.stat-item p {
  font-size: 16px;
  color: #6b7280;
  font-weight: 500;
  margin: 0;
}

/* Features Section */
.features-section {
  padding: 100px 24px;
  max-width: 1200px;
  margin: 0 auto;
}
.section-header {
  text-align: center;
  margin-bottom: 64px;
}
.section-header h2 {
  font-size: 36px;
  font-weight: 800;
  color: #001529;
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
  background: white;
  border-radius: 20px;
  padding: 40px;
  text-align: left;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.04);
  border: 1px solid rgba(0, 0, 0, 0.02);
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}
.feature-card:hover {
  transform: translateY(-10px);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.08);
}
.feature-icon-wrapper {
  width: 64px;
  height: 64px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 24px;
}
.feature-icon-wrapper.blue { background: #e6f7ff; color: #1890ff; }
.feature-icon-wrapper.green { background: #f6ffed; color: #52c41a; }
.feature-icon-wrapper.red { background: #fff1f0; color: #f5222d; }
.feature-icon {
  font-size: 32px;
}
.feature-card h3 {
  font-size: 20px;
  font-weight: 700;
  color: #1f2937;
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
  background: white;
  padding: 100px 24px;
}
.split-container {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 64px;
}
.split-image {
  flex: 1;
}
.shadow-img {
  width: 100%;
  border-radius: 20px;
  box-shadow: 0 20px 40px rgba(0,0,0,0.1);
  border: 1px solid #f0f0f0;
}
.split-content {
  flex: 1;
}
.split-content h2 {
  font-size: 36px;
  font-weight: 800;
  color: #001529;
  margin-bottom: 24px;
}
.split-desc {
  font-size: 18px;
  color: #4b5563;
  margin-bottom: 32px;
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
  color: #52c41a;
  margin-top: 4px;
}
.benefit-list div {
  font-size: 16px;
  color: #4b5563;
  line-height: 1.5;
}
.benefit-list strong {
  color: #1f2937;
  font-weight: 700;
}

/* Footer */
.footer {
  background: #001529;
  color: white;
  padding: 40px 24px;
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
  font-weight: 700;
}
.footer-logo {
  font-size: 24px;
  color: #1890ff;
}
.footer-copy {
  color: #8c8c8c;
  font-size: 14px;
}

/* Responsive */
@media (max-width: 992px) {
  .hero-container, .split-container { flex-direction: column; text-align: center; }
  .hero-content { text-align: center; }
  .hero-title { font-size: 40px; }
  .hero-cta { justify-content: center; }
  .benefit-list li { text-align: left; }
}
@media (max-width: 768px) {
  .hero-cta { flex-direction: column; }
  .cta-btn { width: 100%; }
  .footer-content { flex-direction: column; text-align: center; }
}
</style>



