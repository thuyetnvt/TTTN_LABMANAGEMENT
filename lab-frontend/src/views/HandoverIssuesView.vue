<template>
  <div class="handover-issues-container">
    <div class="toolbar">
      <div>
        <h2>Báo cáo sai lệch bàn giao</h2>
        <p>Kiểm tra các vấn đề sinh viên phát hiện ngay khi nhận thiết bị tại Lab.</p>
      </div>
      <a-select v-model:value="statusFilter" allow-clear placeholder="Tất cả trạng thái" class="status-filter" @change="fetchReports">
        <a-select-option value="HANDOVER_ISSUE_PENDING">Chờ xử lý</a-select-option>
        <a-select-option value="HANDOVER_ISSUE_RESOLVED">Đã ghi nhận</a-select-option>
        <a-select-option value="HANDOVER_ISSUE_REJECTED">Đã từ chối</a-select-option>
      </a-select>
    </div>

    <a-card :bordered="false" class="handover-issues-card">
      <a-table
        class="desktop-table"
        :data-source="reports"
        :columns="columns"
        :loading="loading"
        row-key="id"
        bordered
        :scroll="{ x: 1280 }"
        :pagination="{ pageSize: 20, showSizeChanger: true, pageSizeOptions: ['20', '50', '100'] }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="statusColor(record.status)">{{ statusLabel(record.status) }}</a-tag>
          </template>
          <template v-else-if="column.key === 'issueType'">
            {{ issueTypeLabel(record.issueType) }}
          </template>
          <template v-else-if="column.key === 'reportedAt'">
            {{ formatDateTime(record.reportedAt) }}
          </template>
          <template v-else-if="column.key === 'description'">
            <span class="description-cell">{{ record.description }}</span>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-button type="text" class="view-action" @click="openDetails(record)">Xem chi tiết</a-button>
          </template>
        </template>
      </a-table>

      <EmptyState v-if="!loading && !reports.length" description="Chưa có báo cáo sai lệch." />
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết báo cáo sai lệch" :footer="null" width="760px">
      <a-descriptions v-if="selectedReport" bordered size="small" :column="1">
        <a-descriptions-item label="Người mượn">
          {{ selectedReport.borrowerName || selectedReport.borrowerUsername || '—' }}
        </a-descriptions-item>
        <a-descriptions-item label="Biên bản bàn giao">{{ selectedReport.handoverCode }}</a-descriptions-item>
        <a-descriptions-item label="Thiết bị">
          {{ selectedReport.equipmentName }} · {{ selectedReport.serial || selectedReport.assetCode || 'Không có mã' }}
        </a-descriptions-item>
        <a-descriptions-item label="Loại sai lệch">{{ issueTypeLabel(selectedReport.issueType) }}</a-descriptions-item>
        <a-descriptions-item label="Nội dung sinh viên báo cáo">{{ selectedReport.description }}</a-descriptions-item>
        <a-descriptions-item label="Thời gian gửi">{{ formatDateTime(selectedReport.reportedAt) }}</a-descriptions-item>
        <a-descriptions-item v-if="selectedReport.status !== 'HANDOVER_ISSUE_PENDING'" label="Kết quả xử lý">
          {{ selectedReport.status === 'HANDOVER_ISSUE_REJECTED' ? 'Từ chối báo cáo' : 'Đã ghi nhận sai lệch' }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedReport.resolutionNote" label="Ghi chú xử lý">
          {{ selectedReport.resolutionNote }}
        </a-descriptions-item>
        <a-descriptions-item v-if="selectedReport.resolvedAt" label="Thời gian xử lý">
          {{ formatDateTime(selectedReport.resolvedAt) }} · {{ selectedReport.resolvedByName || 'Quản lý Lab' }}
        </a-descriptions-item>
      </a-descriptions>
      <div class="details-actions">
        <a-button @click="detailsVisible = false">Đóng</a-button>
        <template v-if="selectedReport?.status === 'HANDOVER_ISSUE_PENDING'">
          <a-button danger @click="openResolve('REJECT')">Từ chối báo cáo</a-button>
          <a-button type="primary" @click="openResolve('ACKNOWLEDGE')">Ghi nhận sai lệch</a-button>
        </template>
      </div>
    </a-modal>

    <a-modal
      v-model:open="resolveVisible"
      :title="resolveAction === 'REJECT' ? 'Từ chối báo cáo sai lệch' : 'Ghi nhận báo cáo sai lệch'"
      ok-text="Lưu kết quả"
      cancel-text="Hủy"
      :confirm-loading="resolving"
      @ok="submitResolve"
    >
      <a-alert
        v-if="resolveAction === 'ACKNOWLEDGE'"
        type="info"
        show-icon
        message="Sinh viên sẽ được yêu cầu kiểm tra lại tại Lab rồi xác nhận nhận thiết bị."
        style="margin-bottom: 16px"
      />
      <a-form layout="vertical">
        <a-form-item label="Ghi chú xử lý" required>
          <a-textarea v-model:value="resolutionNote" :rows="4" maxlength="2000" show-count placeholder="Nhập kết quả kiểm tra và hướng xử lý thực tế..." />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { message } from 'ant-design-vue'
import EmptyState from '../components/EmptyState.vue'
import { handoverApi } from '../api/handoverApi'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDateTime as formatDateTime } from '../utils/dateTime'

