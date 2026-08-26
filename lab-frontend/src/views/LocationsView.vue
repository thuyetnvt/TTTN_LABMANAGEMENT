<template>
  <div class="locations-page">
    <div class="page-heading">
      <div>
        <h2>Cây vị trí tài sản</h2>
        <p>Quản lý phòng lab, khu vực, tủ/kệ/bàn và ngăn bằng dữ liệu thật.</p>
      </div>
      <a-button type="primary" @click="openCreate">Thêm vị trí</a-button>
    </div>

    <a-card :bordered="false">
      <a-table bordered :data-source="locations" :columns="columns" :loading="loading" row-key="id" :scroll="{ x: 1240 }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'parent'">
            {{ parentName(record.parentId) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'default'">{{ record.isActive ? 'Đang sử dụng' : 'Ngừng sử dụng' }}</a-tag>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-tooltip title="Sửa vị trí">
                <a-button type="link" size="small" aria-label="Sửa vị trí" @click="openEdit(record)">
                  <template #icon><EditOutlined /></template>
                </a-button>
              </a-tooltip>
              <a-tooltip :title="deleteLocationLabel(record)">
                <a-button
                  type="link"
                  danger
                  size="small"
                  :aria-label="deleteLocationLabel(record)"
                  :disabled="!canDeleteLocation(record)"
                  @click="removeLocation(record)"
                >
                  <template #icon><DeleteOutlined /></template>
                </a-button>
              </a-tooltip>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="modalOpen" :title="editing ? 'Sửa vị trí' : 'Thêm vị trí'" :confirm-loading="saving" ok-text="Lưu" cancel-text="Hủy" @ok="submit">
      <a-form layout="vertical">
        <a-form-item label="Mã vị trí" required><a-input v-model:value="form.code" placeholder="VD: LAB-IOT-A1" /></a-form-item>
        <a-form-item label="Tên vị trí" required><a-input v-model:value="form.name" placeholder="VD: Tủ linh kiện A1" /></a-form-item>
        <a-form-item label="Loại vị trí" required>
          <a-select v-model:value="form.type" :options="typeOptions" />
        </a-form-item>
        <a-form-item label="Vị trí cha">
          <a-select v-model:value="form.parentId" allow-clear placeholder="Không có vị trí cha">
            <a-select-option v-for="location in parentOptions" :key="location.id" :value="location.id">{{ location.code }} — {{ location.name }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Mô tả"><a-textarea v-model:value="form.description" :rows="3" /></a-form-item>
        <a-form-item><a-checkbox v-model:checked="form.isActive">Đang sử dụng</a-checkbox></a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import { locationApi } from '../api/locationApi'
import { getApiErrorMessage } from '../utils/apiError'

const locations = ref([])
const loading = ref(false)
const saving = ref(false)
const modalOpen = ref(false)
const editing = ref(null)
const form = reactive({ code: '', name: '', type: 'ROOM', parentId: null, description: '', isActive: true })
const typeOptions = [
  { value: 'ROOM', label: 'Phòng lab' },
  { value: 'AREA', label: 'Khu vực' },
  { value: 'CABINET', label: 'Tủ/Kệ/Bàn' },
  { value: 'SHELF', label: 'Ngăn' }
]
const columns = [
  { title: 'Mã', dataIndex: 'code', key: 'code', width: 150 },
  { title: 'Tên vị trí', dataIndex: 'name', key: 'name', width: 240 },
  { title: 'Loại', dataIndex: 'type', key: 'type', width: 150 },
  { title: 'Vị trí cha', key: 'parent', width: 240 },
  { title: 'Số tài sản', dataIndex: 'equipmentCount', key: 'equipmentCount', width: 130 },
  { title: 'Trạng thái', key: 'status', width: 150 },
  { title: 'Thao tác', key: 'action', fixed: 'right', width: 180, align: 'center' }
]

const parentOptions = computed(() => locations.value.filter(item => item.id !== editing.value?.id && item.isActive))
const parentName = (id) => locations.value.find(item => item.id === id)?.name || '—'
const canDeleteLocation = (record) => Number(record.equipmentCount || 0) === 0
const deleteLocationLabel = (record) => canDeleteLocation(record)
  ? 'Xóa vị trí'
  : `Không thể xóa: vị trí còn ${record.equipmentCount} tài sản`

const fetchLocations = async () => {
  loading.value = true
  try {
    locations.value = await locationApi.getAll() || []
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách vị trí.'))
  } finally {
    loading.value = false
  }
}

const resetForm = () => Object.assign(form, { code: '', name: '', type: 'ROOM', parentId: null, description: '', isActive: true })
const openCreate = () => { editing.value = null; resetForm(); modalOpen.value = true }
const openEdit = (record) => { editing.value = record; Object.assign(form, record); modalOpen.value = true }

const submit = async () => {
  if (!form.code.trim() || !form.name.trim() || !form.type) {
    message.warning('Vui lòng nhập mã, tên và loại vị trí.')
    return
  }
  saving.value = true
  try {
    if (editing.value) await locationApi.update(editing.value.id, { ...form })
    else await locationApi.create({ ...form })
    message.success(editing.value ? 'Đã cập nhật vị trí.' : 'Đã thêm vị trí.')
    modalOpen.value = false
    await fetchLocations()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể lưu vị trí.'))
  } finally {
    saving.value = false
  }
}

const removeLocation = (record) => {
  if (!canDeleteLocation(record)) {
    message.warning(`Không thể xóa vị trí vì đang có ${record.equipmentCount} tài sản. Hãy chuyển tài sản hoặc ngừng sử dụng vị trí.`)
    return
  }
  Modal.confirm({
    title: 'Xóa vị trí',
    content: `Bạn có chắc muốn xóa vị trí ${record.name}?`,
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try { await locationApi.remove(record.id); message.success('Đã xóa vị trí.'); await fetchLocations() }
      catch (error) { message.error(getApiErrorMessage(error, 'Không thể xóa vị trí.')) }
    }
  })
}

onMounted(fetchLocations)
</script>

<style scoped>
.locations-page { padding: 0; }
.page-heading { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 20px; }
.page-heading h2 { margin: 0 0 6px; }
.page-heading p { margin: 0; color: #64748b; }
@media (max-width: 640px) { .page-heading { flex-direction: column; } }
</style>
