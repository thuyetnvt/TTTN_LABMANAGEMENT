<template>
  <a-modal v-model:open="open" :title="title" :ok-text="okText" cancel-text="Hủy" :confirm-loading="loading" @ok="$emit('confirm')" @cancel="$emit('cancel')">
    <p>{{ message }}</p>
  </a-modal>
</template>

<script setup>
defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: 'Xác nhận thao tác' },
  message: { type: String, required: true },
  okText: { type: String, default: 'Xác nhận' },
  loading: { type: Boolean, default: false }
})
defineEmits(['confirm', 'cancel', 'update:open'])
</script>
