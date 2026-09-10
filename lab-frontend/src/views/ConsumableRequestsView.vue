<template>
  <div class="asset-requests-container">
    <div class="toolbar">
      <h2>{{ isManager ? 'Duyệt và bàn giao vật tư' : canApprove ? 'Duyệt yêu cầu vật tư được ủy quyền' : 'Yêu cầu cấp phát vật tư của tôi' }}</h2>
      <p>
        {{ isManager
          ? 'Thực hiện đúng quy trình: duyệt giữ kho, chọn lô bàn giao, người nhận xác nhận.'
          : canApprove
            ? (canHandover
              ? 'Bạn có thể duyệt, từ chối và bàn giao yêu cầu trong thời gian được ủy quyền.'
              : 'Bạn có thể duyệt hoặc từ chối yêu cầu trong thời gian được ủy quyền; việc bàn giao do người có quyền bàn giao thực hiện.')
            : 'Theo dõi yêu cầu và xác nhận sau khi đã nhận đủ vật tư.' }}
      </p>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Vật tư, người yêu cầu..." class="filter-search" @search="applyFilters" />
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
        :scroll="{ x: 'max-content' }"
        :pagination="tablePagination"
        @change="handleTableChange"
      >
        <template #headerCell="{ column }">
          <TableColumnFilter
            v-if="column.filterType || column.sortable"
            :title="column.title"
            :type="column.filterType"
            :options="column.filterOptions"
            :filterable="Boolean(column.filterType)"
            :sortable="Boolean(column.sortable)"
            :sort-order="sortState.field === column.sortKey ? sortState.order : undefined"
            :value="column.filterKey === 'status' ? statusFilter : searchQuery"
            :placeholder="column.filterPlaceholder"
            @apply="value => applyColumnFilter(column, value)"
            @sort="value => applyColumnSort(column, value)"
          />
          <span v-else>{{ column.title }}</span>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="consumable" />
          </template>
          <template v-else-if="column.key === 'requestDate'">
            {{ formatDateTime(record.requestDate) }}
          </template>
          <template v-else-if="column.key === 'action'">
            <div class="action-cell">
              <template v-if="canApprove && statusMatches(record.status, STATUS.CONSUMABLE_PENDING)">
                <a-button type="primary" size="small" @click="handleApprove(record.id)">Duyệt</a-button>
                <a-button danger size="small" @click="openRejectModal(record)">Từ chối</a-button>
              </template>

              <template v-else-if="statusMatches(record.status, STATUS.CONSUMABLE_APPROVED)">
                <a-button v-if="canHandover" type="primary" size="small" @click="openHandover(record)">Bàn giao</a-button>
                <a-button v-if="canApprove" danger size="small" @click="openRejectModal(record)">
                  {{ isApprovedRequest(record) ? 'Không thể bàn giao' : 'Từ chối' }}
                </a-button>
                <span v-if="canApprove && !canHandover" class="waiting-text">Chờ người có quyền bàn giao</span>
              </template>

              <a-button
                v-else-if="!canApprove && statusMatches(record.status, STATUS.CONSUMABLE_HANDED_OVER)"
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
            <div><strong>{{ item.consumableName }}</strong><span>{{ item.fullName || item.username || '—' }} · {{ item.categoryName || 'Chưa phân loại' }}</span></div>
            <StatusBadge :status="item.status" type="consumable" />
          </div>
          <dl class="mobile-request-details">
            <div><dt>Số lượng</dt><dd>{{ item.quantity }}</dd></div>
            <div><dt>Ngày gửi</dt><dd>{{ formatDateTime(item.requestDate) }}</dd></div>
            <div><dt>Mục đích</dt><dd>{{ item.reason || '—' }}</dd></div>
          </dl>
          <div class="mobile-request-actions">
            <template v-if="canApprove && statusMatches(item.status, STATUS.CONSUMABLE_PENDING)">
              <a-button type="primary" @click="handleApprove(item.id)">Duyệt</a-button>
              <a-button danger @click="openRejectModal(item)">Từ chối</a-button>
            </template>
            <template v-else-if="statusMatches(item.status, STATUS.CONSUMABLE_APPROVED)">
              <a-button v-if="canHandover" type="primary" @click="openHandover(item)">Bàn giao</a-button>
              <a-button v-if="canApprove" danger @click="openRejectModal(item)">
                {{ isApprovedRequest(item) ? 'Không thể bàn giao' : 'Từ chối' }}
              </a-button>
              <span v-if="canApprove && !canHandover" class="waiting-text">Chờ người có quyền bàn giao</span>
            </template>
            <a-button v-else-if="!canApprove && statusMatches(item.status, STATUS.CONSUMABLE_HANDED_OVER)" type="primary" block @click="openReceiptConfirmation(item)">Xem & xác nhận</a-button>
            <a-button v-else block @click="showDetails(item)"><EyeOutlined /> Xem chi tiết</a-button>
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết yêu cầu cấp phát" :footer="null" width="680px">
      <a-descriptions v-if="selectedRequest" bordered :column="1" size="small">
        <a-descriptions-item label="Tên vật tư">{{ selectedRequest.consumableName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Danh mục">{{ selectedRequest.categoryName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Người yêu cầu">{{ selectedRequest.fullName || selectedRequest.username || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Số lượng">{{ selectedRequest.quantity }}</a-descriptions-item>
        <a-descriptions-item label="Mục đích">{{ selectedRequest.reason || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái">
          <StatusBadge :status="selectedRequest.status" type="consumable" />
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.rejectionReason" label="Lý do xử lý">
          {{ selectedRequest.rejectionReason }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.rejectionReason" label="Người xử lý">
          {{ selectedRequest.rejectedByName || selectedRequest.rejectedByUsername || '—' }}
          <span v-if="selectedRequest.rejectionStage"> · {{ rejectionStageLabel(selectedRequest.rejectionStage) }}</span>
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedRequest.rejectedAt" label="Thời gian xử lý">
          {{ formatDateTime(selectedRequest.rejectedAt) }}
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
      v-model:open="rejectVisible"
      :title="rejectModalTitle"
      ok-text="Xác nhận"
      cancel-text="Hủy"
      :confirm-loading="rejectSubmitting"
      :ok-button-props="{ danger: true, disabled: !rejectReason.trim() }"
      @ok="submitReject"
    >
      <a-alert
        type="warning"
        show-icon
        :message="rejectModalMessage"
        class="handover-alert"
      />
      <a-form layout="vertical">
        <a-form-item label="Lý do xử lý" required>
          <a-textarea
            v-model:value="rejectReason"
            :rows="4"
            :maxlength="2000"
            show-count
            :placeholder="rejectModalPlaceholder"
          />
        </a-form-item>
      </a-form>
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
import { message } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination } from '../utils/tablePagination'
import { formatVietnamDate as formatDate, formatVietnamDateTime as formatDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination({ defaultPageSize: 10, pageSize: 10 }),
  current: 1,
  pageSize: 10,
  total: 0
})
const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))
const canApprove = computed(() => isManager.value || Boolean(authStore.approvalPermissions?.canApproveConsumable))
const canHandover = computed(() => isManager.value || Boolean(authStore.approvalPermissions?.canHandoverConsumable))

const dataSource = ref([])
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const sortState = reactive({ field: undefined, order: undefined })
const detailsVisible = ref(false)
const selectedRequest = ref(null)
const rejectVisible = ref(false)
const rejectSubmitting = ref(false)
const rejectRequest = ref(null)
const rejectReason = ref('')
const handoverVisible = ref(false)
const handoverRequest = ref(null)
const availableLots = ref([])
const lotQuantities = ref({})
const lotsLoading = ref(false)
const handoverSubmitting = ref(false)
const receiptVisible = ref(false)
const receiptRequest = ref(null)
const receiptSubmitting = ref(false)
const consumableRequestStatusOptions = [
  { value: STATUS.CONSUMABLE_PENDING, label: 'Chờ duyệt cấp phát' },
  { value: STATUS.CONSUMABLE_APPROVED, label: 'Chờ bàn giao' },
  { value: STATUS.CONSUMABLE_HANDED_OVER, label: 'Chờ xác nhận nhận' },
  { value: STATUS.CONSUMABLE_RECEIVED, label: 'Đã nhận' },
  { value: STATUS.REJECTED, label: 'Từ chối' }
]

const columns = [
  { title: 'Tên vật tư', dataIndex: 'consumableName', key: 'consumableName', sortKey: 'consumable', sortable: true, width: 220, fixed: 'left', filterType: 'search', filterPlaceholder: 'Tìm tên vật tư...' },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', sortKey: 'category', sortable: true, width: 165, filterType: 'search', filterPlaceholder: 'Tìm danh mục...' },
  { title: 'Người yêu cầu', dataIndex: 'fullName', key: 'fullName', sortKey: 'requester', sortable: true, width: 195, filterType: 'search', filterPlaceholder: 'Tìm người yêu cầu...' },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', sortKey: 'quantity', sortable: true, width: 125, align: 'center' },
  { title: 'Mục đích', dataIndex: 'reason', key: 'reason', sortKey: 'reason', sortable: true, width: 280, filterType: 'search', filterPlaceholder: 'Tìm mục đích...' },
  { title: 'Trạng thái', key: 'status', sortKey: 'status', sortable: true, width: 255, align: 'center', className: 'status-column', filterType: 'select', filterKey: 'status', filterOptions: consumableRequestStatusOptions },
  { title: 'Ngày gửi', dataIndex: 'requestDate', key: 'requestDate', sortKey: 'requestDate', sortable: true, width: 170 },
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
      status: statusFilter.value,
      sortBy: sortState.field,
      sortDirection: sortState.order === 'descend' ? 'desc' : (sortState.order === 'ascend' ? 'asc' : undefined)
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

const applyColumnFilter = (column, value) => {
  if (column.filterKey === 'status') statusFilter.value = value
  else searchQuery.value = value || ''
  applyFilters()
}

const isApprovedRequest = record => statusMatches(record?.status, STATUS.CONSUMABLE_APPROVED)
const rejectionStageLabel = stage => stage === 'HANDOVER' ? 'khâu bàn giao' : 'khâu duyệt'
const rejectModalTitle = computed(() => isApprovedRequest(rejectRequest.value) ? 'Không thể bàn giao vật tư' : 'Từ chối yêu cầu')
const rejectModalMessage = computed(() => isApprovedRequest(rejectRequest.value)
  ? 'Yêu cầu đã được duyệt nhưng chưa thể bàn giao. Vui lòng ghi rõ nguyên nhân thực tế.'
  : 'Vui lòng ghi rõ lý do để người yêu cầu biết và có thể xử lý lại.')
const rejectModalPlaceholder = computed(() => isApprovedRequest(rejectRequest.value)
  ? 'Ví dụ: Kho thực tế không đủ số lượng đã duyệt...'
  : 'Ví dụ: Vật tư vượt định mức hoặc mục đích chưa phù hợp...')

const openRejectModal = record => {
  rejectRequest.value = record
  rejectReason.value = ''
  rejectVisible.value = true
}

const applyColumnSort = (column, order) => {
  sortState.field = order ? column.sortKey : undefined
  sortState.order = order
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

const submitReject = async () => {
  const reason = rejectReason.value.trim()
  if (!rejectRequest.value || !reason) {
    message.warning('Vui lòng nhập lý do xử lý.')
    return
  }

  rejectSubmitting.value = true
  try {
    const result = await consumableRequestApi.reject(rejectRequest.value.id, { reason })
    message.success(result?.message || 'Đã lưu kết quả xử lý.')
    rejectVisible.value = false
    rejectRequest.value = null
    rejectReason.value = ''
    await fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể lưu kết quả xử lý.'))
  } finally {
    rejectSubmitting.value = false
  }
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

onMounted(async () => {
  await authStore.loadApprovalPermissions().catch(() => {})
  fetchData()
})
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
