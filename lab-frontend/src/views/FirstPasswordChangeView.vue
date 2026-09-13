<template>
  <main class="first-password-page">
    <a-card title="Đổi mật khẩu lần đầu">
      <p>Đặt mật khẩu riêng trước khi sử dụng hệ thống. Mật khẩu cần ít nhất 8 ký tự, có chữ hoa, chữ thường và số.</p>
      <a-form layout="vertical" @finish="submit">
        <a-form-item label="Mật khẩu hiện tại" required><a-input-password v-model:value="currentPassword" autocomplete="current-password" /></a-form-item>
        <a-form-item label="Mật khẩu mới" required><a-input-password v-model:value="newPassword" autocomplete="new-password" /></a-form-item>
        <a-form-item label="Nhập lại mật khẩu mới" required><a-input-password v-model:value="confirmation" autocomplete="new-password" /></a-form-item>
        <a-button type="primary" html-type="submit" :loading="saving">Đổi mật khẩu</a-button>
        <a-button type="link" :disabled="saving" @click="logout">Đăng xuất</a-button>
      </a-form>
    </a-card>
  </main>
</template>
<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { useAuthStore } from '../stores/authStore'
import { userApi } from '../api/userApi'
import { getApiErrorMessage } from '../utils/apiError'
const router = useRouter()
const auth = useAuthStore()
const currentPassword = ref('')
const newPassword = ref('')
const confirmation = ref('')
const saving = ref(false)
const logout = () => { auth.logout(); router.replace({ name: 'Login' }) }
const submit = async () => {
  if (saving.value) return
  if (!currentPassword.value || newPassword.value.length < 8 || !/[a-z]/.test(newPassword.value) || !/[A-Z]/.test(newPassword.value) || !/[0-9]/.test(newPassword.value)) {
    message.warning('Nhập mật khẩu hiện tại và mật khẩu mới đủ 8 ký tự, có chữ hoa, chữ thường và số.'); return
  }
  if (newPassword.value !== confirmation.value) { message.warning('Mật khẩu nhập lại không khớp.'); return }
  saving.value = true
  try {
    await userApi.changePassword({ currentPassword: currentPassword.value, newPassword: newPassword.value })
    message.success('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.')
    logout()
  } catch (error) { message.error(getApiErrorMessage(error, 'Không thể đổi mật khẩu.')) }
  finally { saving.value = false }
}
</script>
<style scoped>
.first-password-page { max-width: 520px; margin: 8vh auto; padding: 16px; }
</style>
