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
          <template v-else-if="column.key === 'evidence'">
            <div class="evidence-thumbnail-cell">
              <span v-if="!firstEvidence(record)" class="evidence-empty">—</span>
              <a-spin
                v-else-if="evidenceLoading[evidenceKey(record, firstEvidence(record))]"
                size="small"
              />
              <a-image
                v-else-if="evidencePreviewUrls[evidenceKey(record, firstEvidence(record))]"
                class="evidence-thumbnail"
                :src="evidencePreviewUrls[evidenceKey(record, firstEvidence(record))]"
                :width="56"
                :height="42"
                :preview="true"
                :alt="`Bằng chứng của ${record.equipmentName || 'thiết bị'}`"
              />
              <a-tooltip v-else title="Không tải được ảnh. Bấm để thử lại">
                <a-button
                  type="text"
                  class="evidence-retry"
                  aria-label="Tải lại ảnh bằng chứng"
                  @click="loadEvidencePreview(record, firstEvidence(record))"
                >
                  <template #icon><PictureOutlined /></template>
                </a-button>
              </a-tooltip>
            </div>
          </template>
          <template v-else-if="column.key === 'description'">
            <span class="description-cell">{{ record.description }}</span>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-tooltip title="Xem chi tiết">
              <a-button type="text" class="view-action" aria-label="Xem chi tiết báo cáo sai lệch" @click="openDetails(record)">
                <template #icon><EyeOutlined /></template>
              </a-button>
            </a-tooltip>
          </template>
        </template>
      </a-table>

      <EmptyState v-if="!loading && !reports.length" description="Chưa có báo cáo sai lệch." />
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết báo cáo sai lệch" :footer="null" width="760px" wrap-class-name="app-modal app-detail-modal">
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
        <a-descriptions-item v-if="selectedReport.evidence?.length" label="Ảnh bằng chứng">
          <div class="issue-evidence-grid">
            <div v-for="evidence in selectedReport.evidence" :key="evidence.id" class="issue-evidence-card">
              <div class="issue-evidence-preview">
                <a-spin v-if="evidenceLoading[evidenceKey(selectedReport, evidence)]" size="small" />
                <a-image
                  v-else-if="evidencePreviewUrls[evidenceKey(selectedReport, evidence)]"
                  :src="evidencePreviewUrls[evidenceKey(selectedReport, evidence)]"
                  :width="96"
                  :height="72"
                  :preview="true"
                  :alt="`Ảnh bằng chứng ${evidence.OriginalFileName || evidence.originalFileName || ''}`"
                />
                <a-button
                  v-else
                  type="text"
                  aria-label="Tải lại ảnh bằng chứng"
                  @click="loadEvidencePreview(selectedReport, evidence)"
                >
                  <template #icon><PictureOutlined /></template>
                </a-button>
              </div>
              <div class="issue-evidence-card-copy">
                <span>{{ evidence.OriginalFileName || evidence.originalFileName }}</span>
                <small>{{ formatFileSize(evidence.FileSize ?? evidence.fileSize) }}</small>
              </div>
              <a-tooltip title="Tải ảnh">
                <a-button
                  type="text"
                  class="issue-evidence-download"
                  aria-label="Tải ảnh bằng chứng"
                  @click="downloadEvidence(selectedReport, evidence)"
                >
                  <template #icon><DownloadOutlined /></template>
                </a-button>
              </a-tooltip>
            </div>
          </div>
        </a-descriptions-item>
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
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { message } from 'ant-design-vue'
import { DownloadOutlined, EyeOutlined, PictureOutlined } from '@ant-design/icons-vue'
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
const evidencePreviewUrls = reactive({})
const evidenceLoading = reactive({})

const columns = [
  { title: 'Người mượn', dataIndex: 'borrowerName', key: 'borrowerName', width: 190 },
  { title: 'Thiết bị', dataIndex: 'equipmentName', key: 'equipmentName', width: 220 },
  { title: 'Loại sai lệch', dataIndex: 'issueType', key: 'issueType', width: 190 },
  { title: 'Nội dung báo cáo', dataIndex: 'description', key: 'description', width: 320 },
  { title: 'Bằng chứng', dataIndex: 'evidence', key: 'evidence', width: 110, align: 'center' },
  { title: 'Thời gian gửi', dataIndex: 'reportedAt', key: 'reportedAt', width: 180 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 180, align: 'center', className: 'status-column' },
  { title: 'Hành động', key: 'action', width: 130, align: 'center', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }) }
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

