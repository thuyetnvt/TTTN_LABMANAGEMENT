<template>
  <div class="borrow-history-container">
    <div class="toolbar">
      <h2>Lịch sử mượn/trả</h2>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Người mượn, thiết bị..." class="filter-search" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
          <a-select-option value="">Tất cả</a-select-option>
          <a-select-option :value="STATUS.BORROW_PENDING">Chờ quản lý duyệt</a-select-option>
          <a-select-option :value="STATUS.TEACHER_PENDING">Chờ giảng viên duyệt</a-select-option>
          <a-select-option :value="STATUS.APPROVED">Chờ nhận</a-select-option>
          <a-select-option :value="STATUS.BORROWED">Đang mượn</a-select-option>
          <a-select-option :value="STATUS.RETURN_PROCESSING">Đang kiểm tra trả</a-select-option>
          <a-select-option :value="STATUS.RETURNED">Đã trả</a-select-option>
          <a-select-option :value="STATUS.RETURNED_DAMAGED">Đã trả, có hư hỏng</a-select-option>
          <a-select-option :value="STATUS.REJECTED">Từ chối</a-select-option>
          <a-select-option :value="STATUS.CANCELLED">Đã hủy</a-select-option>
          <a-select-option :value="STATUS.EXPIRED">Hết hạn giữ chỗ</a-select-option>
          <a-select-option value="OVERDUE">Quá hạn</a-select-option>
        </a-select>
      </div>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1640 }" :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'expectedReturnDate' || column.key === 'actualReturnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'returnCondition'">
            <StatusBadge v-if="record.returnCondition" :status="record.returnCondition" type="returnCondition" />
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" :color="record.isOverdue ? 'red' : ''" :label-override="borrowWorkflowLabel(record)" />
          </template>
          <template v-else-if="column.key === 'compensationAmount'">
            {{ record.compensationAmount ? record.compensationAmount.toLocaleString('vi-VN') + ' VNĐ' : '' }}
          </template>
          <template v-else-if="column.key === 'action'">
            <a-button
              v-if="record.canCancel"
              danger
              size="small"
              @click="openCancelModal(record)"
            >
              Hủy phiếu
            </a-button>
            <a-button
              v-else-if="record.canConfirmHandover"
              type="primary"
              size="small"
              @click="openHandover(record)"
            >
              Xem & xác nhận nhận
            </a-button>
            <a-tooltip v-else title="Xem chi tiết phiếu">
              <a-button type="text" class="view-action" aria-label="Xem chi tiết phiếu mượn" @click="openDetails(record)">
                <template #icon><EyeOutlined /></template>
              </a-button>
            </a-tooltip>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="dataSource" :loading="loading" :pagination="tablePagination" empty-description="Chưa có lịch sử mượn/trả" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-card-heading">
            <strong>{{ item.device }}</strong>
            <StatusBadge :status="item.status" type="borrow" :color="item.isOverdue ? 'red' : ''" :label-override="borrowWorkflowLabel(item)" />
          </div>
          <div class="mobile-card-subtitle">{{ borrowerLabel(item) }} · {{ item.serial || 'Không có số seri' }}</div>
          <dl class="mobile-card-details">
            <div><dt>Ngày đăng ký</dt><dd>{{ formatDate(item.requestDate) }}</dd></div>
            <div><dt>Hạn trả</dt><dd>{{ formatDate(item.expectedReturnDate) }}</dd></div>
            <div><dt>Ngày trả thực tế</dt><dd>{{ item.actualReturnDate ? formatDate(item.actualReturnDate) : '—' }}</dd></div>
            <div v-if="item.returnCondition"><dt>Tình trạng trả</dt><dd><StatusBadge :status="item.returnCondition" type="returnCondition" /></dd></div>
            <div v-if="item.compensationAmount"><dt>Bồi thường</dt><dd>{{ item.compensationAmount.toLocaleString('vi-VN') }} VNĐ</dd></div>
          </dl>
          <a-button v-if="item.canCancel" danger block @click="openCancelModal(item)">Hủy phiếu</a-button>
          <a-button v-else-if="item.canConfirmHandover" type="primary" block @click="openHandover(item)">Xem & xác nhận nhận</a-button>
          <a-button v-else block @click="openDetails(item)"><EyeOutlined /> Xem chi tiết</a-button>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal
      v-model:open="isHandoverVisible"
      title="Kiểm tra biên bản bàn giao"
      width="720px"
    >
      <a-spin :spinning="handoverLoading">
        <a-alert v-if="selectedRecord?.canConfirmHandover"
          type="warning"
          show-icon
          message="Chỉ xác nhận sau khi đã nhận và kiểm tra thực tế"
          description="Khi xác nhận, phiếu sẽ chuyển sang Đang mượn và tài sản được ghi nhận đang do bạn quản lý."
          style="margin-bottom: 16px"
        />
        <a-descriptions v-if="selectedHandover" bordered size="small" :column="1">
          <a-descriptions-item label="Mã biên bản">{{ selectedHandover.code }}</a-descriptions-item>
          <a-descriptions-item label="Thời gian lập">{{ formatDateTime(selectedHandover.handoverAt) }}</a-descriptions-item>
          <a-descriptions-item label="Ghi chú">{{ selectedHandover.notes || 'Không có' }}</a-descriptions-item>
        </a-descriptions>
        <div v-if="selectedHandover?.items?.length" class="handover-items">
          <a-card v-for="item in selectedHandover.items" :key="item.equipmentId" size="small">
            <strong>{{ item.equipmentName }}</strong>
            <div>Số seri: {{ item.serial || '—' }}</div>
            <div>Tình trạng: <StatusBadge :status="item.condition" type="returnCondition" /></div>
            <div>Phụ kiện: {{ item.accessories || 'Không ghi nhận' }}</div>
            <div>Ghi chú: {{ item.note || 'Không có' }}</div>
          </a-card>
        </div>
      </a-spin>
      <template #footer>
        <a-button @click="isHandoverVisible = false">Đóng</a-button>
        <a-button v-if="selectedRecord?.canConfirmHandover" type="primary" :loading="confirming" @click="confirmReceipt">
          Xác nhận đã nhận đủ
        </a-button>
      </template>
    </a-modal>

    <a-modal v-model:open="isDetailsVisible" title="Chi tiết phiếu mượn/trả" :footer="null" width="760px">
      <a-descriptions v-if="selectedRecord" bordered size="small" :column="1">
        <a-descriptions-item label="Người mượn">{{ borrowerLabel(selectedRecord) }}</a-descriptions-item>
        <a-descriptions-item label="Thiết bị">{{ selectedRecord.device }}</a-descriptions-item>
        <a-descriptions-item label="Hạn trả">{{ formatDate(selectedRecord.expectedReturnDate) }}</a-descriptions-item>
        <a-descriptions-item label="Ngày trả thực tế">{{ selectedRecord.actualReturnDate ? formatDate(selectedRecord.actualReturnDate) : 'Chưa trả' }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái"><StatusBadge :status="selectedRecord.status" type="borrow" :color="selectedRecord.isOverdue ? 'red' : ''" :label-override="borrowWorkflowLabel(selectedRecord)" /></a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.holdExpiresAt" label="Thời hạn giữ chỗ">{{ formatDateTime(selectedRecord.holdExpiresAt) }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.cancellationReason" label="Lý do hủy">{{ selectedRecord.cancellationReason }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.cancelledAt" label="Thời điểm hủy">{{ formatDateTime(selectedRecord.cancelledAt) }}</a-descriptions-item>
        <a-descriptions-item label="Ghi chú kiểm tra">{{ selectedRecord.returnInspectionNote || 'Chưa có' }}</a-descriptions-item>
        <a-descriptions-item label="Xử lý bảo hành">{{ selectedRecord.warrantyAction || 'Không có' }}</a-descriptions-item>
      </a-descriptions>
    </a-modal>

    <a-modal
      v-model:open="isCancelVisible"
      title="Hủy phiếu mượn"
      ok-text="Xác nhận hủy"
      cancel-text="Đóng"
      :confirm-loading="cancelling"
      @ok="submitCancellation"
    >
      <a-alert
        type="warning"
        show-icon
        message="Sau khi hủy, yêu cầu sẽ không thể tiếp tục xử lý."
        style="margin-bottom: 16px"
      />
      <a-form layout="vertical">
        <a-form-item label="Lý do hủy" required>
          <a-textarea v-model:value="cancelReason" :rows="4" maxlength="1000" show-count placeholder="Nhập lý do để phòng lab theo dõi..." />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { borrowApi } from '../api/borrowApi'
import { handoverApi } from '../api/handoverApi'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { STATUS, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDate as formatDate, formatVietnamDateTime as formatDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const dataSource = ref([])
const route = useRoute()
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const isHandoverVisible = ref(false)
const handoverLoading = ref(false)
const confirming = ref(false)
const selectedRecord = ref(null)
const selectedHandover = ref(null)
const isDetailsVisible = ref(false)
const isCancelVisible = ref(false)
const cancelling = ref(false)
const cancelReason = ref('')
const cancelRecord = ref(null)

const borrowerLabel = record => record?.borrowerName?.trim() || record?.student || 'Không xác định'

const columns = [
  { title: 'Người mượn', dataIndex: 'borrowerName', key: 'borrowerName', width: 170 },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', width: 160 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 130 },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', width: 120 },
  { title: 'Hạn trả', dataIndex: 'expectedReturnDate', key: 'expectedReturnDate', width: 120 },
  { title: 'Ngày trả thực tế', dataIndex: 'actualReturnDate', key: 'actualReturnDate', width: 130 },
  { title: 'Tình trạng trả', dataIndex: 'returnCondition', key: 'returnCondition', width: 130 },
  { title: 'Ghi chú kiểm tra', dataIndex: 'returnInspectionNote', key: 'returnInspectionNote', width: 200 },
  { title: 'Xử lý bảo hành', dataIndex: 'warrantyAction', key: 'warrantyAction', width: 180 },
  { title: 'Bồi thường', dataIndex: 'compensationAmount', key: 'compensationAmount', width: 130 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center', width: 140 },
  { title: 'Hành động', key: 'action', align: 'center', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 190 }
]

const borrowWorkflowLabel = record => {
  if (record?.isOverdue) {
    const daysOverdue = Math.max(1, Math.abs(Number(record.daysUntilDue || 0)))
    return `Quá hạn ${daysOverdue} ngày`
  }
  if (statusMatches(record.status, STATUS.APPROVED)) {
    return record.handover ? 'Đã bàn giao, chờ người nhận xác nhận' : 'Đã duyệt, chờ lập bàn giao'
  }
  return ''
}

const openDetails = record => {
  selectedRecord.value = record
  isDetailsVisible.value = true
}

const openCancelModal = record => {
  cancelRecord.value = record
  cancelReason.value = ''
  isCancelVisible.value = true
}

const submitCancellation = async () => {
  const reason = cancelReason.value.trim()
  if (!reason) {
    message.warning('Vui lòng nhập lý do hủy phiếu.')
    return
  }
  cancelling.value = true
  try {
    await borrowApi.cancel(cancelRecord.value.id, reason)
    message.success('Đã hủy phiếu mượn.')
    isCancelVisible.value = false
    await fetchHistory()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể hủy phiếu mượn.'))
  } finally {
    cancelling.value = false
  }
}

const syncRouteFilter = () => {
  const routeStatus = typeof route.query.status === 'string' ? route.query.status : undefined
  if (routeStatus && routeStatus !== statusFilter.value) statusFilter.value = routeStatus
}

onMounted(() => {
  syncRouteFilter()
  fetchHistory()
})

watch(() => route.query.status, () => {
  syncRouteFilter()
  applyFilters()
})


const openHandover = async record => {
  selectedRecord.value = record
  selectedHandover.value = record.handover || null
  isHandoverVisible.value = true
  handoverLoading.value = true
  try {
    selectedHandover.value = await handoverApi.getByBorrowRecord(record.id)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể tải biên bản bàn giao.'))
    isHandoverVisible.value = false
  } finally {
    handoverLoading.value = false
  }
}

const confirmReceipt = async () => {
  if (!selectedRecord.value || handoverLoading.value) return
  confirming.value = true
  try {
    await handoverApi.confirmReceipt(selectedRecord.value.id)
    message.success('Đã xác nhận nhận tài sản. Phiếu đã chuyển sang đang mượn.')
    isHandoverVisible.value = false
    await fetchHistory()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể xác nhận nhận tài sản.'))
  } finally {
    confirming.value = false
  }
}

const fetchHistory = async () => {
  loading.value = true
  try {
    const response = await borrowApi.getHistoryPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    dataSource.value = response.items || []
    tablePagination.total = response.total || 0
  } catch {
    message.error('Lỗi khi tải lịch sử!')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchHistory()
}

const handleTableChange = (pager) => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchHistory()
}
</script>

<style scoped>
.borrow-history-container {
  padding: 0;
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 24px;
}

.toolbar-filters { display: flex; flex-wrap: wrap; gap: 10px; }

@media (max-width: 767px) {
  .toolbar { align-items: stretch; flex-direction: column; }
  .toolbar-filters > * { width: 100% !important; }
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.muted { color: #8c8c8c; font-size: 13px; }
.view-action { color: var(--color-primary); }
.handover-items { display: grid; gap: 10px; margin-top: 16px; }
.handover-items :deep(.ant-card-body) { display: grid; gap: 6px; }
.mobile-card-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-card-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-card-subtitle, .mobile-card-note { margin-top: 6px; color: var(--color-text-secondary); font-size: 13px; }
.mobile-card-details { display: grid; gap: 7px; margin: 12px 0; }
.mobile-card-details div { display: flex; justify-content: space-between; gap: 12px; }
.mobile-card-details dt { color: var(--color-text-secondary); }
.mobile-card-details dd { margin: 0; text-align: right; }
@media (max-width: 767px) { .desktop-table { display: none; } }
</style>
