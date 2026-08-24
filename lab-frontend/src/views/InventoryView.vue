<template>
  <div class="inventory-container">
    <div class="toolbar">
      <div>
        <h2>Kiểm kê tài sản</h2>
        <p>Tạo đợt kiểm kê theo phạm vi, quét QR và theo dõi chênh lệch thực tế.</p>
      </div>
      <a-button type="primary" @click="showCreate = true">Tạo đợt kiểm kê</a-button>
    </div>

    <a-card :bordered="false">
      <a-table :data-source="sessions" :columns="columns" :loading="loading" row-key="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'"><StatusBadge :status="record.status" /></template>
          <template v-else-if="column.key === 'progress'">
            {{ record.found + record.wrongLocation + record.damaged }}/{{ record.total }} đã quét
            <a-progress :percent="progress(record)" size="small" />
          </template>
          <template v-else-if="column.key === 'startedAt'">{{ formatDate(record.startedAt) }}</template>
          <template v-else-if="column.key === 'action'">
            <a-button type="link" size="small" @click="openDetail(record)">Chi tiết</a-button>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="showCreate" title="Tạo đợt kiểm kê" ok-text="Tạo" cancel-text="Hủy" :confirm-loading="creating" @ok="createSession">
      <a-form layout="vertical">
        <a-form-item label="Tên đợt kiểm kê" required><a-input v-model:value="createForm.name" placeholder="Ví dụ: Kiểm kê quý III/2026" /></a-form-item>
        <a-form-item label="Vị trí phạm vi"><a-select v-model:value="createForm.locationNodeId" allow-clear placeholder="Tất cả vị trí"><a-select-option v-for="location in locations" :key="location.id" :value="location.id">{{ location.code }} — {{ location.name }}</a-select-option></a-select></a-form-item>
        <a-form-item label="Danh mục phạm vi"><a-select v-model:value="createForm.assetCategoryId" allow-clear placeholder="Tất cả danh mục"><a-select-option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</a-select-option></a-select></a-form-item>
      </a-form>
    </a-modal>

    <a-drawer v-model:open="detailOpen" title="Chi tiết đợt kiểm kê" width="720" @close="selectedSession = null">
      <template v-if="selectedSession">
        <a-descriptions bordered :column="1" size="small">
          <a-descriptions-item label="Mã đợt">{{ selectedSession.code }}</a-descriptions-item>
          <a-descriptions-item label="Tên đợt">{{ selectedSession.name }}</a-descriptions-item>
          <a-descriptions-item label="Trạng thái"><StatusBadge :status="selectedSession.status" /></a-descriptions-item>
        </a-descriptions>
        <a-divider />
        <a-space direction="vertical" style="width: 100%">
          <a-input-search v-model:value="scanToken" placeholder="Nhập QR token để ghi nhận nhanh" enter-button="Ghi nhận" :loading="scanning" @search="scanByToken" />
          <a-alert v-if="scanMessage" :type="scanMessageType" :message="scanMessage" show-icon />
        </a-space>
        <a-table :data-source="selectedSession.items" :columns="itemColumns" row-key="id" size="small" style="margin-top: 16px" :pagination="{ pageSize: 8 }">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'"><StatusBadge :status="record.status" /></template>
            <template v-else-if="column.key === 'scannedAt'">{{ record.scannedAt ? formatDate(record.scannedAt) : 'Chưa quét' }}</template>
          </template>
        </a-table>
        <a-button v-if="selectedSession.status === 'INVENTORY_OPEN'" danger block @click="completeSession">Kết thúc đợt kiểm kê</a-button>
      </template>
    </a-drawer>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import StatusBadge from '../components/StatusBadge.vue'
import { inventoryApi } from '../api/inventoryApi'
import { locationApi } from '../api/locationApi'
import { assetCategoryApi } from '../api/assetCategoryApi'

const sessions = ref([])
const locations = ref([])
const categories = ref([])
const loading = ref(false)
const creating = ref(false)
const showCreate = ref(false)
const createForm = ref({ name: '', locationNodeId: null, assetCategoryId: null })
const detailOpen = ref(false)
const selectedSession = ref(null)
const scanToken = ref('')
const scanning = ref(false)
const scanMessage = ref('')
const scanMessageType = ref('success')

