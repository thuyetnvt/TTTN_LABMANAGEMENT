<template>
  <div class="users-container">
    <div class="toolbar">
      <h2>Quản lý người dùng</h2>
      <div class="toolbar-actions">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Tìm tên, mã, email..." style="width: 260px" @search="applyFilters" />
        <a-select v-model:value="roleFilter" allow-clear placeholder="Vai trò" style="width: 160px" @change="applyFilters">
          <a-select-option :value="ROLE.ADMIN">Quản trị viên</a-select-option>
          <a-select-option :value="ROLE.LAB_HEAD">Trưởng lab</a-select-option>
          <a-select-option :value="ROLE.DEPUTY_LAB_HEAD">Phó lab</a-select-option>
          <a-select-option :value="ROLE.TEACHER">Giảng viên</a-select-option>
          <a-select-option :value="ROLE.STUDENT">Sinh viên</a-select-option>
        </a-select>
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
          <a-select-option value="">Tất cả</a-select-option>
          <a-select-option value="ACTIVE">Hoạt động</a-select-option>
          <a-select-option value="INACTIVE">Đã khóa</a-select-option>
        </a-select>
        <a-button v-if="isAdminRole(role)" type="primary" @click="showAddModal">+ Thêm tài khoản</a-button>
      </div>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-spin :spinning="loading">
        <UserTable :dataSource="users" :pagination="pagination" @change="handleTableChange" @edit="showEditModal" @delete="handleDelete" @activate="handleActivate" />
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
import { computed, reactive, ref, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import UserTable from '../components/UserTable.vue'
import UserForm from '../components/UserForm.vue'
import { userApi } from '../api/userApi'
import { useAuthStore } from '../stores/authStore'
import { ROLE, isAdminRole } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const users = ref([])
const loading = ref(false)
const submitting = ref(false)
const searchQuery = ref('')
const roleFilter = ref(undefined)
const statusFilter = ref(undefined)
const pagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

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
    const res = await userApi.getPaged({
      page: pagination.current,
      pageSize: pagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      role: roleFilter.value,
      status: statusFilter.value
    })
    users.value = res.items || []
    pagination.total = res.total || 0
  } catch {
    message.error('Lỗi khi lấy danh sách người dùng!')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  pagination.current = 1
  fetchUsers()
}

const handleTableChange = (pager) => {
  pagination.current = pager.pageSize === pagination.pageSize ? pager.current : 1
  pagination.pageSize = pager.pageSize
  fetchUsers()
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
    message.warning('Không thể khóa tài khoản quản trị hệ thống!')
    return
  }
  Modal.confirm({
    title: 'Khóa tài khoản',
    content: `Khóa tài khoản ${record.username}? Người dùng sẽ bị đăng xuất và không thể đăng nhập cho đến khi được mở khóa.`,
    okText: 'Khóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await userApi.delete(record.id)
        message.success(`Đã khóa tài khoản: ${record.username}`)
        fetchUsers()
      } catch (error) {
        message.error(getApiErrorMessage(error, 'Không thể khóa tài khoản!'))
      }
    }
  })
}

const handleActivate = async record => {
  if (!isAdminRole(role.value)) return
  try {
    await userApi.activate(record.id)
    message.success(`Đã mở khóa tài khoản: ${record.username}`)
    await fetchUsers()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể mở khóa tài khoản!'))
  }
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
    await fetchUsers()
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

.toolbar-actions { display: flex; align-items: center; justify-content: flex-end; flex-wrap: wrap; gap: 10px; }

@media (max-width: 767px) {
  .toolbar { align-items: stretch; flex-direction: column; gap: 14px; }
  .toolbar-actions { justify-content: stretch; }
  .toolbar-actions > * { width: 100% !important; }
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}
</style>
