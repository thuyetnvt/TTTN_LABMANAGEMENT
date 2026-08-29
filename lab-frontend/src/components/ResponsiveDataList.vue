<template>
  <div class="responsive-data-list">
    <article v-for="item in pagedItems" :key="item[itemKey]" class="responsive-data-card">
      <slot :item="item" />
    </article>
    <EmptyState v-if="!items.length && !loading" :description="emptyDescription" />
    <a-spin v-if="loading" class="responsive-loading" />
    <a-pagination
      v-if="items.length && !loading"
      :current="currentPage"
      :page-size="pageSize"
      :total="items.length"
      :page-size-options="TABLE_PAGE_SIZE_OPTIONS"
      :show-size-changer="true"
      :hide-on-single-page="false"
      class="responsive-pagination"
      @change="handlePageChange"
    />
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import EmptyState from './EmptyState.vue'
import { TABLE_PAGE_SIZE, TABLE_PAGE_SIZE_OPTIONS } from '../utils/tablePagination'

const props = defineProps({
  items: { type: Array, default: () => [] },
  itemKey: { type: [String, Function], default: 'id' },
  loading: { type: Boolean, default: false },
  emptyDescription: { type: String, default: 'Chưa có dữ liệu' }
})

const currentPage = ref(1)
const pageSize = ref(TABLE_PAGE_SIZE)
const pagedItems = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return props.items.slice(start, start + pageSize.value)
})

const handlePageChange = (page, nextPageSize) => {
  currentPage.value = nextPageSize === pageSize.value ? page : 1
  pageSize.value = nextPageSize
}

watch(() => props.items.length, total => {
  const lastPage = Math.max(1, Math.ceil(total / pageSize.value))
  if (currentPage.value > lastPage) currentPage.value = lastPage
})
</script>

<style scoped>
.responsive-data-list { display: none; }
.responsive-data-card { padding: 14px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 12px; }
.responsive-loading { display: block; margin: 24px auto; }
.responsive-pagination { justify-self: end; margin-top: 6px; }
@media (max-width: 767px) { .responsive-data-list { display: grid; gap: 10px; } }
</style>
