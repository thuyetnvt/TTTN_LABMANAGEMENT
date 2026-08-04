<template>
  <a-form :model="formState" layout="vertical" ref="formRef">
    <a-row :gutter="16">
      <a-col :xs="24" :sm="12">
        <a-form-item label="Tài khoản" name="username" :rules="[{ required: true, message: 'Vui lòng nhập tài khoản!' }]">
          <a-input v-model:value="formState.username" placeholder="Nhập tên tài khoản..." :disabled="isProtected" />
        </a-form-item>
      </a-col>
      <a-col :xs="24" :sm="12">
        <a-form-item label="Email" name="email" :rules="[{ type: 'email', message: 'Email không hợp lệ!' }]">
          <a-input v-model:value="formState.email" placeholder="Nhập địa chỉ email..." />
        </a-form-item>
      </a-col>
      <a-col :xs="24" :sm="12">
        <a-form-item label="Mật khẩu" name="password" :help="isEditing ? 'Để trống nếu không muốn đổi mật khẩu' : ''" :rules="passwordRules">
          <a-input-password v-model:value="formState.password" placeholder="Nhập mật khẩu..." />
        </a-form-item>
      </a-col>
      <a-col :xs="24" :sm="12">
        <a-form-item label="Vai trò" name="role" :rules="[{ required: true, message: 'Vui lòng chọn vai trò!' }]">
          <a-select v-model:value="formState.role" placeholder="Chọn vai trò" :disabled="isProtected">
            <a-select-option value="Admin">Admin (Quản trị hệ thống)</a-select-option>
            <a-select-option value="Trưởng lab">Trưởng lab</a-select-option>
            <a-select-option value="Phó lab">Phó lab</a-select-option>
            <a-select-option value="Giảng viên">Giảng viên</a-select-option>
            <a-select-option value="Sinh viên">Sinh viên</a-select-option>
          </a-select>
        </a-form-item>
      </a-col>
    </a-row>
  </a-form>
</template>

<script setup>
import { reactive, ref, defineExpose } from 'vue'

const formRef = ref(null)
const isEditing = ref(false)
const isProtected = ref(false)

const formState = reactive({
  username: '',
  email: '',
  password: '',
  role: 'Sinh viên'
})

const setFormData = (data) => {
  isEditing.value = !!data.username // Nếu có dữ liệu là đang edit
  isProtected.value = data.username === 'admin'
  formState.username = data.username || ''
  formState.email = data.email || ''
  formState.password = '' // Luôn reset password khi mở form
  formState.role = data.role || 'Sinh viên'
}

const passwordRules = [
  {
    validator: async (_rule, value) => {
      if (!isEditing.value && !value) throw new Error('Vui lòng nhập mật khẩu!')
      if (value && value.length < 8) throw new Error('Mật khẩu phải có ít nhất 8 ký tự!')
    }
  }
]

const getFormData = async () => {
  await formRef.value.validate()
  return { ...formState }
}

defineExpose({ setFormData, getFormData })
</script>


