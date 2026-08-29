<template>
  <div class="landing-container">
    <!-- Navbar -->
    <header class="navbar" :class="{ 'navbar-scrolled': isScrolled }">
      <div class="nav-content">
        <div class="logo">
          <experiment-outlined class="logo-icon" />
          <span class="logo-text">LabManagement</span>
        </div>
        <div class="nav-actions">
          <button class="premium-btn login-btn" @click="$router.push('/login')">
            <span class="btn-text">Đăng nhập <arrow-right-outlined /></span>
          </button>
        </div>
      </div>
    </header>

    <!-- Hero Section -->
    <section class="hero-section">
      <div class="hero-container">
        <div class="hero-content reveal slide-right">
          <div class="premium-badge">
            <span class="badge-dot"></span>
            Hệ thống quản lý thông minh thế hệ mới
          </div>
          <h1 class="hero-title">
            <span class="nowrap-desktop">Chuyển đổi số toàn diện</span> <br/>
            <span class="highlight text-gradient typing-container">{{ typedText }}<span class="cursor">|</span></span>
          </h1>
          <p class="hero-subtitle">Nền tảng hợp nhất giúp Khoa Công nghệ Thông tin số hóa quy trình quản lý thiết bị, kiểm kê, bảo trì và cấp phát vật tư chuyên nghiệp nhất.</p>
          <div class="hero-cta">
            <button class="premium-btn primary-cta" @click="$router.push('/login')">
              <span class="btn-text">Vào hệ thống ngay</span>
            </button>
            <button class="premium-btn-outline secondary-cta" @click="scrollToShowcase">
              <span class="btn-text">Khám phá tính năng <arrow-down-outlined style="font-size: 14px; margin-left: 6px;" /></span>
            </button>
          </div>
        </div>
        
        <div class="hero-illustration reveal slide-left reveal-delay-2">
          <div class="glow-orb"></div>
          <div class="hero-composition">
            <img src="https://images.unsplash.com/photo-1581092160562-40aa08e78837?q=80&w=1000&auto=format&fit=crop" alt="Phòng Lab Công Nghệ" class="floating-img main-img" />
            
            <!-- Light Floating Elements -->
            <div class="floating-card glass-badge card-top-left float-delay-1">
              <div class="card-icon"><desktop-outlined /></div>
              <div class="card-text">
                <strong>+1,200</strong>
                <span>Thiết bị IoT</span>
              </div>
            </div>
            
            <div class="floating-card glass-badge card-bottom-right float-delay-2">
              <div class="card-icon success-icon"><check-circle-filled /></div>
              <div class="card-text">
                <strong>Quét QR</strong>
                <span>Thành công!</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Interactive Showcase Carousel -->
    <section class="showcase-section" id="showcase">
      <div class="section-header reveal fade-up">
        <h2 class="serif-title">Trải nghiệm thị giác hiện đại</h2>
        <p>Quản lý trang thiết bị công nghệ cao qua lăng kính trực quan nhất.</p>
      </div>
      
      <div class="carousel-container premium-glass reveal fade-up reveal-delay-1">
        <div class="carousel-nav">
          <div 
            v-for="(slide, index) in slides" 
            :key="index"
            class="carousel-nav-item"
            :class="{ active: currentSlide === index }"
            @click="setSlide(index)"
          >
            <div class="progress-bar">
              <div class="progress-fill" :style="{ width: currentSlide === index ? progress + '%' : (currentSlide > index ? '100%' : '0%') }"></div>
            </div>
            <h3>{{ slide.title }}</h3>
            <p>{{ slide.desc }}</p>
          </div>
        </div>
        
        <div class="carousel-visual">
          <transition name="fade-scale" mode="out-in">
            <img :key="slides[currentSlide].image" :src="slides[currentSlide].image" :alt="slides[currentSlide].title" class="carousel-image" />
          </transition>
          <div class="carousel-img-overlay"></div>
        </div>
      </div>
    </section>

    <!-- Statistics Section -->
    <section class="stats-section">
      <div class="stats-grid">
        <div class="stat-item premium-glass reveal fade-up">
          <h2>Mã hóa</h2>
          <p>Quản lý Seri</p>
        </div>
        <div class="stat-item premium-glass reveal fade-up reveal-delay-1">
          <h2>Kiểm kê</h2>
          <p>Webcam & QR</p>
        </div>
        <div class="stat-item premium-glass reveal fade-up reveal-delay-2">
          <h2>Phiếu xuất</h2>
          <p>Mượn tài sản</p>
        </div>
        <div class="stat-item premium-glass reveal fade-up reveal-delay-3">
          <h2>Chu kỳ</h2>
          <p>Bảo trì & Vật tư</p>
        </div>
      </div>
    </section>

    <!-- Workflow Timeline Section -->
    <section class="workflow-section">
      <div class="section-header reveal fade-up">
        <h2 class="serif-title">Quy trình khép kín & minh bạch</h2>
        <p>Mọi bước mượn trả đều được số hóa, gắn liền với trách nhiệm rõ ràng.</p>
      </div>
      
      <div class="timeline-container">
        <div class="timeline-line reveal slide-right"></div>
        
        <div class="timeline-step reveal fade-up">
          <div class="step-circle premium-glass"><form-outlined /></div>
          <div class="step-content">
            <h3>1. Tạo yêu cầu</h3>
            <p>Sinh viên tạo phiếu mượn thiết bị và chọn giảng viên bảo lãnh trên hệ thống.</p>
          </div>
        </div>
        
        <div class="timeline-step reveal fade-up reveal-delay-1">
          <div class="step-circle premium-glass"><check-square-outlined /></div>
          <div class="step-content">
            <h3>2. Phê duyệt</h3>
            <p>Giảng viên duyệt bảo lãnh, sau đó Quản lý lab kiểm tra và chấp nhận yêu cầu.</p>
          </div>
        </div>
        
        <div class="timeline-step reveal fade-up reveal-delay-2">
          <div class="step-circle premium-glass"><solution-outlined /></div>
          <div class="step-content">
            <h3>3. Bàn giao</h3>
            <p>Hai bên đối chiếu tình trạng, lập biên bản bàn giao kèm hình ảnh chụp nhanh.</p>
          </div>
        </div>
        
        <div class="timeline-step reveal fade-up reveal-delay-3">
          <div class="step-circle premium-glass"><sync-outlined /></div>
          <div class="step-content">
            <h3>4. Nhận trả</h3>
            <p>Ghi nhận tài sản trả về và tự động lên lịch bảo trì nếu phát hiện hỏng hóc.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Features Bento Box Section (Light Version) -->
    <section class="features-section" id="features">
      <div class="section-header reveal fade-up">
        <h2 class="serif-title">Hệ sinh thái chức năng siêu cấp</h2>
        <p>Kiểm soát từng chi tiết nhỏ nhất trong hoạt động hằng ngày của Phòng Lab IoT.</p>
      </div>
      
      <div class="bento-grid">
        <!-- Feature 1 -->
        <div class="bento-card bento-lead premium-glass reveal fade-up">
          <div class="bento-glow"></div>
          <div class="bento-content">
            <div class="feature-icon-wrapper">
              <cluster-outlined class="feature-icon" />
            </div>
            <h3>Cấu trúc không gian vô cực</h3>
            <p>Tổ chức tài sản theo cây: Tòa nhà → Phòng → Tủ/kệ → Ngăn, giúp định vị nhanh và hạn chế sai lệch vị trí.</p>
          </div>
        </div>
        
        <!-- Feature 2 -->
        <div class="bento-card bento-side premium-glass reveal fade-up reveal-delay-1">
          <div class="bento-glow"></div>
          <div class="bento-content">
            <div class="feature-icon-wrapper">
              <desktop-outlined class="feature-icon" />
            </div>
            <h3>Định danh IoT</h3>
            <p>Theo dõi vòng đời thiết bị, firmware, MAC, IMEI, bảo hành và toàn bộ lịch sử sử dụng.</p>
          </div>
        </div>
        
        <!-- Feature 3 -->
        <div class="bento-card premium-glass reveal fade-up reveal-delay-2">
          <div class="bento-glow"></div>
          <div class="bento-content">
            <div class="feature-icon-wrapper">
              <qrcode-outlined class="feature-icon" />
            </div>
            <h3>Mã QR Động</h3>
            <p>Tạo QR bảo mật, in hàng loạt và quét liên tục bằng camera để kiểm kê nhanh.</p>
          </div>
        </div>

        <!-- Feature 4 -->
        <div class="bento-card premium-glass reveal fade-up">
          <div class="bento-glow"></div>
          <div class="bento-content">
            <div class="feature-icon-wrapper">
              <apartment-outlined class="feature-icon" />
            </div>
            <h3>Phân quyền Đa tầng</h3>
            <p>Phân quyền RBAC rõ ràng cho Admin, Trưởng Lab, Phó Lab, Giảng viên và Sinh viên.</p>
          </div>
        </div>
        
        <!-- Feature 5 -->
        <div class="bento-card premium-glass reveal fade-up reveal-delay-1">
          <div class="bento-glow"></div>
          <div class="bento-content">
            <div class="feature-icon-wrapper">
              <bell-outlined class="feature-icon" />
            </div>
            <h3>Realtime & Báo cáo nâng cao</h3>
            <p>Nhận thông báo thời gian thực qua SignalR và xuất báo cáo chênh lệch ra Excel hoặc PDF.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Split Section -->
    <section class="split-section">
      <div class="split-container">
        <div class="split-image reveal slide-right">
          <div class="img-glow-backdrop"></div>
          <img src="/feature.png" alt="Giao diện báo cáo" class="shadow-img premium-glass" />
        </div>
        <div class="split-content reveal slide-left">
          <h2 class="serif-title">Lưu trữ trên nền tảng Đám mây</h2>
          <p class="split-desc">Mọi biên bản, quyết định hay hóa đơn đều được số hóa. Nền tảng được thiết kế tương thích với kiến trúc AWS S3 / MinIO cao cấp nhất.</p>
          
          <ul class="benefit-list">
            <li class="premium-glass reveal fade-up reveal-delay-1">
              <check-circle-filled class="check-icon breathing" />
              <div>
                <strong>Lưu vết kiểm toán (Audit Log).</strong> Ghi nhận mọi thao tác tạo, sửa, xóa với địa chỉ IP và thời gian chuẩn xác nhất.
              </div>
            </li>
            <li class="premium-glass reveal fade-up reveal-delay-2">
              <check-circle-filled class="check-icon breathing" />
              <div>
                <strong>Bằng chứng giao nhận.</strong> Bắt buộc chụp ảnh và tải tệp lên trong các bước bàn giao tài sản và báo cáo hỏng hóc.
              </div>
            </li>
            <li class="premium-glass reveal fade-up reveal-delay-3">
              <check-circle-filled class="check-icon breathing" />
              <div>
                <strong>Bảo mật cấp doanh nghiệp.</strong> Chặn mã độc giả mạo đuôi tệp tin (Magic Bytes Validation), bảo vệ kho dữ liệu.
              </div>
            </li>
          </ul>
        </div>
      </div>
    </section>

    <!-- CTA Band -->
    <section class="cta-band-coral reveal fade-up">
      <div class="cta-band-content">
        <h2 class="serif-title text-white">Sẵn sàng bước vào Kỷ nguyên số?</h2>
        <p class="text-white">Bắt đầu ngay hôm nay để trải nghiệm một tiêu chuẩn quản lý hoàn toàn mới.</p>
        <button class="premium-btn cta-final" @click="$router.push('/login')">
          <span class="btn-text" style="color: var(--color-primary)">Khởi chạy Hệ thống</span>
        </button>
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
          © 2026 LabManagement — Premium IoT Lab System.
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { 
  ExperimentOutlined, 
  ArrowRightOutlined,
  ArrowDownOutlined,
  DesktopOutlined, 
  QrcodeOutlined,
  ApartmentOutlined,
  BellOutlined,
  ClusterOutlined,
  CheckCircleFilled,
  FormOutlined,
  CheckSquareOutlined,
  SolutionOutlined,
  SyncOutlined
} from '@ant-design/icons-vue'

