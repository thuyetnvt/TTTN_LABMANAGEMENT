<template>
  <div class="split-layout">
    <!-- Nửa trái: Thông tin giới thiệu (Blue side) -->
    <div class="left-side">
      <div class="left-content">
        <!-- Header -->
        <div class="brand-header">
          <div class="logo">
            <experiment-outlined /> <span>LabManagement</span>
            <span class="platform-tag">Phòng Lab IoT</span>
          </div>
          <div class="tag-pill">{{ $t('hero.brandTag') }}</div>
        </div>

        <!-- Hero Text -->
        <h1 class="hero-title">
          <span class="hero-title-line">{{ $t('hero.title1') }}</span>
          <span class="hero-title-line hero-title-brand">
            <span class="hero-title-iot">{{ $t('hero.title2') }}</span>
            <span class="text-coral">{{ $t('hero.title3') }}</span>
          </span>
        </h1>
        <p class="hero-desc">
          {{ $t('hero.desc') }}
        </p>

        <!-- Features Grid -->
        <div class="features-grid">
          <div class="feature-item">
            <div class="feature-icon"><desktop-outlined /></div>
            <div class="feature-text">
              <h4>{{ $t('hero.feature1Title') }}</h4>
              <p>{{ $t('hero.feature1Desc') }}</p>
            </div>
          </div>
          <div class="feature-item">
            <div class="feature-icon"><history-outlined /></div>
            <div class="feature-text">
              <h4>{{ $t('hero.feature2Title') }}</h4>
              <p>{{ $t('hero.feature2Desc') }}</p>
            </div>
          </div>
          <div class="feature-item">
            <div class="feature-icon"><appstore-outlined /></div>
            <div class="feature-text">
              <h4>{{ $t('hero.feature3Title') }}</h4>
              <p>{{ $t('hero.feature3Desc') }}</p>
            </div>
          </div>
          <div class="feature-item">
            <div class="feature-icon"><tool-outlined /></div>
            <div class="feature-text">
              <h4>{{ $t('hero.feature4Title') }}</h4>
              <p>{{ $t('hero.feature4Desc') }}</p>
            </div>
          </div>
        </div>

        <!-- Statistics -->
        <div class="stats-section">
          <div class="stats-title">{{ $t('hero.statsTitle') }}</div>
          <div class="stats-row">
            <div class="stat-box">
              <h3>Tài sản</h3>
              <p>Mã, số seri và vị trí</p>
            </div>
            <div class="stat-box">
              <h3>QR</h3>
              <p>Tra cứu và kiểm kê</p>
            </div>
            <div class="stat-box">
              <h3>Mượn trả</h3>
              <p>Phê duyệt và bàn giao</p>
            </div>
            <div class="stat-box">
              <h3>Bảo trì</h3>
              <p>Lịch định kỳ và lịch sử</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Nửa phải: Form đăng nhập (White side) -->
    <div class="right-side">
      <div class="right-content">
        <div class="mobile-brand">
          <experiment-outlined />
          <span>LabManagement</span>
          <small>Phòng Lab IoT · Khoa CNTT</small>
        </div>
        <div class="login-header">
          <h2>{{ $t('login.welcomeBack') }}</h2>
          <p>{{ $t('login.loginToContinue') }}</p>
        </div>

        <a-form :model="formState" @finish="handleLogin" layout="vertical" class="login-form">
          <!-- Tài khoản -->
          <a-form-item 
            :label="$t('login.account')" 
            name="username" 
            :rules="[{ required: true, message: $t('login.accountRequired') }]"
          >
            <a-input v-model:value="formState.username" :placeholder="$t('login.accountPlaceholder')" size="large" />
          </a-form-item>

          <!-- Mật khẩu -->
          <a-form-item 
            :label="$t('login.password')"
            name="password" 
            :rules="[{ required: true, message: $t('login.passwordRequired') }]"
          >
            <a-input-password v-model:value="formState.password" placeholder="••••••••" size="large" />
          </a-form-item>

          <!-- Ghi nhớ & Quên mật khẩu -->
          <div class="form-actions" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
            <a-checkbox v-model:checked="formState.remember">{{ $t('login.rememberMe') }}</a-checkbox>
            <router-link to="/forgot-password" class="forgot-link">{{ $t('login.forgotPassword') }}</router-link>
          </div>

          <!-- Nút Submit -->
          <a-button type="primary" html-type="submit" :loading="loading" block size="large" class="submit-btn">
            {{ loading ? $t('login.loggingIn') : $t('login.loginBtn') }} <arrow-right-outlined />
          </a-button>

          <!-- Liên kết phụ -->
          <div class="extra-links">
            {{ $t('login.noAccount') }} <span class="contact-admin">{{ $t('login.contactAdmin') }}</span>
            <br><br>
            <router-link to="/" class="back-link">Trang chủ</router-link>
          </div>
        </a-form>

        <div class="right-footer">
          <div class="footer-links">Tài khoản và dữ liệu được quản lý theo phân quyền của hệ thống.</div>
          <div class="copyright">© 2026 LabManagement — Phòng Lab IoT, Khoa Công nghệ Thông tin.</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { 
  ExperimentOutlined, 
  DesktopOutlined, 
  HistoryOutlined, 
  AppstoreOutlined, 
  ToolOutlined,
  ArrowRightOutlined
} from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const formState = reactive({
  username: '',
  password: '',
  remember: false
})
const loading = ref(false)

