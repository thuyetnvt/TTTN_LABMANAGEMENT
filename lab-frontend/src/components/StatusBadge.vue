<template>
  <a-tag :color="color || getStatusColor(status)" :title="label">
    {{ label }}
  </a-tag>
</template>

<script setup>
import { computed } from 'vue'
import {
  getBorrowStatusLabel,
  getConsumableRequestStatusLabel,
  getEquipmentStatusLabel,
  getInventoryStatusLabel,
  getMaintenanceStatusLabel,
  getPenaltyStatusLabel,
  getReturnConditionLabel,
  getStatusColor
} from '../utils/statusLabels'

const props = defineProps({
  status: {
    type: String,
    default: ''
  },
  type: {
    type: String,
    default: 'equipment'
  },
  labelOverride: {
    type: String,
    default: ''
  },
  color: {
    type: String,
    default: ''
  }
})

const labelers = {
  equipment: getEquipmentStatusLabel,
  borrow: getBorrowStatusLabel,
  maintenance: getMaintenanceStatusLabel,
  inventory: getInventoryStatusLabel,
  consumable: getConsumableRequestStatusLabel,
  penalty: getPenaltyStatusLabel,
  returnCondition: getReturnConditionLabel
}

const label = computed(() => props.labelOverride || (labelers[props.type] || getEquipmentStatusLabel)(props.status))
</script>