const evidenceKey = (report, evidence) => `${report.id}-${evidence.id}`
const firstEvidence = report => report?.evidence?.[0] || null

const formatFileSize = bytes => {
  if (!bytes) return '0 KB'
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

const loadEvidencePreview = async (report, evidence, silent = false) => {
  if (!report || !evidence) return
  const key = evidenceKey(report, evidence)
  if (evidencePreviewUrls[key] || evidenceLoading[key]) return
  evidenceLoading[key] = true
  try {
    const blob = await handoverApi.downloadIssueEvidence(report.id, evidence.id)
    evidencePreviewUrls[key] = URL.createObjectURL(blob)
  } catch (error) {
    if (!silent) message.error(getApiErrorMessage(error, 'Không thể tải ảnh bằng chứng.'))
  } finally {
    evidenceLoading[key] = false
  }
}

const fetchReports = async () => {
  loading.value = true
  try {
    const result = await handoverApi.getIssueReports(statusFilter.value)
    reports.value = Array.isArray(result) ? result : []
    reports.value.forEach(report => {
      const evidence = firstEvidence(report)
      if (evidence) void loadEvidencePreview(report, evidence, true)
    })
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể tải báo cáo sai lệch.'))
  } finally {
    loading.value = false
  }
}

const openDetails = async report => {
  selectedReport.value = report
  detailsVisible.value = true
  await Promise.all((report.evidence || []).map(evidence => loadEvidencePreview(report, evidence)))
}

const downloadEvidence = async (report, evidence) => {
  try {
    const blob = await handoverApi.downloadIssueEvidence(report.id, evidence.id)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = evidence.OriginalFileName || evidence.originalFileName || 'anh-bang-chung'
    link.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể tải ảnh bằng chứng.'))
  }
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
onBeforeUnmount(() => {
  Object.values(evidencePreviewUrls).forEach(url => URL.revokeObjectURL(url))
})
</script>

<style scoped>
.handover-issues-container { padding: 0; }
.toolbar { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; margin-bottom: 24px; }
.toolbar h2 { margin: 0; color: var(--color-ink); font-weight: 650; }
.toolbar p { margin: 6px 0 0; color: var(--color-secondary); }
.status-filter { min-width: 190px; }
.handover-issues-card { border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,.05); }
.description-cell { display: -webkit-box; overflow: hidden; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.view-action { color: var(--color-primary); }
.evidence-thumbnail-cell { display: flex; min-height: 42px; align-items: center; justify-content: center; }
.evidence-thumbnail { overflow: hidden; border: 1px solid var(--color-border, #e5e7eb); border-radius: 6px; cursor: zoom-in; background: #f5f5f5; }
.evidence-thumbnail :deep(.ant-image-img) { width: 56px; height: 42px; object-fit: cover; }
.evidence-empty { color: var(--color-secondary); }
.evidence-retry { color: var(--color-secondary); }
.issue-evidence-grid { display: grid; gap: 10px; }
.issue-evidence-card { display: grid; grid-template-columns: 96px minmax(0, 1fr) 36px; align-items: center; gap: 14px; min-height: 92px; padding: 10px; border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; background: #fff; }
.issue-evidence-preview { display: flex; width: 96px; height: 72px; align-items: center; justify-content: center; overflow: hidden; border-radius: 7px; background: #f1f5f9; }
.issue-evidence-preview :deep(.ant-image) { flex: 0 0 auto; overflow: hidden; border-radius: 7px; background: #f1f5f9; }
.issue-evidence-preview :deep(.ant-image-img) { width: 96px; height: 72px; object-fit: cover; }
.issue-evidence-card-copy { display: flex; min-width: 0; flex-direction: column; gap: 3px; }
.issue-evidence-card-copy span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.issue-evidence-card-copy small { color: var(--color-secondary); }
.issue-evidence-download { color: var(--color-primary); }
.details-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 20px; }
@media (max-width: 767px) {
  .toolbar { flex-direction: column; }
  .status-filter { width: 100%; }
  .issue-evidence-card { grid-template-columns: 72px minmax(0, 1fr) 32px; gap: 10px; min-height: 74px; padding: 8px; }
  .issue-evidence-preview { width: 72px; height: 54px; }
  .issue-evidence-preview :deep(.ant-image-img) { width: 72px; height: 54px; }
}
</style>
