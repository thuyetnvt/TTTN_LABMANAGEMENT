<template>
  <div class="inventory-container">
    <PageHeader title="Kiểm kê tài sản" subtitle="Tạo đợt kiểm kê theo phạm vi, quét QR và theo dõi chênh lệch thực tế.">
      <template #actions><a-button type="primary" @click="showCreate = true">Tạo đợt kiểm kê</a-button></template>
    </PageHeader>

    <div class="inventory-filters">
      <a-input-search v-model:value="searchQuery" allow-clear placeholder="Mã hoặc tên đợt kiểm kê..." class="filter-search" @search="applyFilters" />
      <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
        <a-select-option value="">Tất cả</a-select-option>
        <a-select-option :value="STATUS.INVENTORY_OPEN">Đang kiểm kê</a-select-option>
        <a-select-option :value="STATUS.INVENTORY_REVIEWING">Đang đối soát</a-select-option>
        <a-select-option :value="STATUS.INVENTORY_COMPLETED">Đã kết thúc</a-select-option>
      </a-select>
    </div>

    <a-card :bordered="false">
      <div class="inventory-desktop-table">
        <a-table :data-source="sessions" :columns="columns" :loading="loading" row-key="id" bordered :scroll="{ x: 1100 }" :pagination="tablePagination" @change="handleTableChange">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'"><StatusBadge :status="record.status" type="inventory" /></template>
            <template v-else-if="column.key === 'progress'">
              {{ record.found + record.wrongLocation + record.damaged }}/{{ record.total }} đã quét
              <a-progress :percent="progress(record)" size="small" />
            </template>
            <template v-else-if="column.key === 'startedAt'">{{ formatDate(record.startedAt) }}</template>
            <template v-else-if="column.key === 'action'">
              <a-tooltip title="Xem chi tiết">
                <a-button
                  type="link"
                  class="table-detail-action"
                  aria-label="Xem chi tiết đợt kiểm kê"
                  @click="openDetail(record)"
                >
                  <template #icon><EyeOutlined /></template>
                </a-button>
              </a-tooltip>
            </template>
          </template>
        </a-table>
      </div>
    </a-card>
    <ResponsiveDataList :items="sessions" :loading="loading" :pagination="tablePagination" empty-description="Chưa có đợt kiểm kê" @change="handleTableChange">
      <template #default="{ item }">
        <div class="mobile-session-header"><strong>{{ item.name }}</strong><StatusBadge :status="item.status" type="inventory" /></div>
        <div class="mobile-session-meta">{{ item.code }} · {{ item.found + item.wrongLocation + item.damaged }}/{{ item.total }} đã quét</div>
        <a-progress :percent="progress(item)" size="small" />
        <a-tooltip title="Xem chi tiết">
          <a-button
            type="link"
            class="table-detail-action"
            aria-label="Xem chi tiết đợt kiểm kê"
            @click="openDetail(item)"
          >
            <template #icon><EyeOutlined /></template>
          </a-button>
        </a-tooltip>
      </template>
    </ResponsiveDataList>

    <a-modal v-model:open="showCreate" title="Tạo đợt kiểm kê" ok-text="Tạo" cancel-text="Hủy" :confirm-loading="creating" @ok="createSession">
      <a-form layout="vertical">
        <a-form-item label="Tên đợt kiểm kê" required><a-input v-model:value="createForm.name" placeholder="Ví dụ: Kiểm kê quý III/2026" /></a-form-item>
        <a-form-item label="Vị trí phạm vi"><a-select v-model:value="createForm.locationNodeId" allow-clear placeholder="Tất cả vị trí"><a-select-option v-for="location in locations" :key="location.id" :value="location.id">{{ location.code }} — {{ location.name }}</a-select-option></a-select></a-form-item>
        <a-form-item label="Danh mục phạm vi"><a-select v-model:value="createForm.assetCategoryId" allow-clear placeholder="Tất cả danh mục"><a-select-option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</a-select-option></a-select></a-form-item>
      </a-form>
    </a-modal>

    <a-drawer v-model:open="detailOpen" title="Chi tiết đợt kiểm kê" width="720" @close="closeDetail">
      <template v-if="selectedSession">
        <a-descriptions bordered :column="1" size="small">
          <a-descriptions-item label="Mã đợt">{{ selectedSession.code }}</a-descriptions-item>
          <a-descriptions-item label="Tên đợt">{{ selectedSession.name }}</a-descriptions-item>
          <a-descriptions-item label="Trạng thái"><StatusBadge :status="selectedSession.status" type="inventory" /></a-descriptions-item>
        </a-descriptions>
        <a-divider />
        <a-space direction="vertical" size="middle" style="width: 100%">
          <a-space wrap>
            <a-button v-if="selectedSession.status === STATUS.INVENTORY_OPEN" @click="toggleCamera">{{ cameraOpen ? 'Đóng camera' : 'Mở camera quét QR' }}</a-button>
            <a-button @click="downloadReport('excel')">Xuất Excel kiểm kê</a-button>
            <a-button @click="downloadReport('pdf')">Xuất PDF kiểm kê</a-button>
          </a-space>
          <a-alert
            type="info"
            show-icon
            message="Kiểm kê tài sản định danh theo mã QR"
            description="Mỗi mã tài sản tương ứng 1 đơn vị: số lượng sổ sách là 1; số lượng thực tế là 1 khi tìm thấy (kể cả sai vị trí hoặc hư hỏng), 0 khi thất lạc. Vật tư tiêu hao không nằm trong đợt này; số lượng được quản lý riêng ở Quản lý lô."
          />
          <a-alert
            v-if="selectedSession.status === STATUS.INVENTORY_REVIEWING"
            type="warning"
            show-icon
            :message="`Đang đối soát: còn ${selectedSession.unreviewedCount || 0} chênh lệch chưa xử lý`"
            description="Duyệt từng tài sản sai vị trí, hư hỏng hoặc thất lạc trước khi kết thúc đợt kiểm kê."
          />
          <a-row v-if="selectedSession.status === STATUS.INVENTORY_OPEN" :gutter="[10, 10]">
            <a-col :xs="24" :md="14">
              <a-select v-model:value="actualLocationNodeId" allow-clear placeholder="Vị trí thực tế đang quét" style="width: 100%">
                <a-select-option v-for="location in locations" :key="location.id" :value="location.id">{{ location.code }} — {{ location.name }}</a-select-option>
              </a-select>
            </a-col>
            <a-col :xs="24" :md="10">
              <a-select v-model:value="scanResultStatus" style="width: 100%">
                <a-select-option :value="STATUS.INVENTORY_FOUND">Tìm thấy, bình thường</a-select-option>
                <a-select-option :value="STATUS.INVENTORY_DAMAGED">Tìm thấy nhưng hư hỏng</a-select-option>
              </a-select>
            </a-col>
          </a-row>
          <div v-if="cameraOpen && selectedSession.status === STATUS.INVENTORY_OPEN" class="continuous-scan-hint">
            <span class="continuous-scan-dot" aria-hidden="true"></span>
            <span>Quét liên tục: đưa lần lượt từng mã QR vào khung hình.</span>
            <span v-if="scanQueueLength">Đang xử lý {{ scanQueueLength }} mã.</span>
            <span v-if="continuousScanStats.success || continuousScanStats.failed">
              Đã ghi nhận {{ continuousScanStats.success }} mã<span v-if="continuousScanStats.failed"> · Lỗi {{ continuousScanStats.failed }}</span>.
            </span>
          </div>
          <QRScanner v-if="cameraOpen && selectedSession.status === STATUS.INVENTORY_OPEN" :continuous="true" @scan-success="onScanSuccessInventory" class="qr-reader" />
          <a-input-search v-if="selectedSession.status === STATUS.INVENTORY_OPEN" v-model:value="scanToken" placeholder="Nhập QR token để ghi nhận nhanh" enter-button="Ghi nhận" :loading="scanning" @search="scanByToken" class="inventory-search-input" />
          <a-alert v-if="scanMessage" :type="scanMessageType" :message="scanMessage" show-icon />
        </a-space>
        <div class="inventory-item-filters">
          <a-input-search v-model:value="itemSearchQuery" allow-clear placeholder="Tìm tài sản trong đợt..." class="filter-search" @search="applyItemFilters" />
          <a-select v-model:value="itemStatusFilter" allow-clear placeholder="Kết quả" class="status-filter" @change="applyItemFilters">
            <a-select-option value="">Tất cả</a-select-option>
            <a-select-option :value="STATUS.INVENTORY_PENDING">Chưa quét</a-select-option>
            <a-select-option :value="STATUS.INVENTORY_FOUND">Đã tìm thấy</a-select-option>
            <a-select-option :value="STATUS.INVENTORY_WRONG_LOCATION">Sai vị trí</a-select-option>
            <a-select-option :value="STATUS.INVENTORY_DAMAGED">Hư hỏng</a-select-option>
            <a-select-option :value="STATUS.INVENTORY_MISSING">Thất lạc</a-select-option>
          </a-select>
        </div>
        <a-table :data-source="selectedSession.items" :columns="itemColumns" row-key="id" size="small" style="margin-top: 16px" :pagination="itemPagination" @change="handleItemTableChange">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'bookQuantity'">{{ record.bookQuantity ?? '—' }}</template>
            <template v-else-if="column.key === 'actualQuantity'">{{ record.actualQuantity ?? '—' }}</template>
            <template v-else-if="column.key === 'quantityDifference'">{{ record.quantityDifference ?? '—' }}</template>
            <template v-else-if="column.key === 'status'"><StatusBadge :status="record.status" type="inventory" /></template>
            <template v-else-if="column.key === 'scannedAt'">{{ record.scannedAt ? formatDate(record.scannedAt) : 'Chưa quét' }}</template>
            <template v-else-if="column.key === 'evidence'">
              <a-upload :before-upload="file => uploadEvidence(record, file)" :show-upload-list="false" accept=".jpg,.jpeg,.png,.webp,.pdf">
                <a-button size="small">Ảnh/file</a-button>
              </a-upload>
            </template>
            <template v-else-if="column.key === 'review'">
              <a-button
                v-if="selectedSession.status === STATUS.INVENTORY_REVIEWING && record.status !== STATUS.INVENTORY_FOUND && !record.reviewedAt"
                type="primary"
                size="small"
                @click="openReview(record)"
              >Xử lý</a-button>
              <a-tag v-else-if="record.reviewedAt || isScannedNormally(record)" color="success">Đã đối soát</a-tag>
              <span v-else-if="selectedSession.status === STATUS.INVENTORY_OPEN" class="muted">Chờ khóa quét</span>
              <span v-else>—</span>
            </template>
          </template>
        </a-table>
        <a-button v-if="selectedSession.status === STATUS.INVENTORY_OPEN" type="primary" block :disabled="scanning || scanQueueLength > 0" @click="startReview">Khóa quét & đối soát chênh lệch</a-button>
        <a-tooltip v-else-if="selectedSession.status === STATUS.INVENTORY_REVIEWING" :title="selectedSession.canComplete ? '' : `Còn ${selectedSession.unreviewedCount || 0} chênh lệch chưa xử lý`">
          <a-button danger block :disabled="!selectedSession.canComplete" @click="completeSession">Kết thúc đợt kiểm kê</a-button>
        </a-tooltip>
      </template>
    </a-drawer>

    <a-modal v-model:open="reviewOpen" title="Xử lý chênh lệch kiểm kê" ok-text="Lưu xử lý" cancel-text="Hủy" :confirm-loading="reviewing" @ok="submitReview">
      <a-form v-if="reviewItem" layout="vertical">
        <a-alert type="info" show-icon :message="`${reviewItem.equipmentName} — ${reviewItem.assetCode}`" style="margin-bottom: 16px" />
        <a-descriptions bordered size="small" :column="1" style="margin-bottom: 16px">
          <a-descriptions-item label="Kết quả"><StatusBadge :status="reviewItem.status" type="inventory" /></a-descriptions-item>
          <a-descriptions-item label="Vị trí dự kiến">{{ reviewItem.expectedLocation || '—' }}</a-descriptions-item>
          <a-descriptions-item label="Vị trí thực tế">{{ reviewItem.actualLocation || 'Chưa ghi nhận' }}</a-descriptions-item>
        </a-descriptions>
        <a-form-item label="Cách xử lý" required>
          <a-select v-model:value="reviewForm.resolution">
            <a-select-option v-for="option in reviewOptions" :key="option.value" :value="option.value">{{ option.label }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Ghi chú đối soát" :required="reviewForm.resolution === 'KEEP_RECORDED_LOCATION'">
          <a-textarea v-model:value="reviewForm.note" :rows="3" placeholder="Mô tả quyết định xử lý..." />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, reactive, ref, onMounted } from 'vue'
