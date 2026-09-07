<template>
  <div>
    <div class="table-actions">
      <a-input-search v-model:value="searchQuery" allow-clear placeholder="Tìm tên, mô tả..." style="width: 240px" />
      <a-button v-if="isManagerRole(role)" type="primary" @click="showAddModal">+ Thêm danh mục</a-button>
    </div>

    <a-table :dataSource="filteredDataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination">
      <template #headerCell="{ column }">
        <TableColumnFilter
          v-if="column.filterType || column.sortable"
          :title="column.title"
          :type="column.filterType"
          :filterable="Boolean(column.filterType)"
          :sortable="Boolean(column.sortable)"
          :sort-order="sortState.field === column.sortKey ? sortState.order : undefined"
          :value="searchQuery"
          :placeholder="column.filterPlaceholder"
          @apply="searchQuery = $event || ''"
          @sort="value => applyColumnSort(column, value)"
        />
        <span v-else>{{ column.title }}</span>
      </template>
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'createdAt'">
          {{ formatVietnamDate(record.createdAt) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space class="table-action-buttons">
            <a-tooltip v-if="isManagerRole(role)" title="Sửa danh mục">
              <a-button type="link" size="small" aria-label="Sửa danh mục" @click="showEditModal(record)">
                <template #icon><EditOutlined /></template>
              </a-button>
            </a-tooltip>
            <a-tooltip v-if="isAdminRole(role)" title="Xóa danh mục">
              <a-button type="link" danger size="small" aria-label="Xóa danh mục" @click="handleDelete(record)">
                <template #icon><DeleteOutlined /></template>
              </a-button>
            </a-tooltip>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="isFormVisible" :title="isEditMode ? 'Sửa danh mục' : 'Thêm danh mục'" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting">
      <a-form layout="vertical">
        <a-form-item label="Tên danh mục" required>
          <a-input v-model:value="formData.name" placeholder="Ví dụ: IoT, AI, Tài sản" />
        </a-form-item>
        <a-form-item label="Mô tả">
          <a-textarea v-model:value="formData.description" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { EditOutlined, DeleteOutlined, EyeOutlined } from '@ant-design/icons-vue'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isManagerRole } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination } from '../utils/tablePagination'
import { formatVietnamDate } from '../utils/dateTime'
import TableColumnFilter from './TableColumnFilter.vue'
import { sortTableRows } from '../utils/tableSort'

const tablePagination = createTablePagination()

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const submitting = ref(false)
const isFormVisible = ref(false)
const isEditMode = ref(false)
const currentEditId = ref(null)
const formData = ref({ name: '', description: '' })
const searchQuery = ref('')
const sortState = reactive({ field: undefined, order: undefined })

const filteredDataSource = computed(() => {
  const keyword = searchQuery.value.trim().toLowerCase()
  const filtered = keyword
    ? dataSource.value.filter(item => [item.name, item.description].some(value => String(value || '').toLowerCase().includes(keyword)))
    : dataSource.value
  return sortTableRows(filtered, sortState.field, sortState.order)
})

const columns = [
  { title: 'Tên danh mục', dataIndex: 'name', key: 'name', sortKey: 'name', sortable: true, filterType: 'search', filterPlaceholder: 'Tìm tên danh mục...' },
  { title: 'Mô tả', dataIndex: 'description', key: 'description', sortKey: 'description', sortable: true, filterType: 'search', filterPlaceholder: 'Tìm mô tả...' },
  { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', sortKey: 'createdAt', sortable: true, width: 140 },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), align: 'center', width: 160 }
]

const applyColumnSort = (column, order) => {
  sortState.field = order ? column.sortKey : undefined
  sortState.order = order
}

onMounted(() => fetchData())

const fetchData = async () => {
  loading.value = true
  try {
    dataSource.value = await assetCategoryApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh mục!')
  } finally {
    loading.value = false
  }
}

const showAddModal = () => {
  isEditMode.value = false
  formData.value = { name: '', description: '' }
  isFormVisible.value = true
}

const showEditModal = (record) => {
  isEditMode.value = true
  currentEditId.value = record.id
  formData.value = { ...record }
  isFormVisible.value = true
}

const submitForm = async () => {
  if (!formData.value.name) {
    message.warning('Vui lòng nhập tên danh mục!')
    return
  }

  submitting.value = true
  try {
    if (isEditMode.value) {
      await assetCategoryApi.update(currentEditId.value, formData.value)
      message.success('Đã cập nhật danh mục!')
    } else {
      await assetCategoryApi.create(formData.value)
      message.success('Đã thêm danh mục!')
    }
    isFormVisible.value = false
    fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Lỗi khi lưu danh mục!'))
  } finally {
    submitting.value = false
  }
}

const handleDelete = (record) => {
  Modal.confirm({
    title: 'Xóa danh mục',
    content: `Bạn chắc chắn muốn xóa danh mục ${record.name}?`,
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await assetCategoryApi.delete(record.id)
        message.success('Đã xóa danh mục!')
        fetchData()
      } catch (error) {
        message.error(getApiErrorMessage(error, 'Không thể xóa danh mục!'))
      }
    }
  })
}
</script>

<style scoped>
.table-actions {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 16px;
}
</style>