// --- SCROLL REVEAL & NAVBAR ---
const isScrolled = ref(false)
const handleScroll = () => {
  isScrolled.value = window.scrollY > 50
}

const scrollToShowcase = () => {
  const el = document.getElementById('showcase')
  if (el) {
    el.scrollIntoView({ behavior: 'smooth' })
  }
}

// --- TYPING EFFECT ---
const typedText = ref('')
const typingPhrases = ['Phòng Lab Công Nghệ', 'Trung Tâm Thực Hành', 'Hệ Sinh Thái IoT']
let phraseIndex = 0
let charIndex = 0
let isDeleting = false
let typingTimer = null

const typeEffect = () => {
  const currentPhrase = typingPhrases[phraseIndex]
  
  if (isDeleting) {
    typedText.value = currentPhrase.substring(0, charIndex - 1)
    charIndex--
  } else {
    typedText.value = currentPhrase.substring(0, charIndex + 1)
    charIndex++
  }

  let typeSpeed = isDeleting ? 40 : 100

  if (!isDeleting && charIndex === currentPhrase.length) {
    typeSpeed = 2500 
    isDeleting = true
  } else if (isDeleting && charIndex === 0) {
    isDeleting = false
    phraseIndex = (phraseIndex + 1) % typingPhrases.length
    typeSpeed = 600 
  }

  typingTimer = setTimeout(typeEffect, typeSpeed)
}

