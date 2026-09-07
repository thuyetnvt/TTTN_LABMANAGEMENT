<template>
  <div class="table-column-header">
    <span class="table-column-title">{{ title }}</span>
    <a-popover v-model:open="open" trigger="click" placement="bottomLeft">
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
        :aria-label="`Lọc cột ${title}`"
        @click.stop
      >
        <template #icon>
          <SearchOutlined v-if="type === 'search'" />
          <FilterOutlined v-else />
        </template>
      </a-button>
    </a-popover>
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
  display: inline-flex;
  align-items: center;
  gap: 3px;
  max-width: 100%;
}

.table-column-title {
  overflow: hidden;
  text-overflow: ellipsis;
}

.table-column-filter-button {
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
