<template>
  <div class="empty-state" role="status">
    <a-empty :description="description" />
    <a-button v-if="$slots.actions" type="link"><slot name="actions" /></a-button>
  </div>
</template>

<script setup>
defineProps({ description: { type: String, default: 'Chưa có dữ liệu' } })
</script>

<style scoped>
.empty-state { padding: 24px 12px; text-align: center; }
</style>
