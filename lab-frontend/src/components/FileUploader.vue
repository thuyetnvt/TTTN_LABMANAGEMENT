<template>
  <a-upload :before-upload="handleBeforeUpload" :show-upload-list="false" :accept="accept">
    <a-button :loading="loading">{{ label }}</a-button>
  </a-upload>
  <span v-if="modelValue" class="file-name">{{ modelValue.name }}</span>
</template>

<script setup>
import { Upload } from 'ant-design-vue'

const props = defineProps({
  modelValue: { type: Object, default: null },
  accept: { type: String, default: '.pdf,.jpg,.jpeg,.png,.webp,.doc,.docx' },
  label: { type: String, default: 'Chọn file' },
  loading: { type: Boolean, default: false },
  maxBytes: { type: Number, default: 10 * 1024 * 1024 }
})
const emit = defineEmits(['update:modelValue', 'invalid'])
const handleBeforeUpload = file => {
  if (file.size > props.maxBytes) {
    emit('invalid', `File không được vượt quá ${Math.round(props.maxBytes / 1024 / 1024)} MB.`)
    return Upload.LIST_IGNORE
  }
  emit('update:modelValue', file)
  return false
}
</script>

<style scoped>.file-name { margin-left: 8px; color: var(--color-secondary); }</style>
