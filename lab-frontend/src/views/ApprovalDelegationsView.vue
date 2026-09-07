<template>
  <div class="delegations-container">
    <div class="toolbar">
      <div>
        <h2>Ủy quyền duyệt</h2>
        <p>Cho phép giảng viên thay mặt quản lý duyệt yêu cầu trong một khoảng thời gian cụ thể.</p>
      </div>
      <a-button type="primary" @click="openCreateModal">+ Tạo ủy quyền</a-button>
    </div>

    <a-card :bordered="false" class="delegations-card">
      <a-table
        class="desktop-table"
        :data-source="displayDelegations"
        :columns="columns"
        :loading="loading"
        row-key="id"
        bordered
        :pagination="false"
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
            :value="column.filterKey === 'scope' ? scopeFilter : (column.filterKey === 'status' ? statusFilter : searchQuery)"
            :placeholder="column.filterPlaceholder"
            @apply="value => applyColumnFilter(column, value)"
            @sort="value => applyColumnSort(column, value)"
          />
          <span v-else>{{ column.title }}</span>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'delegate'">
            <div class="person-cell">
              <strong>{{ record.delegateName || record.delegateUsername }}</strong>
              <span>{{ record.delegateCode || record.delegateUsername }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'delegator'">
            {{ record.delegatorName || record.delegatorUsername || '—' }}
          </template>
          <template v-else-if="column.key === 'scope'">
            {{ scopeLabel(record.scope) }}
          </template>
          <template v-else-if="column.key === 'handover'">
            <a-tag :color="record.canHandover ? 'blue' : 'default'">
              {{ record.canHandover ? 'Duyệt + bàn giao' : 'Chỉ duyệt' }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'period'">
            <div class="period-cell">
              <span>{{ formatDateTime(record.startsAt) }}</span>
              <span>đến {{ formatDateTime(record.endsAt) }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="statusColor(record.status)">{{ statusLabel(record.status) }}</a-tag>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-button v-if="record.isActive && record.status !== 'EXPIRED'" danger size="small" @click="revokeDelegation(record)">
              Thu hồi
            </a-button>
            <span v-else class="muted">—</span>
          </template>
        </template>
      </a-table>

      <ResponsiveDataList :items="displayDelegations" :loading="loading" empty-description="Chưa có quyền ủy quyền">
        <template #default="{ item }">
          <div class="mobile-delegation-heading">
            <div class="person-cell">
              <strong>{{ item.delegateName || item.delegateUsername }}</strong>
              <span>{{ item.delegateCode || item.delegateUsername }}</span>
            </div>
            <a-tag :color="statusColor(item.status)">{{ statusLabel(item.status) }}</a-tag>
          </div>
          <dl class="mobile-delegation-details">
            <div><dt>Phạm vi</dt><dd>{{ scopeLabel(item.scope) }}</dd></div>
            <div><dt>Quyền bàn giao</dt><dd>{{ item.canHandover ? 'Duyệt + bàn giao' : 'Chỉ duyệt' }}</dd></div>
            <div><dt>Người ủy quyền</dt><dd>{{ item.delegatorName || item.delegatorUsername || '—' }}</dd></div>
            <div><dt>Thời gian</dt><dd>{{ formatDateTime(item.startsAt) }} – {{ formatDateTime(item.endsAt) }}</dd></div>
            <div><dt>Lý do</dt><dd>{{ item.reason || '—' }}</dd></div>
          </dl>
          <a-button v-if="item.isActive && item.status !== 'EXPIRED'" danger block @click="revokeDelegation(item)">Thu hồi</a-button>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      title="Tạo quyền ủy quyền duyệt"
      ok-text="Tạo ủy quyền"
      cancel-text="Hủy"
      :confirm-loading="submitting"
      @ok="submitDelegation"
    >
      <a-alert
        type="info"
        show-icon
        message="Phân quyền theo đúng phạm vi và thời gian đã chọn."
        description="Có thể cấp thêm quyền lập biên bản bàn giao tài sản/vật tư; các thao tác quản lý khác vẫn thuộc về Admin, Trưởng lab hoặc Phó lab."
        class="delegation-alert"
      />
      <a-form ref="formRef" :model="formState" layout="vertical">
        <a-form-item label="Giảng viên được ủy quyền" name="delegateUserId" :rules="[{ required: true, message: 'Vui lòng chọn giảng viên.' }]">
          <a-select v-model:value="formState.delegateUserId" show-search option-filter-prop="label" placeholder="Chọn giảng viên">
            <a-select-option v-for="teacher in teachers" :key="teacher.id" :value="teacher.id" :label="teacher.fullName || teacher.username">
              {{ teacher.fullName || teacher.username }}<span v-if="teacher.universityCode"> ({{ teacher.universityCode }})</span>
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Phạm vi ủy quyền" name="scope">
          <a-select v-model:value="formState.scope">
            <a-select-option value="BORROW_REQUEST">Duyệt yêu cầu mượn/trả</a-select-option>
            <a-select-option value="CONSUMABLE_REQUEST">Duyệt yêu cầu cấp phát</a-select-option>
            <a-select-option value="BOTH">Cả hai loại yêu cầu</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item name="canHandover" value-prop-name="checked">
          <a-checkbox v-model:checked="formState.canHandover">
            Cho phép giảng viên lập biên bản bàn giao
          </a-checkbox>
          <div class="form-help">Áp dụng cho phạm vi yêu cầu đã chọn ở trên.</div>
        </a-form-item>
        <a-row :gutter="12">
          <a-col :xs="24" :sm="12">
            <a-form-item label="Bắt đầu" name="startsAt" :rules="dateRules">
              <a-date-picker v-model:value="formState.startsAt" show-time format="DD/MM/YYYY HH:mm" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Kết thúc" name="endsAt" :rules="dateRules">
              <a-date-picker v-model:value="formState.endsAt" show-time format="DD/MM/YYYY HH:mm" style="width: 100%" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-form-item label="Lý do ủy quyền" name="reason" :rules="[{ required: true, message: 'Vui lòng nhập lý do ủy quyền.' }]">
          <a-textarea v-model:value="formState.reason" :rows="3" :maxlength="1000" show-count placeholder="Ví dụ: Quản lý đi công tác ngày..." />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import dayjs from 'dayjs'
import { message, Modal } from 'ant-design-vue'
import { approvalDelegationApi } from '../api/approvalDelegationApi'
import { userApi } from '../api/userApi'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDateTime as formatDateTime } from '../utils/dateTime'
import { sortTableRows } from '../utils/tableSort'

const delegations = ref([])
const teachers = ref([])
const loading = ref(false)
const submitting = ref(false)
const modalVisible = ref(false)
const formRef = ref(null)
const formState = reactive({
  delegateUserId: undefined,
  scope: 'BOTH',
  canHandover: false,
  startsAt: dayjs(),
  endsAt: dayjs().add(1, 'day'),
  reason: ''
})
const dateRules = [{ required: true, message: 'Vui lòng chọn thời gian.' }]
const searchQuery = ref('')
const scopeFilter = ref(undefined)
const statusFilter = ref(undefined)
const sortState = reactive({ field: undefined, order: undefined })
const scopeOptions = [
  { value: 'BORROW_REQUEST', label: 'Mượn/trả' },
  { value: 'CONSUMABLE_REQUEST', label: 'Cấp phát vật tư' },
  { value: 'BOTH', label: 'Mượn/trả và cấp phát' }
]
const statusOptions = [
  { value: 'ACTIVE', label: 'Đang hiệu lực' },
  { value: 'SCHEDULED', label: 'Sắp áp dụng' },
  { value: 'EXPIRED', label: 'Hết hiệu lực' },
  { value: 'REVOKED', label: 'Đã thu hồi' }
]

const columns = [
  { title: 'Giảng viên được ủy quyền', key: 'delegate', sortKey: 'delegate', sortable: true, filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm giảng viên...', width: 220 },
  { title: 'Người ủy quyền', key: 'delegator', sortKey: 'delegator', sortable: true, filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm người ủy quyền...', width: 170 },
  { title: 'Phạm vi', key: 'scope', sortKey: 'scope', sortable: true, filterType: 'select', filterKey: 'scope', filterOptions: scopeOptions, width: 190 },
  { title: 'Quyền bàn giao', key: 'handover', sortKey: 'handover', sortable: true, width: 145, align: 'center' },
  { title: 'Thời gian', key: 'period', sortKey: 'startsAt', sortable: true, width: 230 },
  { title: 'Lý do', dataIndex: 'reason', key: 'reason', sortKey: 'reason', sortable: true, filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm lý do...', width: 260 },
  { title: 'Trạng thái', key: 'status', sortKey: 'status', sortable: true, filterType: 'select', filterKey: 'status', filterOptions: statusOptions, width: 120, align: 'center' },
  { title: 'Hành động', key: 'action', width: 110, align: 'center' }
]

const scopeValue = item => item.scope || ''
const statusValue = item => !item.isActive
  ? 'REVOKED'
  : item.status || ''
const filteredDelegations = computed(() => {
  const keyword = searchQuery.value.trim().toLowerCase()
  return delegations.value.filter(item => {
    const matchesSearch = !keyword || [item.delegateName, item.delegateUsername, item.delegatorName, item.delegatorUsername, item.reason]
      .some(value => String(value || '').toLowerCase().includes(keyword))
    return matchesSearch
      && (!scopeFilter.value || scopeValue(item) === scopeFilter.value)
      && (!statusFilter.value || statusValue(item) === statusFilter.value)
  })
})
const displayDelegations = computed(() => sortTableRows(
  filteredDelegations.value,
  sortState.field,
  sortState.order,
  (item, field) => {
    if (field === 'delegate') return item.delegateName || item.delegateUsername
    if (field === 'delegator') return item.delegatorName || item.delegatorUsername
    if (field === 'scope') return scopeLabel(item.scope)
    if (field === 'handover') return item.canHandover ? 1 : 0
    if (field === 'period' || field === 'startsAt') return item.startsAt
    if (field === 'status') return statusLabel(statusValue(item))
    return item[field]
  }
))

const scopeLabel = scope => ({
  BORROW_REQUEST: 'Mượn/trả',
  CONSUMABLE_REQUEST: 'Cấp phát vật tư',
  BOTH: 'Mượn/trả và cấp phát'
}[scope] || scope || '—')

const statusLabel = status => ({ ACTIVE: 'Đang hiệu lực', SCHEDULED: 'Sắp áp dụng', EXPIRED: 'Hết hiệu lực' }[status] || 'Đã thu hồi')
const statusColor = status => ({ ACTIVE: 'green', SCHEDULED: 'blue', EXPIRED: 'orange' }[status] || 'red')

const applyColumnFilter = (column, value) => {
  if (column.filterKey === 'scope') scopeFilter.value = value
  else if (column.filterKey === 'status') statusFilter.value = value
  else searchQuery.value = value || ''
}

const applyColumnSort = (column, order) => {
  sortState.field = order ? column.sortKey : undefined
  sortState.order = order
}

const fetchData = async () => {
  loading.value = true
  try {
    const [delegationData, teacherData] = await Promise.all([
      approvalDelegationApi.getAll(),
      userApi.getTeachers()
    ])
    delegations.value = delegationData || []
    teachers.value = teacherData || []
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách ủy quyền.'))
  } finally {
    loading.value = false
  }
}

const resetForm = () => {
  formState.delegateUserId = undefined
  formState.scope = 'BOTH'
  formState.canHandover = false
  formState.startsAt = dayjs()
  formState.endsAt = dayjs().add(1, 'day')
  formState.reason = ''
}

const openCreateModal = () => {
  resetForm()
  modalVisible.value = true
}

const submitDelegation = async () => {
  try {
    await formRef.value?.validate()
    if (!formState.startsAt || !formState.endsAt || !formState.endsAt.isAfter(formState.startsAt)) {
      message.warning('Thời gian kết thúc phải sau thời gian bắt đầu.')
      return
    }
    submitting.value = true
    await approvalDelegationApi.create({
      delegateUserId: Number(formState.delegateUserId),
      scope: formState.scope,
      canHandover: formState.canHandover,
      startsAt: formState.startsAt.toISOString(),
      endsAt: formState.endsAt.toISOString(),
      reason: formState.reason.trim()
    })
    message.success('Đã tạo quyền ủy quyền duyệt.')
    modalVisible.value = false
    await fetchData()
  } catch (error) {
    if (!error?.errorFields) message.error(getApiErrorMessage(error, 'Không thể tạo quyền ủy quyền.'))
  } finally {
    submitting.value = false
  }
}

const revokeDelegation = record => {
  Modal.confirm({
    title: 'Thu hồi quyền ủy quyền?',
    content: `Giảng viên ${record.delegateName || record.delegateUsername} sẽ không thể tiếp tục duyệt trong phạm vi này.`,
    okText: 'Thu hồi',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await approvalDelegationApi.revoke(record.id)
        message.success('Đã thu hồi quyền ủy quyền.')
        await fetchData()
      } catch (error) {
        message.error(getApiErrorMessage(error, 'Không thể thu hồi quyền ủy quyền.'))
      }
    }
  })
}

onMounted(fetchData)
</script>

<style scoped>
.delegations-container { padding: 0; }
.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 24px; }
.toolbar h2 { margin: 0 0 6px; color: #1f1f1f; font-weight: 600; }
.toolbar p { margin: 0; color: #6b7280; }
.delegations-card { border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, .05); }
.person-cell, .period-cell { display: grid; gap: 3px; }
.person-cell span, .period-cell span { color: #6b7280; font-size: 12px; }
.muted { color: #9ca3af; }
.delegation-alert { margin-bottom: 18px; }
.form-help { margin-top: 4px; color: #6b7280; font-size: 12px; }
.mobile-delegation-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; margin-bottom: 12px; }
.mobile-delegation-details { display: grid; gap: 8px; margin: 0 0 14px; }
.mobile-delegation-details > div { display: grid; grid-template-columns: 110px 1fr; gap: 8px; }
.mobile-delegation-details dt { color: #6b7280; }
.mobile-delegation-details dd { margin: 0; color: #1f2937; word-break: break-word; }
@media (max-width: 767px) {
  .toolbar { align-items: stretch; flex-direction: column; }
  .toolbar .ant-btn { width: 100%; }
  .desktop-table { display: none; }
}
</style>
