<template>
  <div class="profile-page">
    <div class="page-title">
      <h2>Hồ sơ cá nhân</h2>
      <p>Cập nhật thông tin liên hệ, định danh và ảnh đại diện của bạn.</p>
    </div>
    <a-card :loading="loading" :bordered="false" class="profile-card">
      <div class="profile-layout">
        <section class="profile-summary" aria-labelledby="profile-summary-title">
          <UserAvatar :name="profile.fullName || profile.username" :avatar-url="avatarPreviewUrl || avatarUrl" :avatar-updated-at="avatarPreviewUrl ? previewVersion : profile.avatarUpdatedAt" :size="120" class="profile-avatar" :loading="avatarUploading" />
          <h3 id="profile-summary-title">{{ profile.fullName || 'Chưa cập nhật họ tên' }}</h3>
          <p class="profile-username">@{{ profile.username || '—' }}</p>
          <a-tag color="orange">{{ roleLabel(profile.role) }}</a-tag>
          <div class="avatar-actions">
            <a-upload accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp" :show-upload-list="false" :before-upload="handleAvatarBeforeUpload">
              <a-button type="primary" shape="round" class="btn-change-avatar" :loading="avatarUploading">
                <template #icon><CameraOutlined /></template>
                Đổi ảnh
              </a-button>
            </a-upload>
            <a-button v-if="profile.hasAvatar" type="default" danger shape="round" class="btn-delete-avatar" :disabled="avatarUploading" @click="deleteAvatarDialogOpen = true">
              <template #icon><DeleteOutlined /></template>
              Xóa ảnh
            </a-button>
          </div>
          <span class="avatar-hint">JPG, PNG hoặc WebP · tối đa 2 MB</span>
        </section>
        <section class="profile-details">

          <a-form layout="vertical" class="profile-form" @submit.prevent="save">
            <a-row :gutter="[18, 0]">
              <a-col :xs="24" :md="12"><a-form-item label="Họ và tên"><a-input v-model:value="profile.fullName" /></a-form-item></a-col>
              <a-col :xs="24" :md="12"><a-form-item :label="universityCodeLabel"><a-input v-model:value="profile.universityCode" /></a-form-item></a-col>
              <a-col :xs="24" :md="12"><a-form-item label="Email"><a-input v-model:value="profile.email" /></a-form-item></a-col>
              <a-col :xs="24" :md="12"><a-form-item label="Số điện thoại"><a-input v-model:value="profile.phone" /></a-form-item></a-col>
              <a-col :xs="24" :md="12"><a-form-item :label="departmentLabel"><a-input v-model:value="profile.department" /></a-form-item></a-col>
              <a-col v-if="isStudent" :xs="24" :md="12"><a-form-item label="Lớp"><a-input v-model:value="profile.className" /></a-form-item></a-col>
            </a-row>
            <a-button type="primary" html-type="submit" :loading="saving">Lưu hồ sơ</a-button>
          </a-form>
        </section>
      </div>
    </a-card>
    <ConfirmDialog v-model:open="deleteAvatarDialogOpen" title="Xóa ảnh đại diện" message="Bạn có chắc muốn xóa ảnh đại diện hiện tại không?" ok-text="Xóa ảnh" ok-type="danger" :loading="deletingAvatar" @confirm="deleteAvatar" />
    <ImageCropperModal v-model:open="cropperOpen" :image-url="rawImageUrl" :saving="avatarUploading" @crop="handleCrop" />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { message } from 'ant-design-vue'
import { CameraOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import { userApi } from '../api/userApi'
import { isStudentRole, roleLabel } from '../constants/business'
import { useAuthStore } from '../stores/authStore'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import UserAvatar from '../components/UserAvatar.vue'
import ImageCropperModal from '../components/ImageCropperModal.vue'

const authStore = useAuthStore()
const loading = ref(false)
const saving = ref(false)
const avatarUploading = ref(false)
const deletingAvatar = ref(false)
const deleteAvatarDialogOpen = ref(false)
const avatarPreviewUrl = ref('')
const previewVersion = ref('preview')
const cropperOpen = ref(false)
const rawImageUrl = ref('')
const rawImageFile = ref(null)
const profile = reactive({ username: '', role: '', email: '', fullName: '', universityCode: '', phone: '', department: '', className: '', hasAvatar: false, avatarUpdatedAt: '' })
const isStudent = computed(() => isStudentRole(profile.role))
const universityCodeLabel = computed(() => isStudent.value ? 'Mã sinh viên' : 'Mã cán bộ')
const departmentLabel = computed(() => isStudent.value ? 'Khoa/ngành' : 'Khoa/bộ môn hoặc đơn vị')
const avatarUrl = computed(() => profile.hasAvatar ? userApi.avatarUrl() : '')

const applyProfile = (data) => { if (data) { Object.assign(profile, data); authStore.setUser(data) } }
const load = async () => {
  loading.value = true
  try { applyProfile(await userApi.getMe()) }
  catch (error) { message.error(error?.message || 'Không tải được hồ sơ cá nhân.') }
  finally { loading.value = false }
}
const save = async () => {
  if (isStudent.value && !profile.className?.trim()) {
    message.warning('Vui lòng nhập lớp.')
    return
  }
  saving.value = true
  try {
    await userApi.updateMe({ email: profile.email, fullName: profile.fullName, universityCode: profile.universityCode, phone: profile.phone, department: profile.department, className: profile.className })
    applyProfile(await userApi.getMe())
    message.success('Đã cập nhật hồ sơ cá nhân.')
  } catch (error) { message.error(error?.message || 'Không thể cập nhật hồ sơ cá nhân.') }
  finally { saving.value = false }
}

const handleAvatarBeforeUpload = (file) => {
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 2 * 1024 * 1024) {
    message.error('Chỉ nhận ảnh JPG, PNG hoặc WebP tối đa 2 MB.'); return false
  }
  if (rawImageUrl.value) URL.revokeObjectURL(rawImageUrl.value)
  rawImageFile.value = file
  rawImageUrl.value = URL.createObjectURL(file)
  cropperOpen.value = true
  return false
}