import { onUnmounted } from 'vue'
import { message } from 'ant-design-vue'
import { Upload } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import QRScanner from '../components/QRScanner.vue'
import StatusBadge from '../components/StatusBadge.vue'
import PageHeader from '../components/PageHeader.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { inventoryApi } from '../api/inventoryApi'
import { locationApi } from '../api/locationApi'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { STATUS } from '../constants/business'
import { formatVietnamDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})
const itemPagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const sessions = ref([])
const locations = ref([])
const categories = ref([])
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const itemSearchQuery = ref('')
const itemStatusFilter = ref(undefined)
const creating = ref(false)
const showCreate = ref(false)
const createForm = ref({ name: '', locationNodeId: null, assetCategoryId: null })
const detailOpen = ref(false)
const selectedSession = ref(null)
const scanToken = ref('')
const scanning = ref(false)
const scanMessage = ref('')
const scanMessageType = ref('success')
const cameraOpen = ref(false)
const scanQueueLength = ref(0)
const continuousScanStats = ref({ success: 0, failed: 0 })
const actualLocationNodeId = ref(null)
const scanResultStatus = ref(STATUS.INVENTORY_FOUND)
const reviewOpen = ref(false)
const reviewing = ref(false)
const reviewItem = ref(null)
const reviewForm = ref({ resolution: '', note: '' })

