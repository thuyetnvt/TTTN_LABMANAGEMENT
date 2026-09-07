<template>
  <a-modal
    :open="open"
    title="Kiểm tra tài sản khi trả"
    ok-text="Lưu kiểm tra"
    cancel-text="Hủy"
    :confirm-loading="returnSubmitting"
    @update:open="value => emit('update:open', value)"
    @ok="submitReturnInspection"
    @cancel="close"
  >
    <a-form layout="vertical">
      <a-alert
        type="info"
        show-icon
        message="Kiểm tra theo từng tài sản"
        description="Có thể ghi nhận riêng tình trạng, ghi chú và minh chứng cho từng món trong phiếu."
        style="margin-bottom: 16px"
      />
      <a-alert
        v-if="record?.isOverdue"
        type="warning"
        show-icon
        message="Phiếu mượn đã quá hạn"
        :description="`Đã quá hạn ${overdueDays(record)} ngày. Vui lòng ghi nhận tình trạng từng tài sản khi trả.`"
        style="margin-bottom: 16px"
      />
      <a-card
        v-for="item in returnForm.items"
        :key="item.equipmentId"
        size="small"
        :title="`${item.equipmentName || 'Tài sản'} — ${item.serial || ''}`"
        style="margin-bottom: 12px"
      >
        <a-form-item label="Tình trạng sau kiểm tra" required>
          <a-select v-model:value="item.condition">
            <a-select-option :value="STATUS.AVAILABLE">Rảnh</a-select-option>
            <a-select-option :value="STATUS.BROKEN">Hỏng</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Ghi chú kiểm tra">
          <a-textarea v-model:value="item.note" :rows="2" placeholder="Mô tả lỗi, phụ kiện thiếu..." />
        </a-form-item>
        <a-form-item label="Ảnh/file trước hoặc sau khi nhận trả">
          <a-upload
            :before-upload="file => selectReturnEvidence(item, file)"
            :show-upload-list="false"
            accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx"
          >
            <a-button size="small">Chọn minh chứng</a-button>
          </a-upload>
          <span v-if="item.returnEvidenceFile" class="muted">{{ item.returnEvidenceFile.name }}</span>
          <a-select
            v-if="item.returnEvidenceFile"
            v-model:value="item.returnEvidenceType"
            style="width: 100%; margin-top: 6px"
          >
            <a-select-option value="PHOTO_BEFORE">Ảnh trước khi trả</a-select-option>
            <a-select-option value="PHOTO_AFTER">Ảnh sau khi trả</a-select-option>
            <a-select-option value="DOCUMENT">Biên bản</a-select-option>
            <a-select-option value="SIGNATURE">Xác nhận điện tử</a-select-option>
          </a-select>
        </a-form-item>
      </a-card>
    </a-form>
  </a-modal>
</template>

<script setup>
import { onBeforeUnmount, ref, watch } from 'vue'
import { message, Upload } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import { STATUS } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'

const props = defineProps({
  open: Boolean,
  record: { type: Object, default: null }
})

const emit = defineEmits(['update:open', 'saved'])
const returnSubmitting = ref(false)
const returnForm = ref({
  condition: STATUS.AVAILABLE,
  note: '',
  items: []
})

const initialize = record => {
  if (!record) {
    returnForm.value = { condition: STATUS.AVAILABLE, note: '', items: [] }
    return
  }

  const details = record.details?.length
    ? record.details
    : [{ equipmentId: record.equipmentId, equipmentName: record.device, serial: record.serial }]

  returnForm.value = {
    condition: STATUS.AVAILABLE,
    note: '',
    items: details.filter(item => !item.returnedAt && item.equipmentId).map(item => ({
      equipmentId: item.equipmentId,
      equipmentName: item.equipmentName,
      serial: item.serial,
      condition: STATUS.AVAILABLE,
      note: '',
      returnEvidenceFile: null,
      returnEvidenceType: 'PHOTO_AFTER'
    }))
  }
}

watch(() => [props.open, props.record], ([open, record]) => {
  if (open) initialize(record)
}, { immediate: true })

const close = () => emit('update:open', false)

const overdueDays = record => Math.max(1, Math.abs(Number(record?.daysUntilDue || 0)))

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

const submitReturnInspection = async () => {
  if (!props.record) return

  returnSubmitting.value = true
  try {
    if (!returnForm.value.items.length) {
      message.warning('Không còn tài sản chưa nhận trả trong phiếu này!')
      return
    }
    for (const item of returnForm.value.items) {
      if (item.returnEvidenceFile) {
        await borrowApi.uploadReturnEvidence(props.record.id, item.returnEvidenceFile, item.returnEvidenceType, item.equipmentId)
      }
    }
    await borrowApi.returnEquipment(props.record.id, {
      items: returnForm.value.items.map(item => ({
        equipmentId: item.equipmentId,
        condition: item.condition,
        note: item.note
      }))
    })
    message.success('Đã lưu kết quả kiểm tra và cập nhật trạng thái tài sản!')
    emit('update:open', false)
    emit('saved')
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Lỗi khi lưu kết quả kiểm tra!'))
  } finally {
    returnSubmitting.value = false
  }
}

onBeforeUnmount(() => {
  returnForm.value.items = []
})
</script>

<style scoped>
.muted {
  display: block;
  margin-top: 6px;
  color: #8c8c8c;
  font-size: 13px;
}
</style>