// --- CAROUSEL LOGIC ---
const slides = [
  {
    title: 'Công nghệ phần cứng hiện đại',
    desc: 'Quản lý hàng ngàn thiết bị vi mạch, vi điều khiển, cảm biến với thông số kỹ thuật chi tiết.',
    image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?q=80&w=1200&auto=format&fit=crop'
  },
  {
    title: 'Không gian thông minh',
    desc: 'Định vị không gian vật lý, giám sát vị trí tủ kệ chính xác và chống thất thoát dữ liệu.',
    image: 'https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?q=80&w=1200&auto=format&fit=crop'
  },
  {
    title: 'Kiểm kê siêu tốc',
    desc: 'Tích hợp Camera QR để quét tự động, lập biên bản kiểm đếm mà không cần giấy tờ thủ công.',
    image: 'https://images.unsplash.com/photo-1522071820081-009f0129c71c?q=80&w=1200&auto=format&fit=crop'
  }
]

const currentSlide = ref(0)
const progress = ref(0)
const SLIDE_DURATION = 5000 
const UPDATE_INTERVAL = 50 
let progressTimer = null

const startCarousel = () => {
  progressTimer = setInterval(() => {
    progress.value += (UPDATE_INTERVAL / SLIDE_DURATION) * 100
    if (progress.value >= 100) {
      progress.value = 0
      currentSlide.value = (currentSlide.value + 1) % slides.length
    }
  }, UPDATE_INTERVAL)
}

