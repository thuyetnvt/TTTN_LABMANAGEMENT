<template><a-tag :color="color">{{ label }}</a-tag></template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ action: { type: String, default: '' } })
const labels = {
  Create: 'Tạo mới', Update: 'Cập nhật', Delete: 'Xóa', Approve: 'Duyệt', Reject: 'Từ chối',
  Return: 'Trả', TeacherApprove: 'Giảng viên duyệt', TeacherReject: 'Giảng viên từ chối',
  SendReturnReminder: 'Nhắc trả', LoginSucceeded: 'Đăng nhập thành công', LoginFailed: 'Đăng nhập thất bại',
  MarkPaid: 'Xác nhận thanh toán', SeedSampleData: 'Tạo dữ liệu khởi tạo', UploadEvidence: 'Tải minh chứng',
  ChangePassword: 'Đổi mật khẩu', SeedFullSampleDataV1: 'Tạo dữ liệu mẫu',
  SeedFullSampleDataV2: 'Tạo dữ liệu mẫu', Logout: 'Đăng xuất', Login: 'Đăng nhập',
  UpdateAvatar: 'Đổi ảnh đại diện', DeleteAvatar: 'Xóa ảnh đại diện'
}
const label = computed(() => labels[props.action] || 'Thao tác khác')
const color = computed(() => ({ Create: 'green', Approve: 'green', TeacherApprove: 'green', Update: 'blue', UpdateAvatar: 'blue', Return: 'purple', SendReturnReminder: 'orange', Delete: 'red', Reject: 'red', TeacherReject: 'red', LoginFailed: 'red', DeleteAvatar: 'red' }[props.action] || 'default'))
</script>
