<template>
  <a-tag :color="statusColor(status)" :title="status">
    {{ statusLabel(status) }}
  </a-tag>
</template>

<script setup>
import { statusColor, statusLabel } from '../constants/business'

defineProps({
  status: {
    type: String,
    default: ''
  }
})
</script>
