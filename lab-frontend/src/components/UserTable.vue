<template>
  <a-table :dataSource="dataSource" :columns="columns" bordered rowKey="id" :scroll="{ x: 'max-content' }">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'role'">
        <a-tag :color="isAdminRole(record.role) ? 'gold' : 'blue'">{{ roleLabel(record.role) }}</a-tag>
      </template>
      <template v-else-if="column.key === 'fullName'">
        <span>{{ record.fullName || '—' }}</span>
      </template>
      <template v-else-if="column.key === 'action'">
        <template v-if="isAdminRole(role)">
          <a-tooltip title="Sửa người dùng">
            <a-button type="link" size="small" aria-label="Sửa người dùng" @click="$emit('edit', record)">
              <template #icon><EditOutlined /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip v-if="record.username !== 'admin'" title="Xóa người dùng">
            <a-button type="link" danger size="small" aria-label="Xóa người dùng" @click="$emit('delete', record)">
              <template #icon><DeleteOutlined /></template>
            </a-button>
          </a-tooltip>
        </template>
        <span v-else style="color: #9ca3af;">Chỉ xem</span>
      </template>
    </template>
  </a-table>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import { isAdminRole, roleLabel } from '../constants/business'

defineProps({
  dataSource: {
    type: Array,
    required: true
  }
})

defineEmits(['edit', 'delete'])

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const columns = [
  { title: 'Tài khoản', dataIndex: 'username', key: 'username' },
  { title: 'Họ và tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Mã định danh', dataIndex: 'universityCode', key: 'universityCode' },
  { title: 'Email', dataIndex: 'email', key: 'email' },
  { title: 'Vai trò', dataIndex: 'role', key: 'role' },
  { title: 'Hành động', key: 'action', width: 150, align: 'center' }
]
</script>
