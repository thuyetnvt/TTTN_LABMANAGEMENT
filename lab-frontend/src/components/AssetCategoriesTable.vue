<template>
  <div>
    <div class="table-actions" v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)">
      <a-button type="primary" @click="showAddModal">+ Thêm danh mục</a-button>
    </div>

    <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered>
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'createdAt'">
          {{ new Date(record.createdAt).toLocaleDateString('vi-VN') }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)" type="link" size="small" @click="showEditModal(record)">Sửa</a-button>
            <a-button v-if="role === 'Admin'" type="link" danger size="small" @click="handleDelete(record)">Xóa</a-button>
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
import { ref, computed, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { useAuthStore } from '../stores/authStore'

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const submitting = ref(false)
const isFormVisible = ref(false)
const isEditMode = ref(false)
const currentEditId = ref(null)
const formData = ref({ name: '', description: '' })

const columns = [
  { title: 'Tên danh mục', dataIndex: 'name', key: 'name' },
  { title: 'Mô tả', dataIndex: 'description', key: 'description' },
  { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', width: 140 },
  { title: 'Hành động', key: 'action', align: 'center', width: 160 }
]

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
    message.error(error?.response?.data || 'Lỗi khi lưu danh mục!')
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
        message.error(error?.response?.data || 'Không thể xóa danh mục!')
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


