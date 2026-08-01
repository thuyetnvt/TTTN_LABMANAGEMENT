<template>
  <div class="reset-page">
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
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
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
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 24px;
  background: #f4f5f7;
}

.reset-card {
  width: 100%;
  max-width: 440px;
  border-radius: 12px;
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.08);
}

.alert {
  margin-bottom: 20px;
}

.back-link {
  margin-top: 20px;
  text-align: center;
}
</style>