const handleLogin = () => {
  loading.value = true
  authStore.login(formState.username, formState.password, formState.remember)
    .then(() => {
      message.success(t('login.loginSuccess'))
      router.push('/dashboard')
    })
    .catch(err => {
      message.error(err.message)
    })
    .finally(() => {
      loading.value = false
    })
}
</script>

<style scoped>
.split-layout {
  display: flex;
  height: 100vh;
  width: 100%;
  font-family: 'Inter', sans-serif;
  overflow: hidden;
}

/* Nửa trái (Dark) */
.left-side {
  width: 55%;
  flex: none;
  min-width: 0;
  overflow-y: auto;
  background: var(--color-surface-dark);
  color: var(--color-on-dark);
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 40px;
}
.left-content {
  max-width: 600px;
  width: 100%;
}
.brand-header {
  margin-bottom: 40px;
}
.logo {
  font-size: 24px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
}
.platform-tag {
  font-size: 14px;
  font-weight: 400;
  opacity: 0.8;
  border-left: 1px solid rgba(255,255,255,0.3);
  padding-left: 12px;
  margin-left: 4px;
}
.tag-pill {
  display: inline-block;
  background: rgba(255, 255, 255, 0.1);
  padding: 6px 16px;
  border-radius: 20px;
  font-size: 13px;
  border: 1px solid rgba(255, 255, 255, 0.2);
}
.hero-title {
  font-family: var(--font-serif);
  font-size: clamp(36px, 4vw, 48px);
  font-weight: 400;
  letter-spacing: -0.02em;
  line-height: 1.3;
  color: var(--color-on-dark);
  margin-bottom: 24px;
}
.text-coral {
  color: var(--color-primary);
}
.hero-desc {
  font-size: 16px;
  line-height: 1.6;
  opacity: 0.9;
  margin-bottom: 40px;
  max-width: 500px;
}
.features-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px 32px;
  margin-bottom: 48px;
}
.feature-item {
  display: flex;
  gap: 16px;
}
.feature-icon {
  width: 40px;
  height: 40px;
  background: rgba(255,255,255,0.1);
  border-radius: 10px;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 20px;
  flex-shrink: 0;
}
.feature-text h4 {
  color: white;
  margin: 0 0 4px 0;
  font-size: 15px;
  font-weight: 600;
}
.feature-text p {
  color: rgba(255,255,255,0.7);
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
}
.stats-section {
  border-top: 1px solid rgba(255,255,255,0.15);
  padding-top: 24px;
}
.stats-title {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 1px;
  opacity: 0.7;
  margin-bottom: 16px;
  text-transform: uppercase;
}
.stats-row {
  display: flex;
  justify-content: space-between;
}
.stat-box h3 {
  color: white;
  font-size: 24px;
  font-weight: 800;
  margin: 0 0 4px 0;
}
.stat-box p {
  color: rgba(255,255,255,0.7);
  font-size: 12px;
  margin: 0;
}