const columns = [
  { title: 'Mã đợt', dataIndex: 'code', key: 'code' },
  { title: 'Tên đợt', dataIndex: 'name', key: 'name' },
  { title: 'Tiến độ', key: 'progress', width: 190 },
  { title: 'Thiếu/chưa quét', key: 'missing', customRender: ({ record }) => `${record.missing}/${record.total}` },
  { title: 'Trạng thái', key: 'status' },
  { title: 'Bắt đầu', key: 'startedAt' },
  { title: 'Thao tác', key: 'action' }
]
const itemColumns = [
  { title: 'Tài sản', dataIndex: 'equipmentName', key: 'equipmentName' },
  { title: 'Mã tài sản', dataIndex: 'assetCode', key: 'assetCode' },
  { title: 'Vị trí dự kiến', dataIndex: 'expectedLocation', key: 'expectedLocation' },
  { title: 'Kết quả', key: 'status' },
  { title: 'Thời gian quét', key: 'scannedAt' }
]

const formatDate = value => value ? new Date(value).toLocaleString('vi-VN') : '—'
const progress = record => record.total ? Math.round(((record.found + record.wrongLocation + record.damaged) / record.total) * 100) : 0

const fetchAll = async () => {
  loading.value = true
  try {
    sessions.value = await inventoryApi.getAll() || []
    locations.value = await locationApi.getAll() || []
    categories.value = await assetCategoryApi.getAll() || []
  } catch (error) {
    message.error(error.response?.data?.message || 'Không tải được dữ liệu kiểm kê!')
  } finally { loading.value = false }
}

const createSession = async () => {
  if (!createForm.value.name.trim()) { message.warning('Vui lòng nhập tên đợt kiểm kê!'); return }
  creating.value = true
  try {
    await inventoryApi.create(createForm.value)
    showCreate.value = false
    createForm.value = { name: '', locationNodeId: null, assetCategoryId: null }
    message.success('Đã tạo đợt kiểm kê.')
    await fetchAll()
  } catch (error) { message.error(error.response?.data?.message || 'Không tạo được đợt kiểm kê!') }
  finally { creating.value = false }
}

const openDetail = async record => {
  try {
    selectedSession.value = await inventoryApi.getById(record.id)
    detailOpen.value = true
    scanToken.value = ''
    scanMessage.value = ''
  } catch (error) { message.error(error.response?.data?.message || 'Không tải được chi tiết kiểm kê!') }
}

const scanByToken = async value => {
  const token = value.trim().replace(/^DEVICE_TOKEN:/, '')
  if (!token || !selectedSession.value) return
  scanning.value = true
  try {
    await inventoryApi.scan(selectedSession.value.id, { qrToken: token, status: 'INVENTORY_FOUND' })
    selectedSession.value = await inventoryApi.getById(selectedSession.value.id)
    scanToken.value = ''
    scanMessageType.value = 'success'
    scanMessage.value = 'Đã ghi nhận tài sản.'
    await fetchAll()
  } catch (error) {
    scanMessageType.value = 'error'
    scanMessage.value = error.response?.data?.message || 'Không ghi nhận được QR.'
  } finally { scanning.value = false }
}

const completeSession = async () => {
  try {
    await inventoryApi.complete(selectedSession.value.id)
    message.success('Đã kết thúc đợt kiểm kê; tài sản chưa quét được đánh dấu thiếu.')
    selectedSession.value = await inventoryApi.getById(selectedSession.value.id)
    await fetchAll()
  } catch (error) { message.error(error.response?.data?.message || 'Không thể kết thúc đợt kiểm kê!') }
}

onMounted(fetchAll)
</script>

<style scoped>
.inventory-container { padding: 0; }
.toolbar { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 24px; }
.toolbar h2 { margin: 0; font-weight: 600; }
.toolbar p { color: #64748b; margin: 6px 0 0; }
</style>
