<template>
  <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" bordered rowKey="id" :scroll="{ x: 'max-content' }" :pagination="pagination" @change="$emit('change', $event)">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'role'">
        <a-tag :color="isAdminRole(record.role) ? 'gold' : 'blue'">{{ roleLabel(record.role) }}</a-tag>
      </template>
      <template v-else-if="column.key === 'fullName'">
        <span>{{ record.fullName || '—' }}</span>
      </template>
      <template v-else-if="column.key === 'action'">
        <div v-if="isAdminRole(role)" class="table-action-buttons">
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
        </div>
        <span v-else style="color: #9ca3af;">Chỉ xem</span>
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
      <div v-if="isAdminRole(role)" class="mobile-user-actions">
        <a-button @click="emit('edit', item)"><template #icon><EditOutlined /></template>Sửa</a-button>
        <a-button v-if="item.username !== 'admin'" danger @click="emit('delete', item)"><template #icon><DeleteOutlined /></template>Xóa</a-button>
      </div>
    </template>
  </ResponsiveDataList>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
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

const emit = defineEmits(['edit', 'delete', 'change'])

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const columns = [
  { title: 'Họ và tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Mã định danh', dataIndex: 'universityCode', key: 'universityCode' },
  { title: 'Email', dataIndex: 'email', key: 'email' },
  { title: 'Vai trò', dataIndex: 'role', key: 'role' },
  { title: 'Hành động', key: 'action', width: 150, align: 'center' }
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
