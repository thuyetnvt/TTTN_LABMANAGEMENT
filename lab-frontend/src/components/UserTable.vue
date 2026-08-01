<template>
  <a-table :dataSource="dataSource" :columns="columns" bordered rowKey="id">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'role'">
        <a-tag :color="record.role === 'Admin' ? 'gold' : 'blue'">{{ record.role }}</a-tag>
      </template>
      <template v-else-if="column.key === 'protected'">
        <a-tag v-if="record.username === 'admin'" color="red">Không được xóa</a-tag>
        <span v-else>-</span>
      </template>
      <template v-else-if="column.key === 'action'">
        <template v-if="role === 'Admin'">
          <a-button type="link" size="small" @click="$emit('edit', record)">Sửa</a-button>
          <a-button v-if="record.username !== 'admin'" type="link" danger size="small" @click="$emit('delete', record)">Xóa</a-button>
        </template>
        <span v-else style="color: #9ca3af;">Chỉ xem</span>
      </template>
    </template>
  </a-table>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/authStore'

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
  { title: 'Email', dataIndex: 'email', key: 'email' },
  { title: 'Vai trò', dataIndex: 'role', key: 'role' },
  { title: 'Lưu ý', key: 'protected', width: 150, align: 'center' },
  { title: 'Hành động', key: 'action', width: 150, align: 'center' }
]
</script>
