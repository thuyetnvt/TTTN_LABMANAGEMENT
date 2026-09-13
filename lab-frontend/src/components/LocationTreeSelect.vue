<template>
  <a-tree-select v-bind="$attrs" :tree-data="treeData" tree-node-filter-prop="title" allow-clear placeholder="Chọn vị trí" />
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ nodes: { type: Array, default: () => [] } })
const treeData = computed(() => props.nodes.map(node => ({
  value: node.id,
  key: node.id,
  title: node.name
})))
</script>
