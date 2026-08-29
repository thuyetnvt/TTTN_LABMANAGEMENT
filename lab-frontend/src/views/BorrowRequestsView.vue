<template>
  <div class="borrow-requests-container">
    <div class="toolbar">
      <h2>Duyệt yêu cầu mượn/trả</h2>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1200 }" :pagination="tablePagination">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'dueStatus'">
            <a-tag v-if="record.isOverdue" color="red">Quá hạn {{ Math.abs(record.daysUntilDue) }} ngày</a-tag>
            <a-tag v-else-if="statusMatches(record.status, STATUS.BORROWED) && record.daysUntilDue <= 2" color="orange">Sắp tới hạn</a-tag>
            <a-tag v-else color="green">Trong hạn</a-tag>
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" />
          </template>
          <template v-else-if="column.key === 'details'">
            <div v-for="detail in record.details" :key="detail.id">
              {{ detail.equipmentName }} x{{ detail.quantity }}
            </div>
          </template>
          <template v-else-if="column.key === 'action'">
            <template v-if="isManagerRole(role)">
              <div class="request-actions">
                <template v-if="statusMatches(record.status, STATUS.BORROW_PENDING)">
                  <a-button type="primary" size="small" @click="handleApprove(record)">Duyệt</a-button>
                  <a-dropdown trigger="click">
                    <a-tooltip title="Thêm thao tác">
                      <a-button
                        type="text"
                        size="small"
                        class="request-more-button"
                        aria-label="Mở thêm thao tác phiếu mượn"
                      >
                        <template #icon><MoreOutlined /></template>
                      </a-button>
                    </a-tooltip>
                    <template #overlay>
                      <a-menu @click="event => handleActionMenuClick(event, record)">
                        <a-menu-item key="reject">Từ chối</a-menu-item>
                      </a-menu>
                    </template>
                  </a-dropdown>
                </template>
                <template v-else-if="statusMatches(record.status, STATUS.BORROWED)">
                  <a-button type="default" size="small" @click="showReturnModal(record)">Kiểm tra trả</a-button>
                  <a-dropdown trigger="click">
                    <a-tooltip title="Thêm thao tác">
                      <a-button
                        type="text"
                        size="small"
                        class="request-more-button"
                        aria-label="Mở thêm thao tác phiếu mượn"
                      >
                        <template #icon><MoreOutlined /></template>
                      </a-button>
                    </a-tooltip>
                    <template #overlay>
                      <a-menu @click="event => handleActionMenuClick(event, record)">
                        <a-menu-item key="handover">Bàn giao</a-menu-item>
                        <a-menu-item key="remind" :disabled="isReminding(record.id)">
                          <LoadingOutlined v-if="isReminding(record.id)" />
                          Nhắc trả
                        </a-menu-item>
                      </a-menu>
                    </template>
                  </a-dropdown>
                </template>
              </div>
            </template>
            <span v-else class="muted">Chỉ xem</span>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="isReturnModalVisible" title="Kiểm tra tài sản khi trả" @ok="submitReturnInspection" @cancel="isReturnModalVisible = false" okText="Lưu kiểm tra" cancelText="Hủy" :confirmLoading="returnSubmitting">
      <a-form layout="vertical">
        <a-alert
          type="info"
          show-icon
          message="Kiểm tra theo từng tài sản"
          description="Có thể ghi nhận riêng tình trạng, ghi chú và bồi thường cho từng món trong phiếu."
          style="margin-bottom: 16px"
        />
        <a-card v-for="item in returnForm.items" :key="item.equipmentId" size="small" :title="`${item.equipmentName || 'Tài sản'} — ${item.serial || ''}`" style="margin-bottom: 12px">
          <a-form-item label="Tình trạng sau kiểm tra" required>
            <a-select v-model:value="item.condition">
              <a-select-option :value="STATUS.AVAILABLE">Rảnh</a-select-option>
              <a-select-option :value="STATUS.BROKEN">Hỏng</a-select-option>
            </a-select>
          </a-form-item>
          <a-form-item label="Ghi chú kiểm tra">
            <a-textarea v-model:value="item.note" :rows="2" placeholder="Mô tả lỗi, phụ kiện thiếu..." />
          </a-form-item>
          <a-form-item v-if="statusMatches(item.condition, STATUS.BROKEN)" label="Số tiền bồi thường nếu hết bảo hành">
            <a-input-number v-model:value="item.compensationAmount" style="width: 100%" :min="0" :step="10000" :formatter="value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')" :parser="value => value.replace(/\$\s?|(,*)/g, '')" />
          </a-form-item>
          <a-form-item label="Ảnh/file trước hoặc sau khi nhận trả">
            <a-upload :before-upload="file => selectReturnEvidence(item, file)" :show-upload-list="false" accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx"><a-button size="small">Chọn minh chứng</a-button></a-upload>
            <span v-if="item.returnEvidenceFile" class="muted">{{ item.returnEvidenceFile.name }}</span>
            <a-select v-if="item.returnEvidenceFile" v-model:value="item.returnEvidenceType" style="width: 100%; margin-top: 6px"><a-select-option value="PHOTO_BEFORE">Ảnh trước khi trả</a-select-option><a-select-option value="PHOTO_AFTER">Ảnh sau khi trả</a-select-option><a-select-option value="DOCUMENT">Biên bản</a-select-option><a-select-option value="SIGNATURE">Xác nhận điện tử</a-select-option></a-select>
          </a-form-item>
        </a-card>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="isHandoverModalVisible"
      :footer="null"
      :closable="false"
      :mask-closable="false"
      width="820px"
      wrap-class-name="handover-modal-wrap"
      @cancel="closeHandoverModal"
    >
      <div class="handover-modal-shell">
        <header class="handover-modal-header">
          <div>
            <h3>Lập biên bản bàn giao</h3>
            <p>Ghi nhận tình trạng thực tế trước khi bàn giao tài sản.</p>
          </div>
          <a-tooltip title="Đóng">
            <a-button type="text" aria-label="Đóng modal lập biên bản bàn giao" class="handover-close" @click="closeHandoverModal">
              <template #icon><CloseOutlined /></template>
            </a-button>
          </a-tooltip>
        </header>

        <div class="handover-modal-content">
          <a-alert type="info" show-icon message="Ghi nhận đủ từng tài sản trước khi bàn giao" />

          <div class="handover-meta-grid">
            <div><span>Người mượn</span><strong>{{ currentHandoverRecord?.borrowerName || currentHandoverRecord?.student || '—' }}</strong></div>
            <div><span>Mã phiếu</span><strong>{{ borrowRecordCode }}</strong></div>
            <div><span>Ngày bàn giao</span><strong>{{ formatDateTime(handoverAt) }}</strong></div>
            <div><span>Dự kiến trả</span><strong>{{ formatDate(currentHandoverRecord?.returnDate) }}</strong></div>
          </div>

          <div class="handover-progress-row">
            <strong>Tài sản đã nhập đủ</strong>
            <span>{{ completedHandoverItems }}/{{ handoverForm.items.length }}</span>
          </div>

          <a-form layout="vertical">
            <section v-for="(item, index) in handoverForm.items" :key="item.equipmentId" class="handover-asset-card">
            <div class="handover-asset-heading">
              <div class="handover-asset-index">{{ index + 1 }}</div>
              <div>
                <h4>{{ item.equipmentName || 'Tài sản' }}</h4>
                <div class="handover-asset-identifiers">
                  <span>Mã tài sản: <strong>{{ item.assetCode || '—' }}</strong></span>
                  <span>Serial: <strong>{{ item.serial || '—' }}</strong></span>
                </div>
              </div>
            </div>
            <a-row :gutter="[16, 16]">
              <a-col :xs="24" :md="12">
                <a-form-item label="Tình trạng" required :validate-status="item.condition ? '' : 'error'">
                  <a-select v-model:value="item.condition" placeholder="Chọn tình trạng">
                    <a-select-option :value="HANDOVER_CONDITIONS.GOOD">Tốt</a-select-option>
                    <a-select-option :value="HANDOVER_CONDITIONS.SCRATCHED">Trầy xước</a-select-option>
                    <a-select-option :value="HANDOVER_CONDITIONS.MISSING_ACCESSORIES">Thiếu phụ kiện</a-select-option>
                    <a-select-option :value="HANDOVER_CONDITIONS.BROKEN">Hỏng</a-select-option>
                  </a-select>
                </a-form-item>
              </a-col>
              <a-col :xs="24" :md="12">
                <a-form-item label="Phụ kiện bàn giao">
                  <a-input v-model:value="item.accessories" placeholder="Ví dụ: nguồn, cáp USB, hộp..." />
                </a-form-item>
              </a-col>
              <a-col :span="24">
                <a-form-item label="Ghi chú">
                  <a-textarea v-model:value="item.note" :rows="2" placeholder="Mô tả tình trạng hoặc lưu ý khi bàn giao..." />
                </a-form-item>
              </a-col>
            </a-row>
          </section>

          <section class="handover-evidence-section">
            <h4>Thông tin bổ sung</h4>
            <a-form-item label="Ghi chú chung">
              <a-textarea v-model:value="handoverForm.notes" :rows="3" placeholder="Ghi chú áp dụng cho toàn bộ biên bản..." />
            </a-form-item>
            <a-form-item label="File/ảnh minh chứng">
              <a-upload-dragger
                :before-upload="selectHandoverEvidence"
                :show-upload-list="false"
                accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx"
                :disabled="handoverSubmitting"
              >
                <p class="ant-upload-drag-icon"><InboxOutlined /></p>
                <p class="ant-upload-text">Kéo thả file vào đây hoặc bấm để chọn</p>
                <p class="ant-upload-hint">PDF, Word, JPG, PNG, WEBP · tối đa 10 MB</p>
              </a-upload-dragger>
              <div v-if="handoverEvidenceFile" class="handover-file-preview">
                <a-image v-if="handoverEvidencePreviewUrl" :src="handoverEvidencePreviewUrl" :width="48" :height="48" />
                <FileOutlined v-else class="handover-file-icon" />
                <div class="handover-file-copy"><strong>{{ handoverEvidenceFile.name }}</strong><span>{{ formatFileSize(handoverEvidenceFile.size) }}</span></div>
                <a-tooltip title="Xóa file minh chứng">
                  <a-button type="text" danger aria-label="Xóa file minh chứng" @click="clearHandoverEvidence">
                    <template #icon><DeleteOutlined /></template>
                  </a-button>
                </a-tooltip>
              </div>
            </a-form-item>
            <a-form-item v-if="handoverEvidenceFile" label="Loại minh chứng">
              <a-select v-model:value="handoverEvidenceType"><a-select-option value="PHOTO">Ảnh</a-select-option><a-select-option value="DOCUMENT">Tài liệu</a-select-option><a-select-option value="SIGNATURE">Xác nhận điện tử</a-select-option></a-select>
            </a-form-item>
            </section>
          </a-form>
        </div>

        <footer class="handover-modal-footer">
          <a-button :disabled="handoverSubmitting" @click="closeHandoverModal">Hủy</a-button>
          <a-button type="primary" :loading="handoverSubmitting" :disabled="handoverSubmitting" @click="submitHandover">Lưu biên bản</a-button>
        </footer>
      </div>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onBeforeUnmount, onMounted, computed } from 'vue'
