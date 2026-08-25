<template>
  <a-modal
    :open="open"
    :title="title"
    :ok-text="okText"
    :ok-type="okType"
    cancel-text="Hủy"
    :confirm-loading="loading"
    @ok="$emit('confirm')"
    @cancel="$emit('cancel')"
    @update:open="$emit('update:open', $event)"
  >
    <p>{{ message }}</p>
  </a-modal>
</template>

<script setup>
defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: 'Xác nhận thao tác' },
  message: { type: String, required: true },
  okText: { type: String, default: 'Xác nhận' },
  okType: { type: String, default: undefined },
  loading: { type: Boolean, default: false }
})
defineEmits(['confirm', 'cancel', 'update:open'])
</script>