const scanQueue = []
const recentCameraScans = new Map()
let processingScanQueue = false
const duplicateScanWindowMs = 2500

const columns = [
  { title: 'Mã đợt', dataIndex: 'code', key: 'code', width: 150 },
  { title: 'Tên đợt', dataIndex: 'name', key: 'name', width: 230 },
  { title: 'Tiến độ', key: 'progress', width: 190 },
  { title: 'Chưa quét / Thất lạc', key: 'missing', width: 170, customRender: ({ record }) => inventoryDifferenceLabel(record) },
  { title: 'Trạng thái', key: 'status', width: 150 },
  { title: 'Bắt đầu', key: 'startedAt', width: 150 },
  { title: 'Thao tác', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 96, align: 'center' }
]
const itemColumns = [
  { title: 'Tài sản', dataIndex: 'equipmentName', key: 'equipmentName' },
  { title: 'Mã tài sản', dataIndex: 'assetCode', key: 'assetCode' },
  { title: 'Vị trí dự kiến', dataIndex: 'expectedLocation', key: 'expectedLocation' },
  { title: 'SL sổ sách', dataIndex: 'bookQuantity', key: 'bookQuantity', width: 100, align: 'center' },
  { title: 'SL thực tế', dataIndex: 'actualQuantity', key: 'actualQuantity', width: 100, align: 'center' },
  { title: 'Chênh lệch', dataIndex: 'quantityDifference', key: 'quantityDifference', width: 100, align: 'center' },
  { title: 'Kết quả', key: 'status' },
  { title: 'Thời gian quét', key: 'scannedAt' },
  { title: 'Minh chứng', key: 'evidence' },
  { title: 'Đối soát', key: 'review', align: 'center', width: 110 }
]

