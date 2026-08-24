<template><a-tag :color="color">{{ label }}</a-tag></template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ action: { type: String, default: '' } })
const labels = {
  Create: 'Tạo mới', Update: 'Cập nhật', Delete: 'Xóa', Approve: 'Duyệt', Reject: 'Từ chối',
  Return: 'Trả', TeacherApprove: 'Giảng viên duyệt', TeacherReject: 'Giảng viên từ chối',
  SendReturnReminder: 'Nhắc trả', LoginSucceeded: 'Đăng nhập thành công', LoginFailed: 'Đăng nhập thất bại',
  MarkPaid: 'Xác nhận thanh toán', SeedSampleData: 'Tạo dữ liệu khởi tạo', UploadEvidence: 'Tải minh chứng'
}
const label = computed(() => labels[props.action] || props.action || 'Không xác định')
const color = computed(() => ({ Create: 'green', Approve: 'green', TeacherApprove: 'green', Update: 'blue', Return: 'purple', SendReturnReminder: 'orange', Delete: 'red', Reject: 'red', TeacherReject: 'red', LoginFailed: 'red' }[props.action] || 'default'))
</script>
