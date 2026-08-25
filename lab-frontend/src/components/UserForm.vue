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
      <a-col :xs="24" :sm="12"><a-form-item label="Họ và tên"><a-input v-model:value="formState.fullName" /></a-form-item></a-col>
      <a-col :xs="24" :sm="12"><a-form-item :label="universityCodeLabel"><a-input v-model:value="formState.universityCode" /></a-form-item></a-col>
      <a-col :xs="24" :sm="12"><a-form-item label="Số điện thoại"><a-input v-model:value="formState.phone" /></a-form-item></a-col>
      <a-col :xs="24" :sm="12"><a-form-item :label="departmentLabel"><a-input v-model:value="formState.department" /></a-form-item></a-col>
      <a-col v-if="isStudent" :xs="24" :sm="12"><a-form-item label="Lớp" name="className" :rules="studentClassRules"><a-input v-model:value="formState.className" /></a-form-item></a-col>
      <a-col :xs="24" :sm="12">
        <a-form-item label="Mật khẩu" name="password" :help="isEditing ? 'Để trống nếu không muốn đổi mật khẩu' : ''" :rules="passwordRules">
          <a-input-password v-model:value="formState.password" placeholder="Nhập mật khẩu..." />
        </a-form-item>
      </a-col>
      <a-col :xs="24" :sm="12">
        <a-form-item label="Vai trò" name="role" :rules="[{ required: true, message: 'Vui lòng chọn vai trò!' }]">
          <a-select v-model:value="formState.role" placeholder="Chọn vai trò" :disabled="isProtected">
            <a-select-option value="Admin">Quản trị viên</a-select-option>
            <a-select-option value="Trưởng lab">Trưởng phòng Lab</a-select-option>
            <a-select-option value="Phó lab">Phó phòng Lab</a-select-option>
            <a-select-option value="Giảng viên">Giảng viên</a-select-option>
            <a-select-option value="Sinh viên">Sinh viên</a-select-option>
          </a-select>
        </a-form-item>
      </a-col>
    </a-row>
  </a-form>
</template>

<script setup>
import { computed, reactive, ref, defineExpose } from 'vue'
import { isStudentRole, ROLE } from '../constants/business'

const formRef = ref(null)
const isEditing = ref(false)
const isProtected = ref(false)

const formState = reactive({
  username: '',
  email: '',
  fullName: '',
  universityCode: '',
  phone: '',
  department: '',
  className: '',
  password: '',
  role: ROLE.STUDENT
})

const isStudent = computed(() => isStudentRole(formState.role))
const universityCodeLabel = computed(() => isStudent.value ? 'Mã sinh viên' : 'Mã cán bộ')
const departmentLabel = computed(() => isStudent.value ? 'Khoa/ngành' : 'Khoa/bộ môn hoặc đơn vị')
const studentClassRules = [{ required: true, message: 'Vui lòng nhập lớp!' }]

const setFormData = (data) => {
  isEditing.value = !!data.username // Nếu có dữ liệu là đang edit
  isProtected.value = data.username === 'admin'
  formState.username = data.username || ''
  formState.email = data.email || ''
  formState.fullName = data.fullName || ''
  formState.universityCode = data.universityCode || ''
  formState.phone = data.phone || ''
  formState.department = data.department || ''
  formState.className = data.className || ''
  formState.password = '' // Luôn reset password khi mở form
  formState.role = data.role || ROLE.STUDENT
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


