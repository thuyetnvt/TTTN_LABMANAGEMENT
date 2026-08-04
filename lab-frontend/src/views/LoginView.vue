<template>
  <div class="split-layout">
    <!-- Nửa trái: Thông tin giới thiệu (Blue side) -->
    <div class="left-side">
      <div class="left-content">
        <!-- Header -->
        <div class="brand-header">
          <div class="logo">
            <experiment-outlined /> <span>LabManagement</span>
            <span class="platform-tag">Platform</span>
          </div>
          <div class="tag-pill">{{ $t('hero.brandTag') }}</div>
        </div>

        <!-- Hero Text -->
        <h1 class="hero-title">
          {{ $t('hero.title1') }} {{ $t('hero.title2') }}<br />
          <span class="text-yellow">{{ $t('hero.title3') }}</span>
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
              <h3>500+</h3>
              <p>{{ $t('hero.stat1Desc') }}</p>
            </div>
            <div class="stat-box">
              <h3>1.200+</h3>
              <p>{{ $t('hero.stat2Desc') }}</p>
            </div>
            <div class="stat-box">
              <h3>99,9%</h3>
              <p>{{ $t('hero.stat3Desc') }}</p>
            </div>
            <div class="stat-box">
              <h3>10x</h3>
              <p>{{ $t('hero.stat4Desc') }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Nửa phải: Form đăng nhập (White side) -->
    <div class="right-side">
      <div class="right-content">
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
            {{ $t('login.noAccount') }} <a href="mailto:support@labmanagement.edu.vn" class="contact-admin">{{ $t('login.contactAdmin') }}</a>
            <br><br>
            <router-link to="/" class="back-link">Home</router-link>
          </div>
        </a-form>

        <div class="right-footer">
          <div class="footer-links">
            <a href="#">Chính sách bảo mật</a>
            <span class="divider">|</span>
            <a href="#">Trợ giúp</a>
            <span class="divider">|</span>
            <a href="#">Điều khoản sử dụng</a>
          </div>
          <div class="copyright">© 2026 LabManagement. All rights reserved.</div>
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

/* Nửa trái (Blue) */
.left-side {
  width: 55%;
  flex: none;
  min-width: 0;
  overflow-y: auto;
  background: linear-gradient(135deg, rgba(28,32,156,0.85) 0%, rgba(39,44,212,0.92) 100%), url('/lab-bg.png') no-repeat center center;
  background-size: cover;
  color: white;
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
  font-size: clamp(36px, 4vw, 48px);
  font-weight: 700;
  line-height: 1.3;
  color: white;
  margin-bottom: 24px;
}
.text-yellow {
  color: #ffde59;
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

/* Nửa phải (White) */
.right-side {
  width: 45%;
  flex: none;
  min-width: 0;
  overflow-y: auto;
  background: white;
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
  color: #272cd4;
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
  background-color: #272cd4;
  border-color: #272cd4;
  border-radius: 6px;
  font-weight: 600;
  height: 44px;
}
.submit-btn:hover {
  background-color: #1c209c;
  border-color: #1c209c;
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
  color: #272cd4;
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
</style>