const setSlide = (index) => {
  clearInterval(progressTimer)
  currentSlide.value = index
  progress.value = 0
  startCarousel()
}

// --- LIFECYCLE ---
onMounted(() => {
  window.addEventListener('scroll', handleScroll)
  
  typingTimer = setTimeout(typeEffect, 1000)
  startCarousel()
  
  const observerOptions = {
    root: null,
    rootMargin: '0px',
    threshold: 0.15
  }

  const observer = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('is-visible')
        observer.unobserve(entry.target)
      }
    })
  }, observerOptions)

  document.querySelectorAll('.reveal').forEach(el => {
    observer.observe(el)
  })
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  clearTimeout(typingTimer)
  clearInterval(progressTimer)
})
</script>

<style scoped>
/* =========================================
   LIGHT THEME & BASE
   ========================================= */
.landing-container {
  font-family: var(--font-sans);
  background-color: var(--color-canvas-cream);
  color: var(--color-ink);
  overflow-x: hidden;
  position: relative;
  min-height: 100vh;
}

/* Typography Enhancements */
.serif-title {
  font-family: var(--font-sans);
  font-weight: 800;
  letter-spacing: -0.03em;
  color: var(--color-ink);
}
.text-gradient {
  background: linear-gradient(135deg, var(--color-primary), #E99D86);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}
.text-white { color: white !important; }
.typing-container {
  color: var(--color-ink);
}
.cursor {
  display: inline-block;
  color: var(--color-primary);
  animation: blink 1s step-end infinite;
  margin-left: 2px;
}
@keyframes blink { 50% { opacity: 0; } }

/* Scroll Reveal */
.reveal {
  opacity: 0;
  transition: opacity 1s ease, transform 1s cubic-bezier(0.16, 1, 0.3, 1);
}
.reveal.is-visible {
  opacity: 1;
  transform: translate(0, 0);
}
.fade-up { transform: translateY(50px); }
.slide-right { transform: translateX(-60px); }
.slide-left { transform: translateX(60px); }
.reveal-delay-1 { transition-delay: 0.15s; }
.reveal-delay-2 { transition-delay: 0.3s; }
.reveal-delay-3 { transition-delay: 0.45s; }

/* =========================================
   PREMIUM BUTTONS & GLASSMORPHISM
   ========================================= */
.premium-btn {
  position: relative;
  background: var(--color-primary);
  color: white;
  border: none;
  padding: 14px 32px;
  border-radius: 100px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  overflow: hidden;
  transition: all 0.3s ease;
  box-shadow: 0 10px 25px rgba(217, 119, 87, 0.3);
  z-index: 1;
}
.premium-btn::before {
  content: '';
  position: absolute;
  top: 0; left: -100%; width: 100%; height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
  transition: all 0.5s ease;
  z-index: -1;
}
.premium-btn:hover::before { left: 100%; }
.premium-btn:hover {
  transform: translateY(-3px);
  box-shadow: 0 15px 35px rgba(217, 119, 87, 0.4);
}

.premium-btn-outline {
  position: relative;
  background: rgba(255,255,255,0.7);
  backdrop-filter: blur(10px);
  color: var(--color-ink);
  border: 1px solid rgba(0,0,0,0.1);
  padding: 14px 32px;
  border-radius: 100px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}
.premium-btn-outline:hover {
  background: white;
  box-shadow: 0 10px 25px rgba(0,0,0,0.05);
  transform: translateY(-3px);
}

.premium-glass {
  position: relative;
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border: 1px solid rgba(255, 255, 255, 1);
  border-radius: 24px;
  overflow: hidden;
  box-shadow: 0 10px 40px rgba(0,0,0,0.03);
  transition: all 0.4s ease;
}
.premium-glass:hover {
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 20px 50px rgba(0,0,0,0.08);
  transform: translateY(-5px);
}

/* Navbar */
.navbar {
  position: fixed;
  top: 0; left: 0; width: 100%;
  background: rgba(255, 255, 255, 0.4);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  z-index: 1000;
  border-bottom: 1px solid transparent;
  transition: all 0.3s ease;
}
.navbar-scrolled {
  background: rgba(255, 255, 255, 0.95);
  border-bottom: 1px solid rgba(0,0,0,0.05);
  box-shadow: 0 5px 20px rgba(0,0,0,0.03);
}
.nav-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 16px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.logo-icon {
  font-size: 26px;
  color: var(--color-primary);
  animation: pulse 3s infinite alternate;
}
.logo-text {
  font-size: 22px;
  font-weight: 800;
  letter-spacing: -0.5px;
  color: var(--color-ink);
  margin-left: 10px;
}
.login-btn {
  padding: 10px 24px;
  font-size: 14px;
}

/* =========================================
   HERO SECTION
   ========================================= */
.hero-section {
  position: relative;
  padding: 200px 24px 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: visible;
  z-index: 2;
  background: linear-gradient(135deg, rgba(217,119,87,0.08) 0%, var(--color-canvas-cream) 40%, rgba(15,58,90,0.05) 100%);
}
.hero-section::before {
  content: '';
  position: absolute;
  top: -30%; left: -20%;
  width: 70%; height: 80%;
  background: radial-gradient(circle, rgba(217,119,87,0.2) 0%, rgba(255,255,255,0) 70%);
  z-index: -1;
  animation: bg-pulse 10s ease-in-out infinite alternate;
}
@keyframes bg-pulse {
  0% { transform: scale(1); opacity: 0.8; }
  100% { transform: scale(1.1); opacity: 1; }
}

.hero-container {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 60px;
  position: relative;
}
.hero-content { flex: 1; }
.premium-badge {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px;
  background: rgba(15, 58, 90, 0.08);
  border: 1px solid rgba(15, 58, 90, 0.1);
  border-radius: 100px;
  font-size: 13px;
  font-weight: 600;
  color: var(--public-blue, #0f3a5a);
  margin-bottom: 32px;
}
.badge-dot {
  width: 8px; height: 8px;
  background: var(--color-primary);
  border-radius: 50%;
  box-shadow: 0 0 10px rgba(217,119,87,0.5);
}
.hero-title {
  font-size: clamp(30px, 3.8vw, 50px);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.25;
  margin-bottom: 24px;
  min-height: 125px;
}
@media (min-width: 992px) { .nowrap-desktop { white-space: nowrap; } }
.hero-subtitle {
  font-size: 18px;
  color: #4b5563;
  line-height: 1.7;
  margin-bottom: 40px;
  max-width: 540px;
}
.hero-cta { display: flex; gap: 16px; }

.hero-illustration {
  flex: 1;
  display: flex;
  justify-content: center;
}
.hero-composition {
  position: relative;
  width: 100%;
  max-width: 540px;
}
.main-img {
  width: 100%;
  border-radius: 24px;
  animation: float-main 8s ease-in-out infinite;
  box-shadow: 0 30px 60px rgba(0,0,0,0.1);
  border: 2px solid white;
}
@keyframes float-main {
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-15px); }
}

.glow-orb {
  position: absolute;
  top: 50%; left: 50%;
  transform: translate(-50%, -50%);
  width: 350px; height: 350px;
  background: linear-gradient(135deg, var(--color-primary), #E99D86);
  filter: blur(120px);
  opacity: 0.25;
  border-radius: 50%;
  animation: orb-breathe 8s infinite alternate;
  z-index: -1;
}
@keyframes orb-breathe {
  0% { transform: translate(-50%, -50%) scale(0.8) rotate(0deg); opacity: 0.15; }
  100% { transform: translate(-50%, -50%) scale(1.2) rotate(10deg); opacity: 0.3; }
}

.floating-card {
  position: absolute;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 20px;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border: 1px solid white;
  border-radius: 100px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.08);
  animation: float-card 6s ease-in-out infinite;
}
.card-top-left { top: 40px; left: -40px; }
.card-bottom-right { bottom: 40px; right: -40px; }
@keyframes float-card {
  0%, 100% { transform: translateY(0px) rotate(0deg); }
  50% { transform: translateY(-20px) rotate(2deg); }
}
.card-icon {
  width: 36px; height: 36px;
  background: rgba(217,119,87,0.1);
  color: var(--color-primary);
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 18px;
}
.success-icon {
  background: rgba(16, 185, 129, 0.1);
  color: #10B981;
}
.card-text strong {
  display: block;
  font-size: 16px; font-weight: 700; color: var(--color-ink); line-height: 1.2;
}
.card-text span {
  font-size: 13px; color: #6b7280;
}

/* =========================================
   CAROUSEL SECTION
   ========================================= */
.showcase-section {
  padding: 100px 24px;
  max-width: 1200px;
  margin: 0 auto;
  position: relative;
  z-index: 2;
}
.section-header {
  text-align: center;
  margin-bottom: 80px;
}
.section-header h2 {
  font-size: 44px;
  margin-bottom: 16px;
}
.section-header p {
  font-size: 18px; color: #6b7280;
  max-width: 600px; margin: 0 auto;
}
.carousel-container {
  display: flex;
  min-height: 500px;
}
.carousel-nav {
  width: 350px;
  padding: 30px 0;
  display: flex; flex-direction: column; justify-content: center;
  border-right: 1px solid rgba(0,0,0,0.05);
}
.carousel-nav-item {
  padding: 24px 30px;
  cursor: pointer;
  position: relative;
  transition: background 0.3s;
}
.carousel-nav-item:hover { background: rgba(0,0,0,0.02); }
.carousel-nav-item.active { background: rgba(217,119,87,0.05); }
.carousel-nav-item h3 {
  font-size: 18px; font-weight: 700; color: #4b5563; margin-bottom: 8px;
  transition: color 0.3s;
}
.carousel-nav-item.active h3 { color: var(--color-primary); }
.carousel-nav-item p {
  font-size: 14px; color: #6b7280; margin: 0; line-height: 1.5;
}
.progress-bar {
  position: absolute; left: 0; top: 0; width: 3px; height: 100%;
  background: transparent;
}
.progress-fill {
  width: 100%; height: 0%;
  background: var(--color-primary);
  transition: height 0.1s linear;
}

.carousel-visual {
  flex: 1; position: relative; background: #000; overflow: hidden;
}
.carousel-image {
  position: absolute; top: 0; left: 0; width: 100%; height: 100%; object-fit: cover;
}
.carousel-img-overlay {
  position: absolute; inset: 0;
  background: linear-gradient(90deg, rgba(255,255,255,0.4) 0%, transparent 30%);
  pointer-events: none;
}
.fade-scale-enter-active, .fade-scale-leave-active { transition: all 1s ease; }
.fade-scale-enter-from { opacity: 0; transform: scale(1.05); }
.fade-scale-leave-to { opacity: 0; transform: scale(0.95); }

/* =========================================
   STATISTICS
   ========================================= */
.stats-section {
  padding: 40px 24px 80px;
  max-width: 1200px; margin: 0 auto;
  position: relative; z-index: 2;
}
.stats-grid {
  display: grid; grid-template-columns: repeat(4, 1fr); gap: 24px;
}
.stat-item {
  text-align: center; padding: 32px 20px;
  background: linear-gradient(135deg, rgba(255,255,255,0.9), rgba(255,255,255,0.6));
}
.stat-item h2 {
  font-size: 42px; font-weight: 700; color: var(--color-ink); margin: 0 0 8px 0;
}
.stat-item p {
  font-size: 14px; color: #6b7280; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin: 0;
}

/* =========================================
   WORKFLOW TIMELINE
   ========================================= */
.workflow-section {
  padding: 80px 24px;
  max-width: 1200px; margin: 0 auto;
  position: relative; z-index: 2;
}
.timeline-container {
  display: flex; justify-content: space-between; position: relative; margin-top: 60px;
}
.timeline-line {
  position: absolute; top: 30px; left: 5%; right: 5%; height: 2px;
  background: linear-gradient(90deg, transparent 0%, rgba(217,119,87,0.3) 50%, transparent 100%);
  z-index: 1;
}
.timeline-step {
  flex: 1; text-align: center; position: relative; z-index: 2; padding: 0 16px;
}
.step-circle {
  width: 64px; height: 64px;
  background: white;
  margin: 0 auto 24px;
  display: flex; align-items: center; justify-content: center;
  font-size: 24px; color: var(--color-primary);
  border-radius: 50%;
}
.timeline-step:hover .step-circle {
  background: var(--color-primary); color: white;
}
.step-content h3 {
  font-size: 18px; font-weight: 700; margin-bottom: 12px; color: var(--color-ink);
}
.step-content p {
  font-size: 14px; color: #6b7280; line-height: 1.6;
}

/* =========================================
   BENTO BOX FEATURES
   ========================================= */
.features-section {
  padding: 80px 24px 120px;
  max-width: 1320px; margin: 0 auto;
  position: relative; z-index: 2;
}
.bento-grid {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 20px;
}
.bento-card {
  grid-column: span 4;
  min-width: 0;
  min-height: 230px;
  padding: 30px 32px;
  display: flex; flex-direction: column;
}
.bento-lead { grid-column: span 7; }
.bento-side { grid-column: span 5; }

/* Glowing Border Effect for Bento (Light Theme Variant) */
.bento-glow {
  position: absolute;
  top: 0; left: 0; width: 100%; height: 100%;
  background: radial-gradient(circle at 50% -20%, rgba(217,119,87,0.1), transparent 50%);
  opacity: 0;
  transition: opacity 0.5s ease;
  pointer-events: none;
}
.bento-card:hover .bento-glow { opacity: 1; }

.bento-content { position: relative; z-index: 2; height: 100%; }
.feature-icon-wrapper {
  width: 52px; height: 52px;
  background: rgba(217,119,87,0.1);
  border: 1px solid rgba(217,119,87,0.2);
  border-radius: 16px;
  display: flex; align-items: center; justify-content: center;
  margin-bottom: 20px; color: var(--color-primary); font-size: 22px;
  transition: all 0.3s ease;
}
.bento-card:hover .feature-icon-wrapper {
  background: var(--color-primary); color: white; transform: scale(1.1) rotate(5deg);
}
.bento-card h3 {
  font-size: 20px; font-weight: 700; color: var(--color-ink); margin: 0 0 10px;
}
.bento-card p {
  max-width: 68ch;
  font-size: 15px; color: #4b5563; line-height: 1.6; margin: 0;
}

/* =========================================
   SPLIT SECTION
   ========================================= */
.split-section {
  padding: 60px 24px 120px;
  max-width: 1200px; margin: 0 auto;
  position: relative; z-index: 2;
}
.split-container {
  display: flex; align-items: center; gap: 80px;
}
.split-image { flex: 1; position: relative; }
.img-glow-backdrop {
  position: absolute; top: 10%; left: 10%; width: 80%; height: 80%;
  background: var(--public-blue, #0f3a5a); filter: blur(80px); opacity: 0.15; border-radius: 20px;
}
.shadow-img { width: 100%; display: block; border-radius: 24px;}
.split-content { flex: 1; }
.split-content h2 {
  font-size: 42px; font-weight: 800; margin-bottom: 24px; line-height: 1.2;
}
.split-desc {
  font-size: 18px; color: #4b5563; margin-bottom: 40px; line-height: 1.7;
}
.benefit-list { list-style: none; padding: 0; margin: 0; }
.benefit-list li {
  display: flex; align-items: flex-start; gap: 20px;
  padding: 20px; margin-bottom: 24px;
}
.check-icon {
  font-size: 24px; color: var(--color-primary); margin-top: 2px;
}
.benefit-list div { font-size: 15px; color: #4b5563; line-height: 1.6; }
.benefit-list strong { color: var(--color-ink); font-weight: 700; display: block; margin-bottom: 4px; font-size: 16px; }

/* =========================================
   CTA & FOOTER
   ========================================= */
.cta-band-coral {
  background: linear-gradient(135deg, var(--color-primary), #d96248);
  padding: 100px 24px;
  text-align: center;
  position: relative; z-index: 2;
  overflow: hidden;
}
.cta-band-coral::before {
  content: '';
  position: absolute; top: -50%; left: -50%; width: 200%; height: 200%;
  background: radial-gradient(circle, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0) 60%);
  animation: rotate-gradient 15s linear infinite; pointer-events: none;
}
@keyframes rotate-gradient { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

.cta-band-content {
  max-width: 800px; margin: 0 auto; position: relative; z-index: 2;
}
.cta-band-content h2 { font-size: 48px; margin-bottom: 16px; }
.cta-band-content p { font-size: 20px; margin-bottom: 40px; opacity: 0.9;}
.cta-final { background: white; color: var(--color-primary); padding: 16px 40px; font-size: 18px; border: none; }
.cta-final:hover { background: #f9f9f9; box-shadow: 0 15px 35px rgba(0,0,0,0.25); transform: translateY(-3px);}

.footer {
  border-top: 1px solid rgba(0,0,0,0.05);
  padding: 40px 24px;
  position: relative; z-index: 2;
  background: #0b2239;
  color: rgba(255,255,255,0.7);
}
.footer-content {
  max-width: 1200px; margin: 0 auto;
  display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 24px;
}
.footer-brand { display: flex; align-items: center; gap: 12px; font-size: 18px; font-weight: 700; color: white; }
.footer-copy { font-size: 14px; }
.footer-logo { color: var(--color-primary); font-size: 24px;}

/* =========================================
   RESPONSIVE
   ========================================= */
@media (max-width: 992px) {
  .hero-container, .split-container { flex-direction: column; text-align: center; }
  .hero-title { font-size: 46px; min-height: auto; }
  .hero-subtitle { margin: 0 auto 40px; }
  .hero-cta { justify-content: center; }
  .bento-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .bento-card, .bento-side { grid-column: span 1; }
  .bento-lead { grid-column: span 2; }
  .stats-grid { grid-template-columns: repeat(2, 1fr); }
  .carousel-container { flex-direction: column; }
  .carousel-nav { width: 100%; flex-direction: row; border-right: none; border-bottom: 1px solid rgba(0,0,0,0.05); padding: 0; }
  .carousel-nav-item { flex: 1; padding: 16px; text-align: center; }
  .carousel-nav-item h3 { font-size: 16px; margin: 0; }
  .carousel-nav-item p { display: none; }
  .progress-bar { width: 100%; height: 3px; top: auto; bottom: -1px; left: 0; }
  .progress-fill { height: 3px; width: 0%; transition: width 0.1s linear; }
  .carousel-visual { min-height: 300px; }
  .timeline-container { flex-direction: column; gap: 40px; }
  .timeline-line { display: none; }
  .benefit-list li { text-align: left; }
}
@media (max-width: 768px) {
  .bento-grid { grid-template-columns: 1fr; }
  .bento-card, .bento-lead, .bento-side { grid-column: span 1; }
  .hero-cta { flex-direction: column; }
  .premium-btn, .premium-btn-outline { width: 100%; }
  .footer-content { flex-direction: column; text-align: center; }
  .cta-band-content h2 { font-size: 36px; }
}
@media (max-width: 480px) {
  .stats-grid { grid-template-columns: 1fr; }
  .hero-section { padding-top: 140px; }
  .carousel-nav { flex-direction: column; }
  .carousel-nav-item p { display: block; margin-top: 8px;}
  .progress-bar { width: 3px; height: 100%; left: -1px; top: 0; }
  .progress-fill { width: 3px; height: 0%; transition: height 0.1s linear; }
}
</style>
