<template>
  <section class="filter-bar" aria-label="Bộ lọc dữ liệu"><slot /></section>
</template>

<style scoped>
.filter-bar { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; padding: 16px; margin-bottom: 16px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 12px; }
.filter-bar :deep(.ant-form-item) { margin-bottom: 0; }
</style>
