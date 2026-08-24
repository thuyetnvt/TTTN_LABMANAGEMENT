<template>
  <a-badge :count="unreadCount" :overflow-count="99">
    <a-button type="text" class="notification-button" aria-label="Mở thông báo" title="Thông báo" @click="$emit('open')">
      <template #icon><bell-outlined /></template>
    </a-button>
  </a-badge>
</template>

<script setup>
import { BellOutlined } from '@ant-design/icons-vue'
defineProps({ unreadCount: { type: Number, default: 0 } })
defineEmits(['open'])
</script>

<style scoped>
.notification-button { color: inherit; }
</style>
