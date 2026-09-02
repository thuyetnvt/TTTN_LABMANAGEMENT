<template>
  <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" bordered rowKey="id" :scroll="{ x: 'max-content' }" :pagination="pagination" @change="$emit('change', $event)">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'role'">
        <a-tag :color="isAdminRole(record.role) ? 'gold' : 'blue'">{{ roleLabel(record.role) }}</a-tag>
      </template>
      <template v-else-if="column.key === 'fullName'">
        <span>{{ record.fullName || '—' }}</span>
      </template>
      <template v-else-if="column.key === 'isActive'">
        <a-tag :color="record.isActive ? 'green' : 'red'">{{ record.isActive ? 'Hoạt động' : 'Đã khóa' }}</a-tag>
      </template>
      <template v-else-if="column.key === 'action'">
        <div v-if="isAdminRole(role)" class="table-action-buttons">
          <a-tooltip v-if="record.isActive" title="Sửa người dùng">
            <a-button type="link" size="small" aria-label="Sửa người dùng" @click="$emit('edit', record)">
              <template #icon><EditOutlined /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip v-if="record.isActive && record.username !== 'admin'" title="Khóa tài khoản">
            <a-button type="link" danger size="small" aria-label="Khóa tài khoản" @click="$emit('delete', record)">
              <template #icon><LockOutlined /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip v-else-if="!record.isActive" title="Mở khóa tài khoản">
            <a-button type="link" size="small" aria-label="Mở khóa tài khoản" @click="$emit('activate', record)">
              <template #icon><UnlockOutlined /></template>
            </a-button>
          </a-tooltip>
        </div>
        <span v-else style="color: #9ca3af;">—</span>
      </template>
    </template>
  </a-table>
  <ResponsiveDataList :items="dataSource" :pagination="pagination" empty-description="Chưa có người dùng" @change="emit('change', $event)">
    <template #default="{ item }">
      <div class="mobile-user-heading">
        <div><strong>{{ item.fullName || 'Chưa cập nhật họ tên' }}</strong><span>{{ item.email }}</span></div>
        <a-tag :color="isAdminRole(item.role) ? 'gold' : 'blue'">{{ roleLabel(item.role) }}</a-tag>
      </div>
      <div class="mobile-user-code">Mã định danh: <strong>{{ item.universityCode || '—' }}</strong></div>
      <a-tag :color="item.isActive ? 'green' : 'red'">{{ item.isActive ? 'Hoạt động' : 'Đã khóa' }}</a-tag>
      <div v-if="isAdminRole(role)" class="mobile-user-actions">
        <a-button v-if="item.isActive" @click="emit('edit', item)"><template #icon><EditOutlined /></template>Sửa</a-button>
        <a-button v-if="item.isActive && item.username !== 'admin'" danger @click="emit('delete', item)"><template #icon><LockOutlined /></template>Khóa</a-button>
        <a-button v-else-if="!item.isActive" @click="emit('activate', item)"><template #icon><UnlockOutlined /></template>Mở khóa</a-button>
      </div>
    </template>
  </ResponsiveDataList>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { EditOutlined, LockOutlined, UnlockOutlined } from '@ant-design/icons-vue'
import { isAdminRole, roleLabel } from '../constants/business'
import ResponsiveDataList from './ResponsiveDataList.vue'

defineProps({
  dataSource: {
    type: Array,
    required: true
  },
  pagination: {
    type: Object,
    required: true
  }
})

const emit = defineEmits(['edit', 'delete', 'activate', 'change'])

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const columns = [
  { title: 'Họ và tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Mã định danh', dataIndex: 'universityCode', key: 'universityCode' },
  { title: 'Email', dataIndex: 'email', key: 'email' },
  { title: 'Vai trò', dataIndex: 'role', key: 'role' },
  { title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive', width: 120 },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 150, align: 'center' }
]
</script>

<style scoped>
.mobile-user-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-user-heading > div { display: grid; min-width: 0; gap: 4px; }
.mobile-user-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-user-heading span { overflow: hidden; color: var(--color-text-secondary); font-size: 12px; text-overflow: ellipsis; }
.mobile-user-code { margin: 12px 0; color: var(--color-text-secondary); font-size: 13px; }
.mobile-user-code strong { color: var(--color-ink); }
.mobile-user-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
@media (max-width: 767px) { .desktop-table { display: none; } }
</style>
