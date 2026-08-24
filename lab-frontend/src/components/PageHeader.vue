<template>
  <header class="page-header">
    <div class="page-heading">
      <div v-if="$slots.breadcrumb" class="page-breadcrumb"><slot name="breadcrumb" /></div>
      <h1>{{ title }}</h1>
      <p v-if="subtitle">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.actions" class="page-actions"><slot name="actions" /></div>
  </header>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' }
})
</script>

<style scoped>
.page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; margin-bottom: 20px; }
.page-heading h1 { margin: 0; color: var(--color-ink); font-size: 24px; line-height: 1.3; font-weight: 700; }
.page-heading p { margin: 6px 0 0; color: var(--color-secondary); }
.page-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
.page-breadcrumb { margin-bottom: 6px; color: var(--color-secondary); font-size: 13px; }
@media (max-width: 640px) {
  .page-header { flex-direction: column; }
  .page-actions { width: 100%; justify-content: flex-start; }
  .page-heading h1 { font-size: 20px; }
}
</style>
