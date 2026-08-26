<template>
  <a-tag :color="getStatusColor(status, type)" :title="getStatusLabel(status, type)">
    {{ getStatusLabel(status, type) }}
  </a-tag>
</template>

<script setup>
import { getStatusColor, getStatusLabel } from '../utils/statusLabels'

defineProps({
  status: {
    type: String,
    default: ''
  },
  type: {
    type: String,
    default: 'equipment'
  }
})
</script>
