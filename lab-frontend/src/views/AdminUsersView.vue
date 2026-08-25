<template>
  <div class="users-container">
    <div class="toolbar">
      <h2>Quản lý người dùng</h2>
      <a-button v-if="isAdminRole(role)" type="primary" @click="showAddModal">+ Thêm tài khoản</a-button>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-spin :spinning="loading">
        <UserTable :dataSource="users" @edit="showEditModal" @delete="handleDelete" />
      </a-spin>
    </a-card>

    <a-modal
      v-model:open="isModalVisible"
      :title="isEditing ? 'Sửa thông tin tài khoản' : 'Thêm tài khoản mới'"
      @ok="handleModalOk"
      @cancel="isModalVisible = false"
      okText="Lưu"
      cancelText="Hủy"
      :confirmLoading="submitting"
      width="700px"
      wrapClassName="responsive-modal"
    >
      <UserForm ref="userFormRef" />
    </a-modal>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import UserTable from '../components/UserTable.vue'
import UserForm from '../components/UserForm.vue'
import { userApi } from '../api/userApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const users = ref([])
const loading = ref(false)
const submitting = ref(false)

const isModalVisible = ref(false)
const isEditing = ref(false)
const userFormRef = ref(null)
const editingUserId = ref(null)

onMounted(() => {
  fetchUsers()
})

const fetchUsers = async () => {
  loading.value = true
  try {
    const res = await userApi.getAll()
    users.value = res.data || res || []
  } catch {
    message.error('Lỗi khi lấy danh sách người dùng!')
  } finally {
    loading.value = false
  }
}

const showAddModal = () => {
  if (!isAdminRole(role.value)) return
  isEditing.value = false
  isModalVisible.value = true
  setTimeout(() => userFormRef.value?.setFormData({}), 0)
}

const showEditModal = (record) => {
  if (!isAdminRole(role.value)) return
  isEditing.value = true
  editingUserId.value = record.id
  isModalVisible.value = true
  setTimeout(() => userFormRef.value?.setFormData(record), 0)
}

const handleDelete = (record) => {
  if (!isAdminRole(role.value)) return
  if (record.username === 'admin') {
    message.warning('Không thể xóa tài khoản quản trị hệ thống!')
    return
  }
  Modal.confirm({
    title: 'Xóa tài khoản',
    content: `Bạn có chắc chắn muốn xóa tài khoản ${record.username}?`,
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await userApi.delete(record.id)
        message.success(`Đã xóa tài khoản: ${record.username}`)
        fetchUsers()
      } catch (error) {
        message.error(getApiErrorMessage(error, 'Lỗi khi xóa tài khoản!'))
      }
    }
  })
}

const handleModalOk = async () => {
  if (!isAdminRole(role.value)) return
  try {
    const formData = await userFormRef.value.getFormData()
    submitting.value = true
    if (isEditing.value) {
      await userApi.update(editingUserId.value, formData)
      message.success('Cập nhật tài khoản thành công!')
    } else {
      await userApi.create(formData)
      message.success('Thêm tài khoản thành công!')
    }
    isModalVisible.value = false
    fetchUsers()
  } catch (error) {
    if (!error.errorFields) {
      message.error(getApiErrorMessage(error, 'Có lỗi xảy ra!'))
    }
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.users-container {
  padding: 0;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}
</style>
