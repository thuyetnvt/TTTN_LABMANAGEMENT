<template>
  <div class="responsive-data-list">
    <article v-for="item in items" :key="item[itemKey]" class="responsive-data-card">
      <slot :item="item" />
    </article>
    <EmptyState v-if="!items.length && !loading" :description="emptyDescription" />
    <a-spin v-if="loading" class="responsive-loading" />
  </div>
</template>

<script setup>
import EmptyState from './EmptyState.vue'

defineProps({
  items: { type: Array, default: () => [] },
  itemKey: { type: [String, Function], default: 'id' },
  loading: { type: Boolean, default: false },
  emptyDescription: { type: String, default: 'Chưa có dữ liệu' }
})
</script>

<style scoped>
.responsive-data-list { display: none; }
.responsive-data-card { padding: 14px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 12px; }
.responsive-loading { display: block; margin: 24px auto; }
@media (max-width: 767px) { .responsive-data-list { display: grid; gap: 10px; } }
</style>