/* Nửa phải (Cream) */
.right-side {
  width: 45%;
  flex: none;
  min-width: 0;
  overflow-y: auto;
  background: var(--color-canvas-cream);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  position: relative;
}
.right-content {
  width: 100%;
  max-width: 360px;
  padding: 40px 0;
}
.login-header {
  text-align: center;
  margin-bottom: 40px;
}
.login-header h2 {
  font-size: 28px;
  font-weight: 700;
  color: #111827;
  margin-bottom: 8px;
}
.login-header p {
  color: #6b7280;
  font-size: 14px;
}
.login-form :deep(.ant-form-item-label > label) {
  font-weight: 500;
  color: #374151;
}
.password-label {
  display: flex;
  justify-content: space-between;
  width: 100%;
}
.forgot-link {
  color: var(--color-primary);
  font-size: 13px;
  font-weight: 500;
}
.forgot-link:hover {
  text-decoration: underline;
}
.form-actions {
  margin-bottom: 24px;
}
.submit-btn {
  background-color: var(--color-primary);
  border-color: var(--color-primary);
  border-radius: 6px;
  font-weight: 600;
  height: 44px;
}
.submit-btn:hover {
  background-color: var(--color-primary-hover);
  border-color: var(--color-primary-hover);
}
.extra-links {
  text-align: center;
  margin-top: 24px;
  font-size: 14px;
  color: #6b7280;
}
.contact-admin {
  color: #111827;
  font-weight: 600;
}
.back-link {
  color: var(--color-primary);
  font-weight: 500;
}
.back-link:hover {
  text-decoration: underline;
}
.right-footer {
  position: absolute;
  bottom: 24px;
  left: 0;
  width: 100%;
  text-align: center;
}
.footer-links {
  font-size: 12px;
  color: #9ca3af;
  margin-bottom: 8px;
}
.footer-links a {
  color: #9ca3af;
}
.footer-links a:hover {
  color: #6b7280;
}
.divider {
  margin: 0 8px;
}
.copyright {
  font-size: 12px;
  color: #d1d5db;
}

/* Responsive */
@media (max-width: 992px) {
  .left-side { display: none; }
  .right-side { width: 100%; }
}

