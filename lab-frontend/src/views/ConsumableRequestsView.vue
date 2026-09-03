<template>
  <div class="asset-requests-container">
    <div class="toolbar">
      <h2>{{ isManager ? 'Duyệt và bàn giao vật tư' : 'Yêu cầu cấp phát vật tư của tôi' }}</h2>
      <p>
        {{ isManager
          ? 'Thực hiện đúng quy trình: duyệt giữ kho, chọn lô bàn giao, người nhận xác nhận.'
          : 'Theo dõi yêu cầu và xác nhận sau khi đã nhận đủ vật tư.' }}
      </p>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Vật tư, người yêu cầu..." style="width: 260px" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
          <a-select-option value="">Tất cả</a-select-option>
          <a-select-option :value="STATUS.CONSUMABLE_PENDING">Chờ duyệt cấp phát</a-select-option>
          <a-select-option :value="STATUS.CONSUMABLE_APPROVED">Chờ bàn giao</a-select-option>
          <a-select-option :value="STATUS.CONSUMABLE_HANDED_OVER">Chờ xác nhận nhận</a-select-option>
          <a-select-option :value="STATUS.CONSUMABLE_RECEIVED">Đã nhận</a-select-option>
          <a-select-option :value="STATUS.REJECTED">Từ chối</a-select-option>
        </a-select>
      </div>
    </div>

    <a-card :bordered="false" class="request-card">
      <a-table
        class="desktop-table"
        :dataSource="dataSource"
        :columns="columns"
        :loading="loading"
        rowKey="id"
        bordered
        :scroll="{ x: 1450 }"
        :pagination="tablePagination"
        @change="handleTableChange"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="consumable" />
          </template>
          <template v-else-if="column.key === 'requestDate'">
            {{ formatDateTime(record.requestDate) }}
          </template>
          <template v-else-if="column.key === 'action'">
            <div class="action-cell">
              <template v-if="isManager && statusMatches(record.status, STATUS.CONSUMABLE_PENDING)">
                <a-button type="primary" size="small" @click="handleApprove(record.id)">Duyệt</a-button>
                <a-button danger size="small" @click="handleReject(record.id)">Từ chối</a-button>
              </template>

              <template v-else-if="isManager && statusMatches(record.status, STATUS.CONSUMABLE_APPROVED)">
                <a-button type="primary" size="small" @click="openHandover(record)">Bàn giao</a-button>
                <a-button danger size="small" @click="handleReject(record.id)">Từ chối</a-button>
              </template>

              <a-button
                v-else-if="!isManager && statusMatches(record.status, STATUS.CONSUMABLE_HANDED_OVER)"
                type="primary"
                size="small"
                @click="openReceiptConfirmation(record)"
              >
                Xem & xác nhận
              </a-button>

              <a-tooltip v-else title="Xem chi tiết">
                <a-button type="text" class="view-action-button" aria-label="Xem chi tiết yêu cầu" @click="showDetails(record)">
                  <template #icon><EyeOutlined /></template>
                </a-button>
              </a-tooltip>
            </div>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="dataSource" :loading="loading" :pagination="tablePagination" empty-description="Chưa có yêu cầu cấp phát" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-request-heading">
            <div><strong>{{ item.consumableName }}</strong><span>{{ item.username }} · {{ item.categoryName || 'Chưa phân loại' }}</span></div>
            <StatusBadge :status="item.status" type="consumable" />
          </div>
          <dl class="mobile-request-details">
            <div><dt>Số lượng</dt><dd>{{ item.quantity }}</dd></div>
            <div><dt>Ngày gửi</dt><dd>{{ formatDateTime(item.requestDate) }}</dd></div>
            <div><dt>Mục đích</dt><dd>{{ item.reason || '—' }}</dd></div>
          </dl>
          <div class="mobile-request-actions">
            <template v-if="isManager && statusMatches(item.status, STATUS.CONSUMABLE_PENDING)">
              <a-button type="primary" @click="handleApprove(item.id)">Duyệt</a-button>
              <a-button danger @click="handleReject(item.id)">Từ chối</a-button>
            </template>
            <template v-else-if="isManager && statusMatches(item.status, STATUS.CONSUMABLE_APPROVED)">
              <a-button type="primary" @click="openHandover(item)">Bàn giao</a-button>
              <a-button danger @click="handleReject(item.id)">Từ chối</a-button>
            </template>
            <a-button v-else-if="!isManager && statusMatches(item.status, STATUS.CONSUMABLE_HANDED_OVER)" type="primary" block @click="openReceiptConfirmation(item)">Xem & xác nhận</a-button>
            <a-button v-else block @click="showDetails(item)"><EyeOutlined /> Xem chi tiết</a-button>
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết yêu cầu cấp phát" :footer="null" width="680px">
      <a-descriptions v-if="selectedRequest" bordered :column="1" size="small">
        <a-descriptions-item label="Tên vật tư">{{ selectedRequest.consumableName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Danh mục">{{ selectedRequest.categoryName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Người yêu cầu">{{ selectedRequest.username || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Số lượng">{{ selectedRequest.quantity }}</a-descriptions-item>
        <a-descriptions-item label="Mục đích">{{ selectedRequest.reason || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái">
          <StatusBadge :status="selectedRequest.status" type="consumable" />
        </a-descriptions-item>
        <a-descriptions-item label="Ngày gửi">{{ formatDateTime(selectedRequest.requestDate) }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.approvalDate" label="Ngày duyệt">
          {{ formatDateTime(selectedRequest.approvalDate) }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.handedOverAt" label="Ngày bàn giao">
          {{ formatDateTime(selectedRequest.handedOverAt) }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.receivedAt" label="Ngày xác nhận nhận">
          {{ formatDateTime(selectedRequest.receivedAt) }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.allocations?.length" label="Các lô đã giao">
          <div v-for="allocation in selectedRequest.allocations" :key="allocation.consumableLotId" class="allocation-line">
            <strong>{{ allocation.lotNumber }}</strong>: {{ allocation.quantity }}
            <span v-if="allocation.expiryDate"> · HSD {{ formatDate(allocation.expiryDate) }}</span>
          </div>
        </a-descriptions-item>
      </a-descriptions>
    </a-modal>

    <a-modal
      v-model:open="handoverVisible"
      title="Chọn lô để bàn giao"
      width="780px"
      okText="Xác nhận bàn giao"
      cancelText="Hủy"
      :confirmLoading="handoverSubmitting"
      :okButtonProps="{ disabled: !canSubmitHandover }"
      @ok="submitHandover"
    >
      <a-alert
        type="info"
        show-icon
        class="handover-alert"
        :message="`Cần giao đúng ${handoverRequest?.quantity || 0} đơn vị. Hệ thống ưu tiên lô gần hết hạn trước.`"
      />
      <a-table
        :dataSource="availableLots"
        :loading="lotsLoading"
        :pagination="false"
        rowKey="id"
        size="small"
        bordered
        :scroll="{ x: 680 }"
      >
        <a-table-column title="Số lô" dataIndex="lotNumber" key="lotNumber" width="160" />
        <a-table-column title="Còn lại" dataIndex="quantity" key="quantity" align="center" width="90" />
        <a-table-column title="Hạn sử dụng" key="expiryDate" width="130">
          <template #default="{ record }">{{ record.expiryDate ? formatDate(record.expiryDate) : 'Không áp dụng' }}</template>
        </a-table-column>
        <a-table-column title="Vị trí" dataIndex="storageLocation" key="storageLocation" width="160" />
        <a-table-column title="Số lượng giao" key="allocation" align="center" width="140">
          <template #default="{ record }">
            <a-input-number
              v-model:value="lotQuantities[record.id]"
              :min="0"
              :max="record.quantity"
              :precision="0"
              style="width: 100px"
            />
          </template>
        </a-table-column>
      </a-table>
      <div class="allocation-summary" :class="{ invalid: allocationTotal !== (handoverRequest?.quantity || 0) }">
        Đã chọn: <strong>{{ allocationTotal }} / {{ handoverRequest?.quantity || 0 }}</strong>
      </div>
    </a-modal>

    <a-modal
      v-model:open="receiptVisible"
      title="Xác nhận đã nhận vật tư"
      okText="Tôi đã nhận đủ"
      cancelText="Đóng"
      :confirmLoading="receiptSubmitting"
      @ok="confirmReceipt"
    >
      <a-alert
        type="warning"
        show-icon
        message="Chỉ xác nhận sau khi bạn đã kiểm đếm đủ số lượng thực tế."
        class="handover-alert"
      />
      <a-descriptions v-if="receiptRequest" bordered :column="1" size="small">
        <a-descriptions-item label="Vật tư">{{ receiptRequest.consumableName }}</a-descriptions-item>
        <a-descriptions-item label="Số lượng">{{ receiptRequest.quantity }}</a-descriptions-item>
        <a-descriptions-item label="Các lô">
          <div v-for="allocation in receiptRequest.allocations || []" :key="allocation.consumableLotId">
            {{ allocation.lotNumber }}: <strong>{{ allocation.quantity }}</strong>
          </div>
        </a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { formatVietnamDate as formatDate, formatVietnamDateTime as formatDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})
const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))

const dataSource = ref([])
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const detailsVisible = ref(false)
const selectedRequest = ref(null)
const handoverVisible = ref(false)
const handoverRequest = ref(null)
const availableLots = ref([])
const lotQuantities = ref({})
const lotsLoading = ref(false)
const handoverSubmitting = ref(false)
const receiptVisible = ref(false)
const receiptRequest = ref(null)
const receiptSubmitting = ref(false)

const columns = [
  { title: 'Tên vật tư', dataIndex: 'consumableName', key: 'consumableName', width: 220 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 140 },
  { title: 'Người yêu cầu', dataIndex: 'username', key: 'username', width: 150 },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', width: 90, align: 'center' },
  { title: 'Mục đích', dataIndex: 'reason', key: 'reason', width: 280 },
  { title: 'Trạng thái', key: 'status', width: 180, align: 'center' },
  { title: 'Ngày gửi', dataIndex: 'requestDate', key: 'requestDate', width: 170 },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 220, align: 'center' }
]

const allocationTotal = computed(() => Object.values(lotQuantities.value)
  .reduce((sum, value) => sum + (Number(value) || 0), 0))
const canSubmitHandover = computed(() => Boolean(handoverRequest.value)
  && availableLots.value.length > 0
  && allocationTotal.value === handoverRequest.value.quantity)


const showDetails = record => {
  selectedRequest.value = record
  detailsVisible.value = true
}

const fetchData = async () => {
  loading.value = true
  try {
    const response = await consumableRequestApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    dataSource.value = response.items || []
    tablePagination.total = response.total || 0
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách yêu cầu.'))
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchData()
}

const handleTableChange = (pager) => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchData()
}

const handleApprove = async id => {
  try {
    await consumableRequestApi.approve(id)
    message.success('Đã duyệt và giữ số lượng trong kho. Tiếp theo hãy bàn giao theo lô.')
    await fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể duyệt yêu cầu.'))
  }
}

const handleReject = id => {
  Modal.confirm({
    title: 'Từ chối yêu cầu',
    content: 'Nếu yêu cầu đã được duyệt, số lượng đang giữ sẽ được trả lại kho khả dụng.',
    okText: 'Từ chối',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await consumableRequestApi.reject(id)
        message.success('Đã từ chối yêu cầu.')
        await fetchData()
      } catch (error) {
        message.error(getApiErrorMessage(error, 'Không thể từ chối yêu cầu.'))
      }
    }
  })
}

const openHandover = async record => {
  handoverRequest.value = record
  availableLots.value = []
  lotQuantities.value = {}
  handoverVisible.value = true
  lotsLoading.value = true
  try {
    const result = await consumableRequestApi.getAvailableLots(record.id)
    availableLots.value = result?.lots || []
    let remaining = record.quantity
    const suggested = {}
    for (const lot of availableLots.value) {
      const quantity = Math.min(remaining, lot.quantity)
      suggested[lot.id] = quantity
      remaining -= quantity
    }
    lotQuantities.value = suggested
    if (remaining > 0) message.warning('Các lô hợp lệ hiện không đủ để bàn giao yêu cầu này.')
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách lô.'))
  } finally {
    lotsLoading.value = false
  }
}

const submitHandover = async () => {
  if (!canSubmitHandover.value) return
  handoverSubmitting.value = true
  try {
    const allocations = Object.entries(lotQuantities.value)
      .filter(([, quantity]) => Number(quantity) > 0)
      .map(([lotId, quantity]) => ({ lotId: Number(lotId), quantity: Number(quantity) }))
    await consumableRequestApi.handover(handoverRequest.value.id, { allocations })
    message.success('Đã bàn giao. Đang chờ người nhận xác nhận.')
    handoverVisible.value = false
    await fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể bàn giao vật tư.'))
  } finally {
    handoverSubmitting.value = false
  }
}

const openReceiptConfirmation = record => {
  receiptRequest.value = record
  receiptVisible.value = true
}

const confirmReceipt = async () => {
  if (!receiptRequest.value) return
  receiptSubmitting.value = true
  try {
    await consumableRequestApi.confirmReceipt(receiptRequest.value.id)
    message.success('Đã xác nhận nhận đủ vật tư.')
    receiptVisible.value = false
    await fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể xác nhận nhận vật tư.'))
  } finally {
    receiptSubmitting.value = false
  }
}

onMounted(fetchData)
</script>

<style scoped>
.asset-requests-container { padding: 0; }
.request-card { border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05); }
.action-cell { display: flex; align-items: center; justify-content: center; gap: 8px; flex-wrap: nowrap; white-space: nowrap; }
.view-action-button { color: var(--color-primary, #e27755); }
.view-action-button:hover { background: rgba(226, 119, 85, 0.1); }
.waiting-text { color: #94a3b8; font-size: 13px; }
.allocation-line + .allocation-line { margin-top: 4px; }
.handover-alert { margin-bottom: 16px; }
.allocation-summary { margin-top: 14px; text-align: right; color: #15803d; }
.allocation-summary.invalid { color: #dc2626; }
.toolbar h2 { margin: 0 0 8px; font-weight: 600; color: #1f1f1f; }
.toolbar p { color: #6b7280; }
.toolbar-filters { display: flex; flex-wrap: wrap; gap: 10px; margin: 14px 0 18px; }
.mobile-request-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-request-heading > div { display: grid; gap: 4px; }
.mobile-request-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-request-heading span { color: var(--color-text-secondary); font-size: 12px; }
.mobile-request-details { display: grid; gap: 7px; margin: 12px 0; }
.mobile-request-details div { display: flex; justify-content: space-between; gap: 14px; }
.mobile-request-details dt { color: var(--color-text-secondary); }
.mobile-request-details dd { margin: 0; max-width: 62%; text-align: right; }
.mobile-request-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.mobile-request-actions :deep(.ant-btn) { flex: 1; }
@media (max-width: 767px) {
  .desktop-table { display: none; }
  .toolbar h2 { font-size: 22px; }
  .request-card :deep(.ant-card-body) { padding: 12px; }
  .toolbar-filters > * { width: 100% !important; }
}
</style>
