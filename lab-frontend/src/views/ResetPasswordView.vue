<template>
  <div class="reset-page">
    <div class="reset-shell">
      <div class="reset-brand">
        <experiment-outlined />
        <span>LabManagement</span>
        <small>Phòng Lab IoT · Khoa CNTT</small>
      </div>
      <a-card title="Đặt lại mật khẩu" class="reset-card" :bordered="false">
      <a-alert
        v-if="!token"
        type="error"
        show-icon
        message="Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu token."
        class="alert"
      />

      <a-form v-else :model="formState" layout="vertical" @finish="handleSubmit">
        <a-form-item
          label="Mật khẩu mới"
          name="password"
          :rules="[
            { required: true, message: 'Vui lòng nhập mật khẩu mới' },
            { min: 8, message: 'Mật khẩu phải có ít nhất 8 ký tự' }
          ]"
        >
          <a-input-password v-model:value="formState.password" size="large" />
        </a-form-item>

        <a-form-item
          label="Xác nhận mật khẩu"
          name="confirmPassword"
          :rules="[{ validator: validateConfirmation }]"
        >
          <a-input-password v-model:value="formState.confirmPassword" size="large" />
        </a-form-item>

        <a-button type="primary" html-type="submit" block size="large" :loading="loading">
          Cập nhật mật khẩu
        </a-button>
      </a-form>

      <div class="back-link">
        <router-link to="/login">Quay lại đăng nhập</router-link>
      </div>
      </a-card>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { ExperimentOutlined } from '@ant-design/icons-vue'
import authApi from '../api/authApi'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const token = computed(() => String(route.query.token || ''))
const formState = reactive({ password: '', confirmPassword: '' })

const validateConfirmation = async (_rule, value) => {
  if (!value) throw new Error('Vui lòng xác nhận mật khẩu')
  if (value !== formState.password) throw new Error('Mật khẩu xác nhận không khớp')
}

const handleSubmit = async () => {
  loading.value = true
  try {
    await authApi.resetPassword({ token: token.value, newPassword: formState.password })
    message.success('Đặt lại mật khẩu thành công.')
    router.push('/login')
  } catch (error) {
    message.error(error?.response?.data?.message || 'Không thể đặt lại mật khẩu.')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.reset-page {
  min-height: 100dvh;
  display: grid;
  place-items: center;
  padding: 24px;
  background:
    linear-gradient(rgba(31, 122, 154, 0.045) 1px, transparent 1px),
    linear-gradient(90deg, rgba(31, 122, 154, 0.045) 1px, transparent 1px),
    linear-gradient(135deg, #fff 0%, var(--public-background) 100%);
  background-size: 36px 36px, 36px 36px, auto;
}

.reset-shell {
  width: 100%;
  max-width: 500px;
}

.reset-brand {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 18px;
  color: var(--public-heading);
  font-size: 20px;
  font-weight: 750;
}

.reset-brand svg { color: var(--public-coral); }

.reset-brand small {
  flex-basis: 100%;
  color: var(--public-muted);
  font-size: 12px;
  font-weight: 500;
  text-align: center;
}

.reset-card {
  width: 100%;
  border: 1px solid #E2E8F0;
  border-radius: 20px;
  background: var(--public-card);
  box-shadow: 0 20px 55px rgba(15, 58, 90, 0.12);
}

.reset-card :deep(.ant-card-head-title) {
  color: var(--public-heading);
  font-size: 26px;
}

.reset-card :deep(.ant-form-item-label > label) { color: var(--public-heading); }

.reset-card :deep(.ant-input-affix-wrapper) {
  border-color: #CBD5E1;
  border-radius: 10px;
}

.reset-card :deep(.ant-input-affix-wrapper:hover),
.reset-card :deep(.ant-input-affix-wrapper-focused) { border-color: var(--public-blue); }

.reset-card :deep(.ant-btn-primary) {
  background: var(--public-coral) !important;
  border-color: var(--public-coral) !important;
  border-radius: 10px;
}

.reset-card :deep(.ant-btn-primary:hover) {
  background: #C96345 !important;
  border-color: #C96345 !important;
}

.alert {
  margin-bottom: 20px;
}

.back-link {
  margin-top: 20px;
  text-align: center;
}

.back-link a { color: var(--public-blue); }

@media (max-width: 480px) {
  .reset-page { padding: 14px; }
  .reset-card { border-radius: 18px; }
  .reset-card :deep(.ant-card-body) { padding: 22px; }
}
</style>
