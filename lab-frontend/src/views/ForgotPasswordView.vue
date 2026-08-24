<template>
  <div class="split-layout">
    <div class="left-side">
      <div class="left-content">
        <div class="brand-header">
          <div class="logo">
            <experiment-outlined />
            <span>LabManagement</span>
            <span class="platform-tag">Nền tảng</span>
          </div>
          <div class="tag-pill">{{ $t('hero.brandTag') }}</div>
        </div>

        <h1 class="hero-title">
          {{ $t('hero.title1') }} {{ $t('hero.title2') }}<br />
          <span class="text-coral">{{ $t('hero.title3') }}</span>
        </h1>
        <p class="hero-desc">{{ $t('hero.desc') }}</p>

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

        <div class="stats-section">
          <div class="stats-title">{{ $t('hero.statsTitle') }}</div>
          <div class="stats-row">
            <div class="stat-box">
              <h3>IoT</h3>
              <p>Siêu dữ liệu thiết bị</p>
            </div>
            <div class="stat-box">
              <h3>QR</h3>
              <p>Quét và truy vết tài sản</p>
            </div>
            <div class="stat-box">
              <h3>360°</h3>
              <p>Bàn giao và kiểm kê</p>
            </div>
            <div class="stat-box">
              <h3>API</h3>
              <p>Luồng nghiệp vụ thật</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="right-side">
      <div class="right-content">
        <div class="login-header">
          <h2>{{ $t('forgot.title') }}</h2>
          <p>{{ $t('forgot.desc') }}</p>
        </div>

        <a-form :model="formState" @finish="handleResetPassword" layout="vertical" class="login-form">
          <a-form-item
            :label="$t('forgot.email')"
            name="email"
            :rules="[
              { required: true, message: $t('forgot.emailRequired') },
              { type: 'email', message: $t('forgot.emailInvalid') }
            ]"
          >
            <a-input v-model:value="formState.email" :placeholder="$t('forgot.emailPlaceholder')" size="large" />
          </a-form-item>

          <a-button type="primary" html-type="submit" :loading="isLoading" block size="large" class="submit-btn">
            {{ isLoading ? $t('forgot.sending') : $t('forgot.submit') }}
            <arrow-right-outlined />
          </a-button>

          <div class="extra-links">
            <router-link to="/login" class="back-link">{{ $t('forgot.back') }}</router-link>
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
          <div class="copyright">© 2026 LabManagement. Quản lý phòng thí nghiệm và tài sản.</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import authApi from '../api/authApi'
import {
  ExperimentOutlined,
  DesktopOutlined,
  HistoryOutlined,
  AppstoreOutlined,
  ToolOutlined,
  ArrowRightOutlined
} from '@ant-design/icons-vue'

const router = useRouter()
const { t } = useI18n()
const isLoading = ref(false)

const formState = reactive({
  email: '',
})

const handleResetPassword = async () => {
  isLoading.value = true
  try {
    await authApi.forgotPassword({ email: formState.email })
    message.success(t('forgot.success'))
    router.push('/login')
  } catch (error) {
    message.error(error?.response?.data?.message || 'Không thể gửi yêu cầu đặt lại mật khẩu.')
  } finally {
    isLoading.value = false
  }
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

.left-side {
  width: 55%;
  flex: none;
  min-width: 0;
  overflow-y: hidden;
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
  border-left: 1px solid rgba(255, 255, 255, 0.3);
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
  background: rgba(255, 255, 255, 0.1);
  border-radius: 10px;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 20px;
  flex-shrink: 0;
}

.feature-text h4 {
  color: white;
  margin: 0 0 4px;
  font-size: 15px;
  font-weight: 600;
}

.feature-text p {
  color: rgba(255, 255, 255, 0.7);
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
}

.stats-section {
  border-top: 1px solid rgba(255, 255, 255, 0.15);
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
  margin: 0 0 4px;
}

.stat-box p {
  color: rgba(255, 255, 255, 0.7);
  font-size: 12px;
  margin: 0;
}

.right-side {
  width: 45%;
  flex: none;
  min-width: 0;
  overflow-y: hidden;
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

.submit-btn {
  background-color: var(--color-primary);
  border-color: var(--color-primary);
  border-radius: 8px;
  font-weight: 600;
  height: 46px;
  margin-top: 16px;
}

.submit-btn:hover {
  background-color: var(--color-primary-hover);
  border-color: var(--color-primary-hover);
}

.extra-links {
  text-align: center;
  margin-top: 24px;
  font-size: 14px;
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

@media (max-width: 992px) {
  .left-side {
    display: none;
  }

  .right-side {
    width: 100%;
  }
}
</style>