/* Public blue theme */
.split-layout {
  min-height: 100dvh;
  height: auto;
  overflow: auto;
  background: var(--public-background);
  color: var(--public-heading);
}
.left-side {
  position: relative;
  isolation: isolate;
  background: linear-gradient(135deg, var(--public-navy-start), var(--public-navy-end));
  overflow: hidden;
  padding: clamp(32px, 5vw, 72px);
}
.left-side::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: -1;
  opacity: 0.22;
  background-image:
    linear-gradient(rgba(255,255,255,0.08) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255,255,255,0.08) 1px, transparent 1px),
    radial-gradient(circle at 78% 20%, rgba(255,255,255,0.22), transparent 26%);
  background-size: 32px 32px, 32px 32px, auto;
}
.left-content { position: relative; z-index: 1; max-width: 620px; }
.brand-header { margin-bottom: 42px; }
.logo { color: #fff; }
.logo svg { color: var(--public-coral); }
.platform-tag { color: rgba(255,255,255,0.82); border-left-color: rgba(255,255,255,0.3); }
.tag-pill { background: rgba(255,255,255,0.1); border-color: rgba(255,255,255,0.22); color: rgba(255,255,255,0.92); }
.hero-title {
  font-family: var(--font-sans);
  font-size: clamp(34px, 4vw, 50px);
  font-weight: 750;
  letter-spacing: -0.035em;
  line-height: 1.12;
  color: #fff;
  margin-bottom: 22px;
}
.hero-title-line { display: block; }
.hero-title-brand {
  display: flex;
  align-items: baseline;
  gap: 16px;
  flex-wrap: nowrap;
  white-space: nowrap;
}
.hero-title-iot, .text-coral { color: #fff; }
.hero-title-brand .text-coral { color: var(--public-coral); }
.hero-desc { color: rgba(255,255,255,0.82); max-width: 540px; }
.features-grid { gap: 16px; margin-bottom: 40px; }
.feature-item {
  min-height: 86px;
  padding: 16px;
  align-items: flex-start;
  background: rgba(255,255,255,0.08);
  border: 1px solid rgba(255,255,255,0.14);
  border-radius: 14px;
}
.feature-icon { background: rgba(255,255,255,0.14); color: #fff; }
.feature-text h4 { color: #fff; }
.feature-text p { color: rgba(255,255,255,0.72); }
.stats-section { border-top-color: rgba(255,255,255,0.2); }
.stats-title { color: rgba(255,255,255,0.78); }
.stat-box h3 { color: #fff; }
.stat-box p { color: rgba(255,255,255,0.68); }
.right-side {
  background: var(--public-background);
  padding: clamp(24px, 4vw, 64px);
}
.right-content {
  max-width: 440px;
  padding: 40px;
  background: var(--public-card);
  border: 1px solid #E2E8F0;
  border-radius: 24px;
  box-shadow: 0 20px 55px rgba(15,58,90,0.12);
}
.mobile-brand { display: none; }
.login-header { margin-bottom: 30px; }
.login-header h2 { color: var(--public-heading); }
.login-header p { color: var(--public-muted); }
.login-form :deep(.ant-form-item-label > label) { color: var(--public-heading); }
.login-form :deep(.ant-input),
.login-form :deep(.ant-input-affix-wrapper) {
  border-color: #CBD5E1;
  border-radius: 10px;
  color: var(--public-heading);
  background: #fff;
}
.login-form :deep(.ant-input:hover),
.login-form :deep(.ant-input-affix-wrapper:hover),
.login-form :deep(.ant-input-affix-wrapper-focused) {
  border-color: var(--public-blue);
}
.forgot-link, .back-link { color: var(--public-blue); }
.contact-admin { color: var(--public-heading); }
.submit-btn {
  background: var(--public-coral) !important;
  border-color: var(--public-coral) !important;
  border-radius: 10px;
  height: 48px;
}
.submit-btn:hover { background: #C96345 !important; border-color: #C96345 !important; }
.extra-links { color: var(--public-muted); }
.right-footer { position: static; margin-top: 28px; }
.footer-links, .copyright { color: var(--public-muted); }

@media (max-width: 992px) {
  .right-side { min-height: 100dvh; padding: 24px; }
  .right-content { max-width: 460px; }
  .mobile-brand {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    flex-wrap: wrap;
    margin-bottom: 28px;
    color: var(--public-heading);
    font-size: 20px;
    font-weight: 750;
  }
  .mobile-brand svg { color: var(--public-coral); }
  .mobile-brand small { flex-basis: 100%; text-align: center; color: var(--public-muted); font-size: 12px; font-weight: 500; }
}
@media (max-width: 520px) {
  .right-side { padding: 14px; }
  .right-content { padding: 28px 22px; border-radius: 18px; }
  .hero-title-brand { gap: 12px; font-size: 0.92em; }
  .features-grid { grid-template-columns: 1fr; }
  .stats-row { gap: 14px; flex-wrap: wrap; }
  .stat-box { flex: 1 1 40%; }
  .right-footer { margin-top: 24px; }
}
</style>
