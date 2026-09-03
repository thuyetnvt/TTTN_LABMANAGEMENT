<template>
  <div class="teacher-approval-container">
    <div class="toolbar">
      <h2>Duyệt bảo lãnh mượn thiết bị</h2>
      <p>Danh sách yêu cầu sinh viên nhờ giảng viên bảo lãnh trước khi gửi lên kho.</p>
      <a-input-search v-model:value="searchQuery" allow-clear placeholder="Sinh viên, thiết bị..." class="filter-search" style="margin-bottom: 16px" @search="applySearch" />
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'device'">
            <div>{{ record.device }}</div>
            <div v-for="detail in record.details || []" :key="detail.equipmentId" class="detail-line">
              {{ detail.equipmentName }} — {{ detail.serial }}
            </div>
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" />
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-button type="primary" size="small" @click="openDecision(record, 'approve')">Bảo lãnh</a-button>
              <a-button type="primary" danger size="small" @click="openDecision(record, 'reject')">Từ chối</a-button>
            </a-space>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="dataSource" :loading="loading" :pagination="tablePagination" empty-description="Không có yêu cầu chờ bảo lãnh" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-approval-heading"><strong>{{ borrowerLabel(item) }}</strong><StatusBadge :status="item.status" type="borrow" /></div>
          <div class="mobile-approval-device">{{ item.device }}</div>
          <div v-for="detail in item.details || []" :key="detail.equipmentId" class="detail-line">{{ detail.equipmentName }} — {{ detail.serial }}</div>
          <dl class="mobile-approval-details">
            <div><dt>Ngày đăng ký</dt><dd>{{ formatDate(item.requestDate) }}</dd></div>
            <div><dt>Dự kiến trả</dt><dd>{{ formatDate(item.returnDate) }}</dd></div>
            <div><dt>Mục đích</dt><dd>{{ item.purpose || '—' }}</dd></div>
          </dl>
          <div class="mobile-approval-actions">
            <a-button type="primary" @click="openDecision(item, 'approve')">Bảo lãnh</a-button>
            <a-button danger @click="openDecision(item, 'reject')">Từ chối</a-button>
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>
  </div>

  <a-modal
    v-model:open="decisionOpen"
    :title="decisionType === 'approve' ? 'Xác nhận bảo lãnh' : 'Từ chối bảo lãnh'"
    :confirm-loading="decisionLoading"
    ok-text="Xác nhận"
    cancel-text="Hủy"
    @ok="submitDecision"
  >
    <p v-if="selectedRecord">Yêu cầu của {{ borrowerLabel(selectedRecord) }} — {{ selectedRecord.device }}</p>
    <a-form-item label="Ghi chú quyết định" required>
      <a-textarea v-model:value="decisionNote" :rows="4" placeholder="Nhập lý do hoặc ghi chú xử lý..." />
    </a-form-item>
  </a-modal>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { formatVietnamDate } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const dataSource = ref([])
const loading = ref(false)
const searchQuery = ref('')
const decisionOpen = ref(false)
const decisionLoading = ref(false)
const decisionType = ref('approve')
const decisionNote = ref('')
const selectedRecord = ref(null)

const borrowerLabel = record => record?.borrowerName?.trim() || record?.student || 'Không xác định'

const columns = [
  { title: 'Sinh viên', dataIndex: 'borrowerName', key: 'borrowerName' },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device' },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate' },
  { title: 'Dự kiến trả', dataIndex: 'returnDate', key: 'returnDate' },
  { title: 'Mục đích', dataIndex: 'purpose', key: 'purpose' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center' },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 190, align: 'center' }
]

onMounted(() => fetchRequests())

const formatDate = value => formatVietnamDate(value)

const fetchRequests = async () => {
  loading.value = true
  try {
    const response = await borrowApi.getTeacherPendingPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined
    })
    dataSource.value = response.items || []
    tablePagination.total = response.total || 0
  } catch {
    message.error('Lỗi khi tải danh sách yêu cầu bảo lãnh!')
  } finally {
    loading.value = false
  }
}

const applySearch = () => {
  tablePagination.current = 1
  fetchRequests()
}

const handleTableChange = pager => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchRequests()
}

const openDecision = (record, type) => {
  selectedRecord.value = record
  decisionType.value = type
  decisionNote.value = ''
  decisionOpen.value = true
}

const submitDecision = async () => {
  const note = decisionNote.value.trim()
  if (!note) {
    message.warning('Vui lòng nhập ghi chú quyết định!')
    return
  }

  decisionLoading.value = true
  try {
    const record = selectedRecord.value
    if (decisionType.value === 'approve') {
      await borrowApi.teacherApprove(record.id, note)
      message.success(`Đã bảo lãnh cho sinh viên ${borrowerLabel(record)}. Đơn đã chuyển lên kho.`)
    } else {
      await borrowApi.teacherReject(record.id, note)
      message.warning(`Đã từ chối bảo lãnh yêu cầu của sinh viên ${borrowerLabel(record)}.`)
    }
    decisionOpen.value = false
    fetchRequests()
  } catch {
    message.error(decisionType.value === 'approve' ? 'Lỗi duyệt yêu cầu!' : 'Lỗi từ chối yêu cầu!')
  } finally {
    decisionLoading.value = false
  }
}
</script>

<style scoped>
.teacher-approval-container {
  padding: 0;
}

.toolbar h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.toolbar p {
  color: #6b7280;
  margin-bottom: 24px;
}

.detail-line {
  color: #6b7280;
  font-size: 12px;
}
.mobile-approval-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-approval-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-approval-device { margin-top: 8px; font-weight: 600; }
.mobile-approval-details { display: grid; gap: 7px; margin: 12px 0; }
.mobile-approval-details div { display: flex; justify-content: space-between; gap: 12px; }
.mobile-approval-details dt { color: var(--color-text-secondary); }
.mobile-approval-details dd { margin: 0; max-width: 62%; text-align: right; }
.mobile-approval-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
@media (max-width: 767px) {
  .desktop-table { display: none; }
  .toolbar :deep(.ant-input-search) { width: 100% !important; }
}
</style>
