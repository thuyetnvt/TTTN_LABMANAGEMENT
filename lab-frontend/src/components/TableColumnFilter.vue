<template>
  <div class="table-column-header">
    <span class="table-column-title" :title="title">{{ title }}</span>
    <span class="table-column-controls table-column-filter-control">
      <span v-if="sortable" class="table-column-sort-control" :aria-label="`Sắp xếp cột ${title}`">
        <button
          type="button"
          class="table-column-sort-button"
          :class="{ 'is-active': sortOrder === 'ascend' }"
          :aria-label="`Sắp xếp ${title} tăng dần`"
          :aria-pressed="sortOrder === 'ascend'"
          title="Tăng dần"
          @click.stop="applySort('ascend')"
        >
          <CaretUpOutlined />
        </button>
        <button
          type="button"
          class="table-column-sort-button"
          :class="{ 'is-active': sortOrder === 'descend' }"
          :aria-label="`Sắp xếp ${title} giảm dần`"
          :aria-pressed="sortOrder === 'descend'"
          title="Giảm dần"
          @click.stop="applySort('descend')"
        >
          <CaretDownOutlined />
        </button>
      </span>
      <a-popover v-if="filterable" v-model:open="open" trigger="click" placement="bottomRight">
        <template #content>
          <div class="table-column-filter-panel">
            <a-input-search
              v-if="type === 'search'"
              v-model:value="draftValue"
              allow-clear
              :placeholder="placeholder || `Tìm ${title.toLowerCase()}...`"
              @search="apply"
              @change="handleInputChange"
            />
            <a-select
              v-else
              v-model:value="draftValue"
              allow-clear
              :placeholder="placeholder || `Lọc ${title.toLowerCase()}`"
              style="min-width: 190px"
              @change="apply"
            >
              <a-select-option v-for="option in options" :key="option.value" :value="option.value">
                {{ option.label }}
              </a-select-option>
            </a-select>
            <a-button v-if="type === 'search' && draftValue" type="link" size="small" @click="clear">
              Xóa lọc
            </a-button>
          </div>
        </template>
        <a-button
          type="text"
          size="small"
          class="table-column-filter-button"
          :class="{ 'is-active': hasValue }"
          :aria-label="type === 'search' ? `Tìm trong cột ${title}` : `Lọc cột ${title}`"
          :title="type === 'search' ? `Tìm trong cột ${title}` : `Lọc cột ${title}`"
          @click.stop
        >
          <template #icon>
            <SearchOutlined v-if="type === 'search'" />
            <FilterOutlined v-else />
          </template>
        </a-button>
      </a-popover>
    </span>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { CaretDownOutlined, CaretUpOutlined, FilterOutlined, SearchOutlined } from '@ant-design/icons-vue'

const props = defineProps({
  title: { type: String, required: true },
  type: { type: String, default: 'search' },
  value: { type: [String, Number], default: undefined },
  options: { type: Array, default: () => [] },
  placeholder: { type: String, default: '' },
  filterable: { type: Boolean, default: true },
  sortable: { type: Boolean, default: false },
  sortOrder: { type: String, default: undefined }
})

const emit = defineEmits(['apply', 'sort'])
const open = ref(false)
const draftValue = ref(props.value)

watch(() => props.value, value => {
  draftValue.value = value
})

const hasValue = computed(() => draftValue.value !== undefined && draftValue.value !== null && draftValue.value !== '')

const apply = () => {
  emit('apply', draftValue.value || undefined)
  open.value = false
}

const clear = () => {
  draftValue.value = undefined
  apply()
}

const handleInputChange = event => {
  if (!event?.target?.value) clear()
}

const applySort = order => {
  emit('sort', props.sortOrder === order ? undefined : order)
}
</script>

<style scoped>
.table-column-header {
  display: flex;
  align-items: center;
  width: max-content;
  min-width: 100%;
  gap: 8px;
  white-space: nowrap;
}

.table-column-title {
  flex: 0 0 auto;
  min-width: max-content;
  overflow: visible;
  text-overflow: clip;
  white-space: nowrap;
}

.table-column-controls {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-left: auto;
  flex: 0 0 auto;
}

.table-column-filter-control {
  display: inline-flex;
  flex: 0 0 auto;
}

.table-column-sort-control {
  display: inline-flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 14px;
  height: 22px;
}

.table-column-sort-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 14px;
  height: 11px;
  padding: 0;
  border: 0;
  background: transparent;
  color: #94a3b8;
  cursor: pointer;
  font-size: 11px;
  line-height: 11px;
}

.table-column-sort-button:hover,
.table-column-sort-button.is-active {
  color: var(--color-primary, #d97757);
}

.table-column-filter-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  padding: 0;
  color: #94a3b8;
}

.table-column-filter-button:hover,
.table-column-filter-button.is-active {
  color: var(--color-primary, #d97757);
}

.table-column-filter-panel {
  display: grid;
  gap: 6px;
  min-width: 220px;
}
</style>
