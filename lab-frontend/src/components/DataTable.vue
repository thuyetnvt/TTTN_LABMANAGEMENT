<template>
  <div class="data-table-shell">
    <a-table v-bind="$attrs" :data-source="dataSource" :loading="loading" :scroll="scroll || { x: 'max-content' }" :pagination="pagination">
      <template v-for="(_, name) in $slots" #[name]="slotData"><slot :name="name" v-bind="slotData || {}" /></template>
    </a-table>
  </div>
</template>

<script setup>
import { createTablePagination } from '../utils/tablePagination'

defineOptions({ inheritAttrs: false })
defineProps({
  dataSource: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  scroll: { type: Object, default: null },
  pagination: { type: [Object, Boolean], default: () => createTablePagination() }
})
</script>

<style scoped>
.data-table-shell { width: 100%; overflow: hidden; }
</style>