const handleCrop = async (croppedBlob) => {
  avatarUploading.value = true
  try {
    const croppedFile = new File([croppedBlob], 'avatar.jpg', { type: 'image/jpeg' })
    if (avatarPreviewUrl.value) URL.revokeObjectURL(avatarPreviewUrl.value)
    avatarPreviewUrl.value = URL.createObjectURL(croppedFile); previewVersion.value = `${Date.now()}`
    const state = await userApi.uploadAvatar(croppedFile)
    profile.hasAvatar = state?.hasAvatar ?? true; profile.avatarUpdatedAt = state?.avatarUpdatedAt || new Date().toISOString(); authStore.setUser(profile)
    URL.revokeObjectURL(avatarPreviewUrl.value); avatarPreviewUrl.value = ''
    message.success('Đã cập nhật ảnh đại diện.')
    cropperOpen.value = false
  } catch (error) {
    message.error(error?.message || 'Không thể cập nhật ảnh đại diện.')
  } finally { avatarUploading.value = false }
}
const deleteAvatar = async () => {
  deletingAvatar.value = true
  try {
    await userApi.deleteAvatar(); profile.hasAvatar = false; profile.avatarUpdatedAt = ''; authStore.setUser(profile)
    if (avatarPreviewUrl.value) URL.revokeObjectURL(avatarPreviewUrl.value); avatarPreviewUrl.value = ''
    deleteAvatarDialogOpen.value = false; message.success('Đã xóa ảnh đại diện.')
  } catch (error) { message.error(error?.message || 'Không thể xóa ảnh đại diện.') }
  finally { deletingAvatar.value = false }
}
onMounted(load)
onBeforeUnmount(() => { 
  if (avatarPreviewUrl.value) URL.revokeObjectURL(avatarPreviewUrl.value)
  if (rawImageUrl.value) URL.revokeObjectURL(rawImageUrl.value)
})
</script>

<style scoped>
.profile-page { max-width: 1120px; margin: 0 auto; }
.page-title { margin-bottom: 20px; }
.page-title h2 { margin: 0; }
.page-title p { color: var(--color-text-secondary); margin: 6px 0 0; }
.profile-card { border-radius: 14px; }
.profile-layout { display: grid; grid-template-columns: 280px minmax(0, 1fr); gap: 38px; }
.profile-summary { display: flex; flex-direction: column; align-items: center; text-align: center; padding: 10px 12px 20px; border-right: 1px solid var(--color-border, #e5e7eb); }
.profile-avatar { margin: 8px 0 16px; box-shadow: 0 8px 24px rgba(217, 119, 87, .2); }
.profile-summary h3 { margin: 0; font-size: 19px; }
.profile-username { margin: 4px 0 10px; color: var(--color-text-secondary); }
.avatar-actions { display: flex; align-items: center; gap: 12px; margin-top: 24px; }
.btn-change-avatar { background: linear-gradient(135deg, var(--color-primary), #5a9ce2); border: none; box-shadow: 0 4px 10px rgba(35, 118, 197, 0.25); font-weight: 500; transition: all 0.2s ease; }
.btn-change-avatar:hover { transform: translateY(-1px); box-shadow: 0 6px 14px rgba(35, 118, 197, 0.35); }
.btn-delete-avatar { border-color: #fca5a5; color: #dc2626; font-weight: 500; transition: all 0.2s ease; }
.btn-delete-avatar:hover { background: #fef2f2; border-color: #ef4444; transform: translateY(-1px); color: #dc2626; }
.avatar-hint { margin-top: 10px; color: var(--color-text-secondary); font-size: 12px; }
.profile-details { min-width: 0; }

.profile-form { margin-top: 6px; }
@media (max-width: 767px) {
  .profile-layout { grid-template-columns: 1fr; gap: 24px; }
  .profile-summary { border-right: 0; border-bottom: 1px solid var(--color-border, #e5e7eb); padding-bottom: 24px; }

}
</style>
