<template>
  <div class="table-column-header">
    <span class="table-column-title">{{ title }}</span>
    <span class="table-column-filter-control">
      <a-popover v-model:open="open" trigger="click" placement="bottomRight">
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
import { FilterOutlined, SearchOutlined } from '@ant-design/icons-vue'

const props = defineProps({
  title: { type: String, required: true },
  type: { type: String, default: 'search' },
  value: { type: [String, Number], default: undefined },
  options: { type: Array, default: () => [] },
  placeholder: { type: String, default: '' }
})

const emit = defineEmits(['apply'])
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
</script>

<style scoped>
.table-column-header {
  display: flex;
  align-items: center;
  width: 100%;
  min-width: 0;
  gap: 8px;
}

.table-column-title {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.table-column-filter-control {
  display: inline-flex;
  flex: 0 0 auto;
  margin-left: auto;
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
