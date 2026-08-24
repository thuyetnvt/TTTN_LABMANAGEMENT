<template>
  <div class="error-state" role="alert">
    <a-result status="error" title="Không tải được dữ liệu" :sub-title="message">
      <template #extra><a-button type="primary" @click="$emit('retry')">Thử lại</a-button></template>
    </a-result>
  </div>
</template>

<script setup>
defineEmits(['retry'])
defineProps({ message: { type: String, default: 'Vui lòng thử lại sau.' } })
</script>

<style scoped>
.error-state { padding: 16px; }
</style>
