<template>
  <div class="schedule-page">
    <div class="toolbar">
      <div>
        <h2>Bảo trì định kỳ</h2>
        <p class="muted">Theo dõi hạn đến và tạo phiếu bảo trì theo kế hoạch.</p>
      </div>
      <a-button type="primary" @click="openCreate">+ Tạo kế hoạch</a-button>
    </div>

    <a-card :bordered="false">
      <a-table :data-source="schedules" :columns="columns" :loading="loading" row-key="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'nextDueAt'">
            <a-tag :color="record.isDue ? 'red' : 'blue'">{{ formatDate(record.nextDueAt) }}</a-tag>
          </template>
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'green' : 'default'">{{ record.isActive ? 'Đang bật' : 'Tạm tắt' }}</a-tag>
          </template>
          <template v-if="column.key === 'action'">
            <a-space>
              <a-button v-if="record.isActive" size="small" type="primary" @click="generate(record)">Tạo phiếu</a-button>
              <a-button size="small" @click="openEdit(record)">Sửa</a-button>
              <a-button v-if="isAdminRole(role)" size="small" danger @click="remove(record)">Xóa</a-button>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalOpen"
      :title="editing ? 'Sửa kế hoạch' : 'Tạo kế hoạch bảo trì'"
      :width="900"
      :confirm-loading="saving"
      ok-text="Lưu"
      cancel-text="Hủy"
      wrap-class-name="maintenance-form-modal"
      @ok="save"
    >
      <a-form layout="vertical">
        <div class="maintenance-form-grid">
          <div class="form-column">
            <a-form-item label="Thiết bị" required>
              <a-select v-model:value="form.equipmentId" show-search option-filter-prop="label" :disabled="!!editing">
                <a-select-option v-for="equipment in equipments" :key="equipment.id" :value="equipment.id" :label="`${equipment.name} ${equipment.serial}`">{{ equipment.name }} — {{ equipment.serial }}</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="Tên kế hoạch" required>
              <a-input v-model:value="form.name" placeholder="VD: Hiệu chuẩn hàng quý" />
            </a-form-item>
            <a-form-item label="Chu kỳ" required>
              <a-space class="maintenance-cycle-fields">
                <a-input-number v-model:value="form.intervalDays" :min="1" :max="3650" />
                <a-select v-model:value="form.intervalUnit">
                  <a-select-option value="DAY">Ngày</a-select-option>
                  <a-select-option value="WEEK">Tuần</a-select-option>
                  <a-select-option value="MONTH">Tháng</a-select-option>
                  <a-select-option value="QUARTER">Quý</a-select-option>
                  <a-select-option value="YEAR">Năm</a-select-option>
                </a-select>
              </a-space>
            </a-form-item>
            <a-form-item label="Hạn kế tiếp">
              <a-input v-model:value="form.nextDueAt" type="date" />
            </a-form-item>
          </div>

          <div class="form-column">
            <a-form-item label="Ghi chú">
              <a-textarea v-model:value="form.notes" :rows="5" placeholder="Ghi chú cho kế hoạch bảo trì" />
            </a-form-item>
            <a-form-item label="Checklist bảo trì">
              <a-textarea v-model:value="form.checklist" :rows="5" placeholder="Mỗi dòng một hạng mục kiểm tra" />
            </a-form-item>
            <a-form-item v-if="editing" label="Trạng thái">
              <a-switch v-model:checked="form.isActive" checked-children="Bật" un-checked-children="Tắt" />
            </a-form-item>
          </div>
        </div>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole } from '../constants/business'
