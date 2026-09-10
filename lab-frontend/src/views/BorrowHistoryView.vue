<template>
  <div class="borrow-history-container">
    <div class="toolbar">
      <h2>Lịch sử mượn/trả</h2>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Người mượn, thiết bị..." class="filter-search" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
          <a-select-option value="">Tất cả</a-select-option>
          <a-select-option :value="BORROW_HISTORY_FILTERS.PENDING">Phiếu đang xử lý</a-select-option>
          <a-select-option :value="STATUS.BORROW_PENDING">Chờ quản lý duyệt</a-select-option>
          <a-select-option :value="STATUS.TEACHER_PENDING">Chờ giảng viên duyệt</a-select-option>
          <a-select-option :value="STATUS.APPROVED">Chờ nhận</a-select-option>
          <a-select-option :value="BORROW_HISTORY_FILTERS.ACTIVE">Đang mượn (tất cả)</a-select-option>
          <a-select-option :value="STATUS.BORROWED">Đang mượn</a-select-option>
          <a-select-option :value="STATUS.RETURN_PROCESSING">Đang kiểm tra trả</a-select-option>
          <a-select-option :value="BORROW_HISTORY_FILTERS.COMPLETED">Đã hoàn tất (tất cả)</a-select-option>
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
      <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination" @change="handleTableChange">
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
          <template v-if="column.key === 'requestDate' || column.key === 'expectedReturnDate' || column.key === 'actualReturnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'borrowerPhone'">
            <a
              v-if="record.borrowerPhone"
              class="borrower-phone-link"
              :href="phoneHref(record.borrowerPhone)"
              :title="`Gọi ${borrowerLabel(record)}`"
            >
              {{ record.borrowerPhone }}
            </a>
            <span v-else class="muted">Chưa cập nhật</span>
          </template>
          <template v-else-if="column.key === 'returnCondition'">
            <StatusBadge v-if="record.returnCondition" :status="record.returnCondition" type="returnCondition" />
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" :color="record.isOverdue ? 'red' : ''" :label-override="borrowWorkflowLabel(record)" />
          </template>
          <template v-else-if="column.key === 'action'">
            <template v-if="isManager && (statusMatches(record.status, STATUS.BORROWED) || statusMatches(record.status, STATUS.RETURN_PROCESSING))">
              <div class="request-actions">
                <a-button type="default" size="small" @click="openReturn(record)">Kiểm tra trả</a-button>
                <a-button size="small" :loading="isReminding(record.id)" @click="handleRemind(record)">Nhắc trả</a-button>
              </div>
            </template>
            <a-button
              v-else-if="record.canCancel"
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
            <div><dt>Số điện thoại liên hệ</dt><dd><a v-if="item.borrowerPhone" :href="phoneHref(item.borrowerPhone)">{{ item.borrowerPhone }}</a><span v-else>Chưa cập nhật</span></dd></div>
            <div><dt>Ngày đăng ký</dt><dd>{{ formatDate(item.requestDate) }}</dd></div>
            <div><dt>Hạn trả</dt><dd>{{ formatDate(item.expectedReturnDate) }}</dd></div>
            <div><dt>Ngày trả thực tế</dt><dd>{{ item.actualReturnDate ? formatDate(item.actualReturnDate) : '—' }}</dd></div>
            <div v-if="item.returnCondition"><dt>Tình trạng trả</dt><dd><StatusBadge :status="item.returnCondition" type="returnCondition" /></dd></div>
          </dl>
          <div v-if="isManager && (statusMatches(item.status, STATUS.BORROWED) || statusMatches(item.status, STATUS.RETURN_PROCESSING))" class="mobile-request-actions">
            <a-button @click="openReturn(item)">Kiểm tra trả</a-button>
            <a-button :loading="isReminding(item.id)" @click="handleRemind(item)">Nhắc trả</a-button>
          </div>
          <a-button v-else-if="item.canCancel" danger block @click="openCancelModal(item)">Hủy phiếu</a-button>
          <a-button v-else-if="item.canConfirmHandover" type="primary" block @click="openHandover(item)">Xem & xác nhận nhận</a-button>
          <a-button v-else block @click="openDetails(item)"><EyeOutlined /> Xem chi tiết</a-button>
        </template>
      </ResponsiveDataList>
    </a-card>

    <ReturnInspectionModal
      :open="isReturnVisible"
      :record="returnRecord"
      @update:open="isReturnVisible = $event"
      @saved="fetchHistory"
    />

    <a-modal
      v-model:open="isHandoverVisible"
      title="Kiểm tra biên bản bàn giao"
      width="720px"
    >
      <a-spin :spinning="handoverLoading">
        <a-alert v-if="selectedHandover?.canConfirm"
          type="warning"
          show-icon
          message="Chỉ xác nhận sau khi đã nhận và kiểm tra thực tế"
          description="Khi xác nhận, phiếu sẽ chuyển sang Đang mượn và tài sản được ghi nhận đang do bạn quản lý."
          style="margin-bottom: 16px"
        />
        <a-alert v-if="selectedHandover?.hasPendingIssueReports"
          type="error"
          show-icon
          message="Có báo cáo sai lệch đang chờ quản lý xử lý"
          description="Chưa thể xác nhận nhận tài sản. Vui lòng phối hợp kiểm tra trực tiếp tại Lab."
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
            <div v-if="issueReportFor(item.equipmentId)?.evidence?.length" class="handover-issue-evidence-count">
              Đã đính kèm {{ issueReportFor(item.equipmentId).evidence.length }} ảnh bằng chứng
            </div>
            <div v-if="selectedHandover?.canReportIssue" class="handover-item-actions">
              <a-button
                v-if="issueReportFor(item.equipmentId)?.status !== 'HANDOVER_ISSUE_PENDING'"
                size="small"
                danger
                @click="openIssueReport(item)"
              >
                Báo sai lệch
              </a-button>
              <a-tag v-else color="orange">Đã báo, chờ xử lý</a-tag>
            </div>
            <div v-else-if="issueReportFor(item.equipmentId)" class="handover-issue-summary">
              <a-tag :color="issueReportFor(item.equipmentId).status === 'HANDOVER_ISSUE_REJECTED' ? 'red' : 'green'">
                {{ issueReportFor(item.equipmentId).status === 'HANDOVER_ISSUE_REJECTED' ? 'Báo cáo bị từ chối' : 'Đã xử lý sai lệch' }}
              </a-tag>
              <span>{{ issueReportFor(item.equipmentId).resolutionNote || 'Đã có kết quả xử lý.' }}</span>
            </div>
          </a-card>
        </div>
      </a-spin>
      <template #footer>
        <a-button @click="isHandoverVisible = false">Đóng</a-button>
        <a-button v-if="selectedHandover?.canConfirm" type="primary" :loading="confirming" @click="confirmReceipt">
          Xác nhận đã nhận đủ
        </a-button>
      </template>
    </a-modal>

    <a-modal
      v-model:open="issueReportVisible"
      title="Báo cáo sai lệch bàn giao"
      ok-text="Gửi báo cáo"
      cancel-text="Hủy"
      :confirm-loading="issueSubmitting"
      @cancel="closeIssueReport"
      @ok="submitIssueReport"
    >
      <a-alert
        v-if="issueReportItem"
        type="warning"
        show-icon
        :message="`Thiết bị: ${issueReportItem.equipmentName}`"
        :description="`Serial: ${issueReportItem.serial || 'Không có serial'}`"
        style="margin-bottom: 16px"
      />
      <a-form layout="vertical">
        <a-form-item label="Loại sai lệch" required>
          <a-select v-model:value="issueForm.issueType" placeholder="Chọn loại sai lệch">
            <a-select-option v-for="option in issueTypeOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Mô tả thực tế" required>
          <a-textarea
            v-model:value="issueForm.description"
            :rows="5"
            maxlength="2000"
            show-count
            placeholder="Mô tả rõ điểm khác với biên bản: thiếu phụ kiện, trầy xước, không hoạt động..."
          />
        </a-form-item>
        <a-form-item label="Ảnh bằng chứng (không bắt buộc)">
          <a-upload
            :multiple="true"
            :before-upload="selectIssueEvidence"
            :show-upload-list="false"
            accept=".jpg,.jpeg,.png,.webp"
            :disabled="issueSubmitting || issueEvidenceFiles.length >= 5"
          >
            <a-button>Chọn ảnh</a-button>
          </a-upload>
          <div class="issue-upload-hint">JPG, PNG, WEBP · tối đa 5 ảnh · mỗi ảnh không quá 10 MB</div>
          <div v-if="issueEvidenceFiles.length" class="issue-evidence-list">
            <div v-for="(file, index) in issueEvidenceFiles" :key="`${file.name}-${file.lastModified}-${index}`" class="issue-evidence-file">
              <a-image :src="issueEvidencePreviewUrls[index]" :width="56" :height="56" :preview="true" />
              <div class="issue-evidence-file-copy">
                <strong>{{ file.name }}</strong>
                <span>{{ formatFileSize(file.size) }}</span>
              </div>
              <a-button type="text" danger :disabled="issueSubmitting" @click="removeIssueEvidence(index)">Xóa</a-button>
            </div>
          </div>
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal v-model:open="isDetailsVisible" title="Chi tiết phiếu mượn/trả" :footer="null" width="760px">
      <a-descriptions v-if="selectedRecord" bordered size="small" :column="1">
        <a-descriptions-item label="Người mượn">{{ borrowerLabel(selectedRecord) }}</a-descriptions-item>
        <a-descriptions-item label="Số điện thoại liên hệ">
          <a v-if="selectedRecord.borrowerPhone" :href="phoneHref(selectedRecord.borrowerPhone)">{{ selectedRecord.borrowerPhone }}</a>
          <span v-else>Chưa cập nhật</span>
        </a-descriptions-item>
        <a-descriptions-item label="Thiết bị">{{ selectedRecord.device }}</a-descriptions-item>
        <a-descriptions-item label="Hạn trả">{{ formatDate(selectedRecord.expectedReturnDate) }}</a-descriptions-item>
        <a-descriptions-item label="Ngày trả thực tế">{{ selectedRecord.actualReturnDate ? formatDate(selectedRecord.actualReturnDate) : 'Chưa trả' }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái"><StatusBadge :status="selectedRecord.status" type="borrow" :color="selectedRecord.isOverdue ? 'red' : ''" :label-override="borrowWorkflowLabel(selectedRecord)" /></a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.managerDecisionNote" label="Lý do quản lý từ chối">{{ selectedRecord.managerDecisionNote }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.teacherDecisionNote" label="Lý do giảng viên từ chối bảo lãnh">{{ selectedRecord.teacherDecisionNote }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.holdExpiresAt" label="Thời hạn giữ chỗ">{{ formatDateTime(selectedRecord.holdExpiresAt) }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.cancellationReason" label="Lý do hủy">{{ selectedRecord.cancellationReason }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedRecord.cancelledAt" label="Thời điểm hủy">{{ formatDateTime(selectedRecord.cancelledAt) }}</a-descriptions-item>
        <a-descriptions-item label="Ghi chú kiểm tra">{{ selectedRecord.returnInspectionNote || 'Chưa có' }}</a-descriptions-item>
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
import { computed, reactive, ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { message, Upload } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { borrowApi } from '../api/borrowApi'
import { handoverApi } from '../api/handoverApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import ReturnInspectionModal from '../components/ReturnInspectionModal.vue'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { BORROW_HISTORY_FILTERS, STATUS, isManagerRole, statusMatches } from '../constants/business'
import { getApiErrorMessage, getApiSuccessMessage } from '../utils/apiError'
import { formatVietnamDate as formatDate, formatVietnamDateTime as formatDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const dataSource = ref([])
const route = useRoute()
const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const sortState = reactive({ field: undefined, order: undefined })
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
const isReturnVisible = ref(false)
const returnRecord = ref(null)
const remindingRecordIds = ref(new Set())
const issueReportVisible = ref(false)
const issueSubmitting = ref(false)
const issueReportItem = ref(null)
const issueForm = reactive({ issueType: 'CONDITION', description: '' })
const issueEvidenceFiles = ref([])
const issueEvidencePreviewUrls = ref([])

const issueTypeOptions = [
  { value: 'CONDITION', label: 'Tình trạng khác mô tả' },
  { value: 'ACCESSORIES', label: 'Thiếu hoặc sai phụ kiện' },
  { value: 'WRONG_ASSET', label: 'Sai thiết bị, mã tài sản hoặc serial' },
  { value: 'NOT_WORKING', label: 'Thiết bị không hoạt động' },
  { value: 'OTHER', label: 'Khác' }
]

const borrowerLabel = record => record?.borrowerName?.trim() || record?.student || 'Không xác định'
const phoneHref = phone => {
  const normalized = String(phone || '').replace(/[^\d+]/g, '')
  return normalized ? `tel:${normalized}` : '#'
}
const formatFileSize = bytes => {
  if (!bytes) return '0 KB'
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
const isReminding = id => remindingRecordIds.value.has(id)

const borrowStatusOptions = [
  { value: STATUS.TEACHER_PENDING, label: 'Chờ giảng viên duyệt' },
  { value: STATUS.BORROW_PENDING, label: 'Chờ quản lý duyệt' },
  { value: STATUS.APPROVED, label: 'Chờ nhận' },
  { value: STATUS.BORROWED, label: 'Đang mượn' },
  { value: STATUS.RETURN_PROCESSING, label: 'Đang xử lý trả' },
  { value: STATUS.RETURNED, label: 'Đã trả' },
  { value: STATUS.REJECTED, label: 'Từ chối' },
  { value: STATUS.CANCELLED, label: 'Đã hủy' },
  { value: STATUS.EXPIRED, label: 'Hết hạn giữ chỗ' },
  { value: 'OVERDUE', label: 'Quá hạn' }
]

const columns = [
  { title: 'Người mượn', dataIndex: 'borrowerName', key: 'borrowerName', sortKey: 'borrower', sortable: true, width: 190, fixed: 'left', filterType: 'search', filterPlaceholder: 'Tìm người mượn...' },
  { title: 'SĐT liên hệ', dataIndex: 'borrowerPhone', key: 'borrowerPhone', sortKey: 'borrowerPhone', sortable: true, width: 155, fixed: 'left', filterType: 'search', filterPlaceholder: 'Tìm số điện thoại...' },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', sortKey: 'device', sortable: true, width: 190, fixed: 'left', filterType: 'search', filterPlaceholder: 'Tìm thiết bị...' },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', sortKey: 'serial', sortable: true, width: 175, filterType: 'search', filterPlaceholder: 'Tìm số seri...' },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', sortKey: 'requestDate', sortable: true, width: 170 },
  { title: 'Hạn trả', dataIndex: 'expectedReturnDate', key: 'expectedReturnDate', sortKey: 'expectedReturnDate', sortable: true, width: 155 },
  { title: 'Ngày trả thực tế', dataIndex: 'actualReturnDate', key: 'actualReturnDate', sortKey: 'actualReturnDate', sortable: true, width: 175 },
  { title: 'Tình trạng trả', dataIndex: 'returnCondition', key: 'returnCondition', sortKey: 'returnCondition', sortable: true, width: 180, className: 'status-column' },
  { title: 'Ghi chú kiểm tra', dataIndex: 'returnInspectionNote', key: 'returnInspectionNote', sortKey: 'returnInspectionNote', sortable: true, width: 200 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', sortKey: 'status', sortable: true, align: 'center', width: 255, className: 'status-column', filterType: 'select', filterKey: 'status', filterOptions: borrowStatusOptions },
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

const openReturn = record => {
  returnRecord.value = record
  isReturnVisible.value = true
}

const handleRemind = async record => {
  if (isReminding(record.id)) return

  remindingRecordIds.value = new Set(remindingRecordIds.value).add(record.id)
  const messageKey = `remind-${record.id}`
  try {
    message.loading({ content: 'Đang gửi nhắc trả...', key: messageKey })
    const result = await borrowApi.remind(record.id)
    message.success({ content: getApiSuccessMessage(result, 'Đã gửi email nhắc trả thành công.'), key: messageKey })
  } catch (error) {
    message.error({ content: getApiErrorMessage(error, 'Không thể gửi nhắc trả.'), key: messageKey })
  } finally {
    const nextIds = new Set(remindingRecordIds.value)
    nextIds.delete(record.id)
    remindingRecordIds.value = nextIds
  }
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
      status: statusFilter.value,
      sortBy: sortState.field,
      sortDirection: sortState.order === 'descend' ? 'desc' : (sortState.order === 'ascend' ? 'asc' : undefined)
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

const applyColumnFilter = (column, value) => {
  if (column.filterKey === 'status') statusFilter.value = value
  else searchQuery.value = value || ''
  applyFilters()
}

const issueReportFor = equipmentId => selectedHandover.value?.issueReports?.find(issue => issue.equipmentId === equipmentId) || null

const openIssueReport = item => {
  issueReportItem.value = item
  issueForm.issueType = 'CONDITION'
  issueForm.description = ''
  resetIssueEvidence()
  issueReportVisible.value = true
}

const resetIssueEvidence = () => {
  issueEvidencePreviewUrls.value.forEach(url => URL.revokeObjectURL(url))
  issueEvidenceFiles.value = []
  issueEvidencePreviewUrls.value = []
}

const selectIssueEvidence = file => {
  if (issueEvidenceFiles.value.length >= 5) {
    message.warning('Mỗi báo cáo được đính kèm tối đa 5 ảnh.')
    return Upload.LIST_IGNORE
  }
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!['jpg', 'jpeg', 'png', 'webp'].includes(extension) || !file.type?.startsWith('image/')) {
    message.error('Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.')
    return Upload.LIST_IGNORE
  }
  if (file.size > 10 * 1024 * 1024) {
    message.error('Mỗi ảnh không được vượt quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  issueEvidenceFiles.value.push(file)
  issueEvidencePreviewUrls.value.push(URL.createObjectURL(file))
  return false
}

const removeIssueEvidence = index => {
  const previewUrl = issueEvidencePreviewUrls.value[index]
  if (previewUrl) URL.revokeObjectURL(previewUrl)
  issueEvidenceFiles.value.splice(index, 1)
  issueEvidencePreviewUrls.value.splice(index, 1)
}

const closeIssueReport = () => {
  if (issueSubmitting.value) return
  issueReportVisible.value = false
  resetIssueEvidence()
}

const submitIssueReport = async () => {
  const description = issueForm.description.trim()
  if (!issueReportItem.value || !description) {
    message.warning('Vui lòng mô tả sai lệch thực tế.')
    return
  }

  issueSubmitting.value = true
  try {
    await handoverApi.createIssueReport(selectedRecord.value.id, {
      equipmentId: issueReportItem.value.equipmentId,
      issueType: issueForm.issueType,
      description,
      files: issueEvidenceFiles.value
    })
    message.success('Đã gửi báo cáo sai lệch cho quản lý Lab.')
    issueReportVisible.value = false
    resetIssueEvidence()
    await openHandover(selectedRecord.value)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể gửi báo cáo sai lệch.'))
  } finally {
    issueSubmitting.value = false
  }
}

const applyColumnSort = (column, order) => {
  sortState.field = order ? column.sortKey : undefined
  sortState.order = order
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
.handover-item-actions { display: flex; align-items: center; gap: 8px; margin-top: 12px; }
.handover-issue-summary { display: flex; align-items: flex-start; gap: 8px; margin-top: 12px; color: var(--color-secondary); font-size: 13px; }
.handover-issue-evidence-count { color: var(--color-secondary); font-size: 13px; }
.issue-upload-hint { margin-top: 8px; color: var(--color-secondary); font-size: 12px; }
.issue-evidence-list { display: grid; gap: 8px; margin-top: 10px; }
.issue-evidence-file { display: flex; align-items: center; gap: 10px; padding: 8px; border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.issue-evidence-file-copy { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 3px; }
.issue-evidence-file-copy strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.issue-evidence-file-copy span { color: var(--color-secondary); font-size: 12px; }
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
.request-actions { display: inline-flex; align-items: center; justify-content: center; gap: 4px; white-space: nowrap; }
.mobile-request-actions { display: flex; gap: 8px; }
.mobile-request-actions :deep(.ant-btn) { flex: 1; }
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
