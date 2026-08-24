<template>
  <a-table :dataSource="dataSource" :columns="columns" bordered rowKey="id" :scroll="{ x: 'max-content' }">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'role'">
        <a-tag :color="isAdminRole(record.role) ? 'gold' : 'blue'">{{ roleLabel(record.role) }}</a-tag>
      </template>
      <template v-else-if="column.key === 'action'">
        <template v-if="isAdminRole(role)">
          <a-button type="link" size="small" @click="$emit('edit', record)" title="Sửa">
            <template #icon><EditOutlined /></template>
          </a-button>
          <a-button v-if="record.username !== 'admin'" type="link" danger size="small" @click="$emit('delete', record)" title="Xóa">
            <template #icon><DeleteOutlined /></template>
          </a-button>
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
