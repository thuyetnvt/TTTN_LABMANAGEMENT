<template>
  <a-tree-select v-bind="$attrs" :tree-data="treeData" tree-node-filter-prop="title" allow-clear placeholder="Chọn vị trí" />
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ nodes: { type: Array, default: () => [] } })
const treeData = computed(() => {
  const byParent = new Map()
  for (const node of props.nodes) {
    const key = node.parentId ?? null
    if (!byParent.has(key)) byParent.set(key, [])
    byParent.get(key).push({ value: node.id, key: node.id, title: `${node.code} — ${node.name}`, children: [] })
  }
  const build = parent => (byParent.get(parent) || []).map(item => ({ ...item, children: build(item.value) }))
  return build(null)
})
</script>