const reviewOptions = computed(() => {
  if (reviewItem.value?.status === STATUS.INVENTORY_WRONG_LOCATION) return [
    { value: 'UPDATE_LOCATION', label: 'Cập nhật tài sản sang vị trí thực tế' },
    { value: 'KEEP_RECORDED_LOCATION', label: 'Giữ vị trí trên hệ thống và ghi lý do' }
  ]
  if (reviewItem.value?.status === STATUS.INVENTORY_DAMAGED) return [
    { value: 'MARK_DAMAGED', label: 'Đánh dấu tài sản hỏng' }
  ]
  if (reviewItem.value?.status === STATUS.INVENTORY_MISSING) return [
    { value: 'MARK_MISSING', label: 'Đánh dấu tài sản thất lạc' }
  ]
  return []
})

const formatDate = value => formatVietnamDateTime(value)
const progress = record => record.total ? Math.round(((record.found + record.wrongLocation + record.damaged) / record.total) * 100) : 0
const isScannedNormally = record => record.status === STATUS.INVENTORY_FOUND && Boolean(record.scannedAt)
const inventoryDifferenceLabel = record => record.status === STATUS.INVENTORY_OPEN
  ? `${record.pending ?? Math.max(0, record.total - record.found - record.wrongLocation - record.damaged)} chưa quét`
  : `${record.missing || 0} thất lạc`