import { equipmentApi } from '../api/equipmentApi'
import { maintenanceScheduleApi } from '../api/maintenanceScheduleApi'

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const schedules = ref([])
const equipments = ref([])
const loading = ref(false)
const saving = ref(false)
const modalOpen = ref(false)
const editing = ref(null)
const form = ref({ equipmentId: null, name: '', intervalDays: 90, intervalUnit: 'DAY', nextDueAt: '', notes: '', checklist: '', isActive: true })
const columns = [
  { title: 'Thiết bị', dataIndex: 'device', key: 'device' },
  { title: 'Kế hoạch', dataIndex: 'name', key: 'name' },
  { title: 'Chu kỳ', dataIndex: 'intervalDays', key: 'intervalDays', customRender: ({ record }) => `${record.intervalDays} ${({ DAY: 'ngày', WEEK: 'tuần', MONTH: 'tháng', QUARTER: 'quý', YEAR: 'năm' })[record.intervalUnit] || 'ngày'}` },
  { title: 'Hạn kế tiếp', dataIndex: 'nextDueAt', key: 'nextDueAt' },
  { title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive' },
  { title: 'Hành động', key: 'action' }
]

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'
const toDateInput = (value) => value ? new Date(value).toISOString().slice(0, 10) : ''

const load = async () => {
  loading.value = true
  try { schedules.value = await maintenanceScheduleApi.getAll() || [] } catch { message.error('Không tải được kế hoạch bảo trì.') } finally { loading.value = false }
}

const openCreate = async () => {
  equipments.value = await equipmentApi.getAll() || []
  editing.value = null
  form.value = { equipmentId: null, name: '', intervalDays: 90, intervalUnit: 'DAY', nextDueAt: '', notes: '', checklist: '', isActive: true }
  modalOpen.value = true
}

const openEdit = (record) => {
  editing.value = record
  form.value = { equipmentId: record.equipmentId, name: record.name, intervalDays: record.intervalDays, intervalUnit: record.intervalUnit || 'DAY', nextDueAt: toDateInput(record.nextDueAt), notes: record.notes || '', checklist: record.checklist || '', isActive: record.isActive }
  modalOpen.value = true
}

const save = async () => {
  if (!form.value.equipmentId || !form.value.name?.trim() || !form.value.intervalDays) return message.warning('Vui lòng nhập đủ thông tin.')
  saving.value = true
  try {
    const payload = { ...form.value, name: form.value.name.trim(), nextDueAt: form.value.nextDueAt ? new Date(`${form.value.nextDueAt}T00:00:00`).toISOString() : null }
    if (editing.value) await maintenanceScheduleApi.update(editing.value.id, payload)
    else await maintenanceScheduleApi.create(payload)
    message.success('Đã lưu kế hoạch bảo trì.')
    modalOpen.value = false
    await load()
  } catch (error) { message.error(error?.response?.data?.message || 'Không thể lưu kế hoạch.') } finally { saving.value = false }
}

const generate = (record) => Modal.confirm({ title: 'Tạo phiếu bảo trì?', content: `Tạo phiếu cho ${record.device} theo kế hoạch “${record.name}”?`, okText: 'Tạo', cancelText: 'Hủy', onOk: async () => {
  try { await maintenanceScheduleApi.generate(record.id); message.success('Đã tạo phiếu bảo trì.'); await load() } catch (error) { message.error(error?.response?.data?.message || 'Không thể tạo phiếu.') }
} })

const remove = (record) => Modal.confirm({ title: 'Xóa kế hoạch?', content: record.name, okType: 'danger', okText: 'Xóa', cancelText: 'Hủy', onOk: async () => {
  try { await maintenanceScheduleApi.delete(record.id); message.success('Đã xóa kế hoạch.'); await load() } catch { message.error('Không thể xóa kế hoạch.') }
} })

onMounted(async () => { equipments.value = await equipmentApi.getAll() || []; await load() })
</script>

<style scoped>
.schedule-page { padding: 0; }
.toolbar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
h2 { margin: 0; }
.muted { margin: 5px 0 0; color: #777; }

:global(.maintenance-form-modal .ant-modal) {
  max-width: calc(100vw - 32px);
}

:global(.maintenance-form-modal .ant-modal-footer) {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.maintenance-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px 24px;
  align-items: start;
}

.form-column {
  min-width: 0;
}

.maintenance-cycle-fields {
  display: flex;
  width: 100%;
}

.maintenance-cycle-fields :deep(.ant-input-number) {
  flex: 1;
  min-width: 0;
}

.maintenance-cycle-fields :deep(.ant-select) {
  flex: 1;
  min-width: 0;
}

@media (max-width: 767px) {
  :global(.maintenance-form-modal .ant-modal) {
    max-width: calc(100vw - 32px);
  }

  .maintenance-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
