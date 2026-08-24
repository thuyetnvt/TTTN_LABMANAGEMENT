<template>
  <div class="profile-page">
    <div class="page-title"><h2>Hồ sơ cá nhân</h2><p>Cập nhật thông tin liên hệ và định danh của bạn.</p></div>
    <a-card :loading="loading" :bordered="false">
      <a-descriptions bordered :column="{ xs: 1, sm: 2 }" class="identity">
        <a-descriptions-item label="Tài khoản">{{ profile.username || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Vai trò">{{ roleLabel(profile.role) }}</a-descriptions-item>
      </a-descriptions>
      <a-form layout="vertical" class="profile-form" @submit.prevent="save">
        <a-row :gutter="16">
          <a-col :xs="24" :sm="12"><a-form-item label="Họ và tên"><a-input v-model:value="profile.fullName" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Mã sinh viên/mã cán bộ"><a-input v-model:value="profile.universityCode" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Email"><a-input v-model:value="profile.email" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Số điện thoại"><a-input v-model:value="profile.phone" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Khoa/bộ môn"><a-input v-model:value="profile.department" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Lớp"><a-input v-model:value="profile.className" /></a-form-item></a-col>
        </a-row>
        <a-button type="primary" html-type="submit" :loading="saving">Lưu hồ sơ</a-button>
      </a-form>
    </a-card>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { message } from 'ant-design-vue'
import { userApi } from '../api/userApi'
import { roleLabel } from '../constants/business'

const loading = ref(false)
const saving = ref(false)
const profile = reactive({ username: '', role: '', email: '', fullName: '', universityCode: '', phone: '', department: '', className: '' })

const load = async () => {
  loading.value = true
  try { Object.assign(profile, await userApi.getMe()) }
  catch (error) { message.error(error?.response?.data?.message || 'Không tải được hồ sơ cá nhân.') }
  finally { loading.value = false }
}

const save = async () => {
  saving.value = true
  try {
    await userApi.updateMe({ email: profile.email, fullName: profile.fullName, universityCode: profile.universityCode, phone: profile.phone, department: profile.department, className: profile.className })
    message.success('Đã cập nhật hồ sơ cá nhân.')
  } catch (error) { message.error(error?.response?.data?.message || 'Không thể cập nhật hồ sơ cá nhân.') }
  finally { saving.value = false }
}

onMounted(load)
</script>

<style scoped>
.profile-page { max-width: 960px; margin: 0 auto; }
.page-title { margin-bottom: 20px; }
.page-title h2 { margin: 0; }
.page-title p { color: var(--color-text-secondary); margin: 6px 0 0; }
.profile-form { margin-top: 24px; }
</style>