const fetchAll = async () => {
  loading.value = true
  try {
    const response = await inventoryApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    sessions.value = response.items || []
    tablePagination.total = response.total || 0
    locations.value = await locationApi.getAll() || []
    categories.value = await assetCategoryApi.getAll() || []
  } catch (error) {
    message.error(error.response?.data?.message || 'Không tải được dữ liệu kiểm kê!')
  } finally { loading.value = false }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchAll()
}

const handleTableChange = pager => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchAll()
}

const normalizeScanToken = value => String(value ?? '').trim().replace(/^DEVICE_TOKEN:/i, '')

const updateScanQueueLength = () => {
  scanQueueLength.value = scanQueue.length
}

const clearScanQueue = () => {
  scanQueue.splice(0, scanQueue.length)
  recentCameraScans.clear()
  updateScanQueueLength()
}

const refreshSessions = async () => {
  try {
    const response = await inventoryApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    sessions.value = response.items || []
    tablePagination.total = response.total || 0
  } catch {
    // Giữ nguyên chi tiết đang mở nếu chỉ lỗi làm mới danh sách đợt kiểm kê.
  }
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

const loadSessionDetail = async (sessionId, resetItems = false) => {
  if (resetItems) itemPagination.current = 1
  const [metadata, itemResponse] = await Promise.all([
    inventoryApi.getById(sessionId),
    inventoryApi.getItemsPaged(sessionId, {
      page: itemPagination.current,
      pageSize: itemPagination.pageSize,
      search: itemSearchQuery.value.trim() || undefined,
      status: itemStatusFilter.value
    })
  ])
  selectedSession.value = { ...metadata, items: itemResponse.items || [] }
  itemPagination.total = itemResponse.total || 0
}

const applyItemFilters = async () => {
  if (!selectedSession.value) return
  await loadSessionDetail(selectedSession.value.id, true)
}

const handleItemTableChange = async pager => {
  itemPagination.current = pager.pageSize === itemPagination.pageSize ? pager.current : 1
  itemPagination.pageSize = pager.pageSize
  if (selectedSession.value) await loadSessionDetail(selectedSession.value.id)
}

const openDetail = async record => {
  try {
    itemSearchQuery.value = ''
    itemStatusFilter.value = undefined
    await loadSessionDetail(record.id, true)
    actualLocationNodeId.value = selectedSession.value.locationNodeId || null
    scanResultStatus.value = STATUS.INVENTORY_FOUND
    detailOpen.value = true
    scanToken.value = ''
    scanMessage.value = ''
    cameraOpen.value = false
    continuousScanStats.value = { success: 0, failed: 0 }
    clearScanQueue()
  } catch (error) { message.error(error.response?.data?.message || 'Không tải được chi tiết kiểm kê!') }
}

const submitInventoryScan = async token => {
  if (!selectedSession.value) return
  const sessionId = selectedSession.value.id

  try {
    await inventoryApi.scan(sessionId, {
      qrToken: token,
      status: scanResultStatus.value,
      locationNodeId: actualLocationNodeId.value
    })
    if (selectedSession.value?.id === sessionId) {
      await loadSessionDetail(sessionId)
    }
    scanToken.value = ''
    scanMessageType.value = 'success'
    scanMessage.value = 'Đã ghi nhận tài sản.'
    continuousScanStats.value.success += 1
    return true
  } catch (error) {
    scanMessageType.value = 'error'
    scanMessage.value = error.response?.data?.message || error.message || 'Không ghi nhận được QR.'
    continuousScanStats.value.failed += 1
    return false
  }
}

const processScanQueue = async () => {
  if (processingScanQueue) return
  processingScanQueue = true
  scanning.value = true

  try {
    while (scanQueue.length > 0 && selectedSession.value) {
      const token = scanQueue.shift()
      updateScanQueueLength()
      await submitInventoryScan(token)
    }
    await refreshSessions()
  } finally {
    processingScanQueue = false
    scanning.value = false
    updateScanQueueLength()
  }
}

const enqueueScan = (value, fromCamera = false) => {
  const token = normalizeScanToken(value)
  if (!token || !selectedSession.value || scanQueue.includes(token)) return

  if (fromCamera) {
    const now = Date.now()
    const previousScanAt = recentCameraScans.get(token)
    if (previousScanAt && now - previousScanAt < duplicateScanWindowMs) return
    recentCameraScans.set(token, now)
  }

  scanQueue.push(token)
  updateScanQueueLength()
  void processScanQueue()
}

const scanByToken = value => {
  enqueueScan(value)
}

const toggleCamera = () => {
  cameraOpen.value = !cameraOpen.value
  if (!cameraOpen.value) {
    clearScanQueue()
  }
}

const onScanSuccessInventory = (decoded) => {
  enqueueScan(decoded, true)
}

const closeDetail = () => {
  cameraOpen.value = false
  clearScanQueue()
  scanToken.value = ''
  scanMessage.value = ''
  continuousScanStats.value = { success: 0, failed: 0 }
  selectedSession.value = null
}

const uploadEvidence = async (record, file) => {
  try {
    await inventoryApi.uploadEvidence(selectedSession.value.id, record.id, file)
    message.success('Đã lưu minh chứng kiểm kê.')
    await loadSessionDetail(selectedSession.value.id)
  } catch (error) {
    message.error(error.response?.data?.message || error.message || 'Không tải được minh chứng.')
  }
  return Upload.LIST_IGNORE
}

const downloadReport = async type => {
  try {
    const blob = type === 'pdf'
      ? await inventoryApi.exportPdf(selectedSession.value.id)
      : await inventoryApi.exportExcel(selectedSession.value.id)
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = type === 'pdf' ? `KiemKe_${selectedSession.value.code}.pdf` : `KiemKe_${selectedSession.value.code}.xlsx`
    anchor.click()
    URL.revokeObjectURL(url)
  } catch (error) { message.error(error.response?.data?.message || error.message || 'Không xuất được báo cáo.') }
}

const startReview = async () => {
  cameraOpen.value = false
  clearScanQueue()
  try {
    const result = await inventoryApi.startReview(selectedSession.value.id)
    message.success(result?.message || 'Đã chuyển sang bước đối soát.')
    await loadSessionDetail(selectedSession.value.id)
    await refreshSessions()
  } catch (error) {
    message.error(error.response?.data?.message || 'Không thể bắt đầu đối soát!')
  }
}

const openReview = record => {
  reviewItem.value = record
  const defaultResolution = record.status === STATUS.INVENTORY_WRONG_LOCATION
    ? 'UPDATE_LOCATION'
    : record.status === STATUS.INVENTORY_DAMAGED
      ? 'MARK_DAMAGED'
      : 'MARK_MISSING'
  reviewForm.value = { resolution: defaultResolution, note: '' }
  reviewOpen.value = true
}

const submitReview = async () => {
  if (!reviewItem.value || !reviewForm.value.resolution) return
  reviewing.value = true
  try {
    await inventoryApi.reviewItem(selectedSession.value.id, reviewItem.value.id, reviewForm.value)
    message.success('Đã xử lý chênh lệch và đồng bộ tài sản.')
    reviewOpen.value = false
    await loadSessionDetail(selectedSession.value.id)
    await refreshSessions()
  } catch (error) {
    message.error(error.response?.data?.message || 'Không thể xử lý chênh lệch!')
  } finally {
    reviewing.value = false
  }
}

const completeSession = async () => {
  cameraOpen.value = false
  clearScanQueue()
  try {
    await inventoryApi.complete(selectedSession.value.id)
    message.success('Đã kết thúc đợt kiểm kê sau khi đối soát đầy đủ.')
    await loadSessionDetail(selectedSession.value.id)
    await fetchAll()
  } catch (error) { message.error(error.response?.data?.message || 'Không thể kết thúc đợt kiểm kê!') }
}

onMounted(fetchAll)
</script>

<style scoped>
.inventory-desktop-table { display: block; }
.mobile-session-header { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.mobile-session-meta { margin: 8px 0 4px; color: var(--color-secondary); font-size: 13px; }
.muted { color: #8c8c8c; font-size: 13px; }
@media (max-width: 767px) { .inventory-desktop-table { display: none; } }
</style>

<style scoped>
.inventory-container { padding: 0; }
.inventory-filters { display: flex; flex-wrap: wrap; gap: 10px; margin: 0 0 16px; }
.inventory-item-filters { display: grid; grid-template-columns: minmax(220px, 1fr) 260px; gap: 10px; margin-top: 16px; }
.toolbar { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 24px; }
.toolbar h2 { margin: 0; font-weight: 600; }
.toolbar p { color: #64748b; margin: 6px 0 0; }
.qr-reader { max-width: 360px; margin: 12px 0; }
.continuous-scan-hint {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  color: var(--color-secondary, #64748b);
  background: #f7f8fa;
  border-radius: 6px;
  font-size: 13px;
}
.continuous-scan-dot {
  width: 8px;
  height: 8px;
  flex: 0 0 auto;
  border-radius: 50%;
  background: var(--color-primary, #d97757);
}
.inventory-search-input :deep(.ant-input-group) {
  height: 40px;
}
.inventory-search-input :deep(.ant-input-group-addon) {
  flex: 0 0 auto !important;
  width: auto !important;
  padding: 0 !important;
  border: 0 !important;
}
.inventory-search-input :deep(.ant-input-search-button) {
  width: auto !important;
  min-width: 88px !important;
  height: 40px !important;
  padding: 0 16px !important;
  border-left: 1px solid rgba(0, 0, 0, 0.15) !important;
  border-radius: 0 8px 8px 0 !important;
  white-space: nowrap;
}
@media (max-width: 767px) {
  .inventory-filters > * { width: 100% !important; }
  .inventory-item-filters { grid-template-columns: 1fr; }
}
</style>