const reports = ref([])
const loading = ref(false)
const statusFilter = ref(undefined)
const detailsVisible = ref(false)
const resolveVisible = ref(false)
const resolving = ref(false)
const selectedReport = ref(null)
const resolveAction = ref('ACKNOWLEDGE')
const resolutionNote = ref('')

const columns = [
  { title: 'Người mượn', dataIndex: 'borrowerName', key: 'borrowerName', width: 190 },
  { title: 'Thiết bị', dataIndex: 'equipmentName', key: 'equipmentName', width: 220 },
  { title: 'Loại sai lệch', dataIndex: 'issueType', key: 'issueType', width: 190 },
  { title: 'Nội dung báo cáo', dataIndex: 'description', key: 'description', width: 320 },
  { title: 'Thời gian gửi', dataIndex: 'reportedAt', key: 'reportedAt', width: 180 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 160, align: 'center' },
  { title: 'Hành động', key: 'action', width: 150, align: 'center' }
]

const issueTypeLabel = type => ({
  CONDITION: 'Tình trạng khác mô tả',
  ACCESSORIES: 'Thiếu hoặc sai phụ kiện',
  WRONG_ASSET: 'Sai thiết bị/mã/serial',
  NOT_WORKING: 'Thiết bị không hoạt động',
  OTHER: 'Khác'
}[type] || type || 'Khác')

const statusLabel = status => ({
  HANDOVER_ISSUE_PENDING: 'Chờ xử lý',
  HANDOVER_ISSUE_RESOLVED: 'Đã ghi nhận',
  HANDOVER_ISSUE_REJECTED: 'Đã từ chối'
}[status] || status || 'Không xác định')

const statusColor = status => ({
  HANDOVER_ISSUE_PENDING: 'orange',
  HANDOVER_ISSUE_RESOLVED: 'green',
  HANDOVER_ISSUE_REJECTED: 'red'
}[status] || 'default')

const fetchReports = async () => {
  loading.value = true
  try {
    reports.value = await handoverApi.getIssueReports(statusFilter.value)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể tải báo cáo sai lệch.'))
  } finally {
    loading.value = false
  }
}

const openDetails = report => {
  selectedReport.value = report
  detailsVisible.value = true
}

const openResolve = action => {
  resolveAction.value = action
  resolutionNote.value = ''
  resolveVisible.value = true
}

const submitResolve = async () => {
  const note = resolutionNote.value.trim()
  if (!selectedReport.value || !note) {
    message.warning('Vui lòng nhập ghi chú xử lý.')
    return
  }

  resolving.value = true
  try {
    await handoverApi.resolveIssueReport(selectedReport.value.id, { action: resolveAction.value, note })
    message.success('Đã cập nhật kết quả báo cáo sai lệch.')
    resolveVisible.value = false
    detailsVisible.value = false
    await fetchReports()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể cập nhật báo cáo sai lệch.'))
  } finally {
    resolving.value = false
  }
}

onMounted(fetchReports)
</script>

<style scoped>
.handover-issues-container { padding: 0; }
.toolbar { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; margin-bottom: 24px; }
.toolbar h2 { margin: 0; color: var(--color-ink); font-weight: 650; }
.toolbar p { margin: 6px 0 0; color: var(--color-secondary); }
.status-filter { min-width: 190px; }
.handover-issues-card { border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,.05); }
.description-cell { display: -webkit-box; overflow: hidden; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.details-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 20px; }
@media (max-width: 767px) {
  .toolbar { flex-direction: column; }
  .status-filter { width: 100%; }
}
</style>