import { message, Upload } from 'ant-design-vue'
import { CloseOutlined, DeleteOutlined, FileOutlined, InboxOutlined, LoadingOutlined, MoreOutlined } from '@ant-design/icons-vue'
import { borrowApi } from '../api/borrowApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import { HANDOVER_CONDITIONS, STATUS, isManagerRole, statusMatches } from '../constants/business'
import { handoverApi } from '../api/handoverApi'
import { getApiErrorMessage, getApiSuccessMessage } from '../utils/apiError'
import { createTablePagination } from '../utils/tablePagination'

const tablePagination = createTablePagination()

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const returnSubmitting = ref(false)
const isReturnModalVisible = ref(false)
const isHandoverModalVisible = ref(false)
const handoverSubmitting = ref(false)
const currentReturnRecord = ref(null)
const currentHandoverRecord = ref(null)
const handoverForm = ref({ notes: '', items: [] })
const handoverEvidenceFile = ref(null)
const handoverEvidenceType = ref('PHOTO')
const handoverEvidencePreviewUrl = ref('')
const handoverAt = ref(new Date())
const remindingRecordIds = ref(new Set())
const returnForm = ref({
  condition: STATUS.AVAILABLE,
  note: '',
  compensationAmount: 0,
  items: []
})

const columns = [
  { title: 'Người mượn', dataIndex: 'student', key: 'student', fixed: 'left', width: 130 },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', fixed: 'left', width: 160 },
  { title: 'Danh mục', dataIndex: 'category', key: 'category', width: 110 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 130 },
  { title: 'Chi tiết yêu cầu', key: 'details', width: 180 },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', width: 120 },
  { title: 'Dự kiến trả', dataIndex: 'returnDate', key: 'returnDate', width: 120 },
  { title: 'Hạn trả', key: 'dueStatus', align: 'center', width: 130 },
  { title: 'Mục đích', dataIndex: 'purpose', key: 'purpose', width: 180 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center', width: 120 },
  { title: 'Hành động', key: 'action', align: 'center', fixed: 'right', width: 280 }
]

onMounted(() => fetchRequests())

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'
const formatDateTime = (value) => value ? new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'
const formatFileSize = (bytes) => {
  if (!bytes) return '0 B'
  if (bytes < 1024 * 1024) return `${Math.ceil(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
const borrowRecordCode = computed(() => currentHandoverRecord.value?.id ? `BR-${String(currentHandoverRecord.value.id).padStart(6, '0')}` : '—')
const completedHandoverItems = computed(() => handoverForm.value.items.filter(item => Boolean(item.condition)).length)
const isReminding = id => remindingRecordIds.value.has(id)

const fetchRequests = async () => {
  loading.value = true
  try {
    dataSource.value = await borrowApi.getPendingRequests() || []
  } catch {
    message.error('Lỗi khi tải danh sách yêu cầu!')
  } finally {
    loading.value = false
  }
}

const handleApprove = async (record) => {
  try {
    await borrowApi.approve(record.id)
    message.success(`Đã duyệt cho ${record.student} mượn tài sản!`)
    fetchRequests()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Lỗi duyệt yêu cầu!'))
  }
}

const handleReject = async (record) => {
  try {
    await borrowApi.reject(record.id)
    message.warning(`Đã từ chối yêu cầu của ${record.student}.`)
    fetchRequests()
  } catch {
    message.error('Lỗi từ chối yêu cầu!')
  }
}

const handleActionMenuClick = ({ key }, record) => {
  if (key === 'reject') handleReject(record)
  if (key === 'handover') showHandoverModal(record)
  if (key === 'remind') handleRemind(record)
}

const showReturnModal = (record) => {
  currentReturnRecord.value = record
  const details = record.details?.length
    ? record.details
    : [{ equipmentId: record.equipmentId, equipmentName: record.device, serial: record.serial }]
  returnForm.value = {
    condition: STATUS.AVAILABLE,
    note: '',
    compensationAmount: 0,
    items: details.filter(item => !item.returnedAt).map(item => ({
      equipmentId: item.equipmentId,
      equipmentName: item.equipmentName,
      serial: item.serial,
      condition: STATUS.AVAILABLE,
      note: '',
      compensationAmount: 0
      ,returnEvidenceFile: null, returnEvidenceType: 'PHOTO_AFTER'
    }))
  }
  isReturnModalVisible.value = true
}

const submitReturnInspection = async () => {
  returnSubmitting.value = true
  try {
    if (!returnForm.value.items.length) {
      message.warning('Không còn tài sản chưa nhận trả trong phiếu này!')
      return
    }
    for (const item of returnForm.value.items) {
      if (item.returnEvidenceFile) {
        await borrowApi.uploadReturnEvidence(currentReturnRecord.value.id, item.returnEvidenceFile, item.returnEvidenceType, item.equipmentId)
      }
    }
    await borrowApi.returnEquipment(currentReturnRecord.value.id, {
      items: returnForm.value.items.map(item => ({
        equipmentId: item.equipmentId,
        condition: item.condition,
        note: item.note,
        compensationAmount: item.compensationAmount
      }))
    })
    message.success('Đã lưu kết quả kiểm tra và cập nhật trạng thái tài sản!')
    isReturnModalVisible.value = false
    fetchRequests()
  } catch {
    message.error('Lỗi khi lưu kết quả kiểm tra!')
  } finally {
    returnSubmitting.value = false
  }
}

const selectReturnEvidence = (item, file) => {
  const allowed = ['pdf', 'jpg', 'jpeg', 'png', 'webp', 'doc', 'docx']
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!allowed.includes(extension) || file.size > 10 * 1024 * 1024) {
    message.error('Minh chứng phải là PDF, Word hoặc ảnh và không quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  item.returnEvidenceFile = file
  return false
}

const showHandoverModal = record => {
  currentHandoverRecord.value = record
  handoverAt.value = new Date()
  const details = record.details?.length
    ? record.details
    : [{ equipmentId: record.equipmentId, equipmentName: record.device, assetCode: record.assetCode, serial: record.serial }]
  handoverForm.value = {
    notes: '',
    items: details.filter(item => item.equipmentId).map(item => ({
      equipmentId: item.equipmentId,
      equipmentName: item.equipmentName,
      assetCode: item.assetCode || '',
      serial: item.serial || '',
      condition: '',
      accessories: '',
      note: ''
    }))
  }
  handoverEvidenceFile.value = null
  handoverEvidencePreviewUrl.value = ''
  handoverEvidenceType.value = 'PHOTO'
  isHandoverModalVisible.value = true
}

const selectHandoverEvidence = (file) => {
  const allowed = ['pdf', 'jpg', 'jpeg', 'png', 'webp', 'doc', 'docx']
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!allowed.includes(extension)) {
    message.error('Chỉ chấp nhận PDF, Word hoặc ảnh JPG/PNG/WEBP.')
    return Upload.LIST_IGNORE
  }
  if (file.size > 10 * 1024 * 1024) {
    message.error('File minh chứng không được vượt quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  if (handoverEvidencePreviewUrl.value) URL.revokeObjectURL(handoverEvidencePreviewUrl.value)
  handoverEvidenceFile.value = file
  handoverEvidencePreviewUrl.value = file.type?.startsWith('image/') ? URL.createObjectURL(file) : ''
  return false
}

const clearHandoverEvidence = () => {
  if (handoverEvidencePreviewUrl.value) URL.revokeObjectURL(handoverEvidencePreviewUrl.value)
  handoverEvidencePreviewUrl.value = ''
  handoverEvidenceFile.value = null
}

const closeHandoverModal = () => {
  if (handoverSubmitting.value) return
  isHandoverModalVisible.value = false
  clearHandoverEvidence()
}

const submitHandover = async () => {
  if (!handoverForm.value.items.length) {
    message.warning('Phiếu chưa có tài sản để bàn giao.')
    return
  }
  if (handoverForm.value.items.some(item => !item.condition)) {
    message.warning('Vui lòng nhập tình trạng cho tất cả tài sản.')
    return
  }
  handoverSubmitting.value = true
  try {
    await handoverApi.create({
      borrowRecordId: currentHandoverRecord.value.id,
      notes: handoverForm.value.notes,
      items: handoverForm.value.items.map(item => ({
        equipmentId: item.equipmentId,
        condition: item.condition,
        accessories: item.accessories,
        note: item.note
      }))
    })
    if (handoverEvidenceFile.value) {
      await handoverApi.uploadEvidence(
        currentHandoverRecord.value.id,
        handoverEvidenceFile.value,
        handoverEvidenceType.value
      )
    }
    message.success('Đã lập biên bản bàn giao.')
    isHandoverModalVisible.value = false
    clearHandoverEvidence()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể lập biên bản bàn giao!'))
  } finally { handoverSubmitting.value = false }
}

onBeforeUnmount(clearHandoverEvidence)

const handleRemind = async (record) => {
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
</script>

<style scoped>
.borrow-requests-container {
  padding: 0;
}

.request-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  white-space: nowrap;
}

.request-more-button {
  display: inline-flex;
  width: 32px;
  height: 32px;
  align-items: center;
  justify-content: center;
  padding: 0;
  color: var(--color-ink, #111827);
}

.request-more-button:hover,
.request-more-button:focus {
  background: #fff7f3;
  color: var(--color-primary, #d97757);
}

.toolbar {
  margin-bottom: 24px;
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.muted {
  color: #9ca3af;
  font-size: 13px;
}

:deep(.handover-modal-wrap .ant-modal) {
  width: min(820px, calc(100vw - 32px)) !important;
  max-width: calc(100vw - 32px);
  padding-bottom: 0;
}

:deep(.handover-modal-wrap .ant-modal-content) {
  padding: 0;
  overflow: hidden;
  border-radius: 12px;
}

:deep(.handover-modal-wrap .ant-modal-body) { padding: 0; }
.handover-modal-shell { display: flex; flex-direction: column; max-height: calc(100vh - 48px); min-height: 0; }
.handover-modal-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; padding: 22px 26px 16px; border-bottom: 1px solid var(--color-border, #e5e7eb); flex: 0 0 auto; }
.handover-modal-header h3 { margin: 0; color: var(--color-ink, #111827); font-size: 20px; }
.handover-modal-header p { margin: 5px 0 0; color: var(--color-text-secondary, #6b7280); font-size: 13px; }
.handover-close { color: #6b7280; font-size: 18px; }
.handover-modal-content { min-height: 0; overflow-y: auto; padding: 20px 26px 8px; }
.handover-meta-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; margin: 16px 0; }
.handover-meta-grid > div { display: flex; flex-direction: column; gap: 5px; min-width: 0; padding: 12px 13px; background: #fafafa; border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.handover-meta-grid span { color: var(--color-text-secondary, #6b7280); font-size: 12px; }
.handover-meta-grid strong { overflow: hidden; color: var(--color-ink, #111827); font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.handover-progress-row { display: flex; justify-content: space-between; margin: 18px 0 10px; color: var(--color-ink, #111827); font-size: 13px; }
.handover-progress-row span { color: var(--color-primary); font-weight: 600; }
.handover-asset-card { margin-bottom: 14px; padding: 16px 16px 2px; border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(17, 24, 39, .04); }
.handover-asset-heading { display: flex; align-items: flex-start; gap: 11px; margin-bottom: 15px; }
.handover-asset-index { display: grid; width: 28px; height: 28px; flex: 0 0 28px; place-items: center; border-radius: 50%; background: var(--color-primary); color: #fff; font-weight: 600; }
.handover-asset-heading h4 { margin: 2px 0 5px; color: var(--color-ink, #111827); font-size: 15px; }
.handover-asset-identifiers { display: flex; flex-wrap: wrap; gap: 5px 18px; color: var(--color-text-secondary, #6b7280); font-size: 12px; }
.handover-asset-identifiers strong { color: var(--color-ink, #111827); font-weight: 500; }
.handover-asset-card :deep(.ant-form-item), .handover-evidence-section :deep(.ant-form-item) { margin-bottom: 14px; }
.handover-evidence-section { margin: 22px 0 8px; padding-top: 18px; border-top: 1px solid var(--color-border, #e5e7eb); }
.handover-evidence-section h4 { margin: 0 0 14px; color: var(--color-ink, #111827); font-size: 16px; }
.handover-evidence-section :deep(.ant-upload-drag) { padding: 14px 12px; border-color: rgba(217, 119, 87, .45); }
.handover-evidence-section :deep(.ant-upload-drag:hover) { border-color: var(--color-primary); }
.handover-evidence-section :deep(.ant-upload-drag-icon) { margin-bottom: 5px; color: var(--color-primary); font-size: 26px; }
.handover-evidence-section :deep(.ant-upload-text) { margin-bottom: 3px; font-size: 13px; }
.handover-evidence-section :deep(.ant-upload-hint) { color: var(--color-text-secondary, #6b7280); font-size: 12px; }
.handover-file-preview { display: flex; align-items: center; gap: 10px; margin-top: 10px; padding: 8px 10px; border: 1px solid var(--color-border, #e5e7eb); border-radius: 8px; }
.handover-file-preview :deep(.ant-image), .handover-file-icon { width: 48px; height: 48px; flex: 0 0 48px; object-fit: cover; }
.handover-file-icon { padding: 13px; border-radius: 6px; background: #fff3ef; color: var(--color-primary); font-size: 22px; }
.handover-file-copy { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 3px; }
.handover-file-copy strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.handover-file-copy span { color: var(--color-text-secondary, #6b7280); font-size: 12px; }
.handover-modal-footer { display: flex; justify-content: flex-end; gap: 10px; padding: 14px 26px 18px; border-top: 1px solid var(--color-border, #e5e7eb); flex: 0 0 auto; }
@media (max-width: 767px) {
  :deep(.handover-modal-wrap .ant-modal) { width: calc(100vw - 12px) !important; max-width: calc(100vw - 12px); margin: 6px auto; }
  .handover-modal-shell { max-height: calc(100vh - 12px); }
  .handover-modal-header, .handover-modal-content, .handover-modal-footer { padding-left: 16px; padding-right: 16px; }
  .handover-meta-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .handover-asset-card { padding: 14px 12px 2px; }
}
</style>



