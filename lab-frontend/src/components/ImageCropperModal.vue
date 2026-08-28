<template>
  <a-modal
    :open="open"
    title="Chỉnh sửa ảnh đại diện"
    :confirm-loading="saving"
    ok-text="Lưu ảnh"
    cancel-text="Hủy"
    :width="600"
    :maskClosable="false"
    destroyOnClose
    @ok="handleOk"
    @cancel="handleCancel"
  >
    <div class="cropper-container">
      <img ref="imageRef" :src="imageUrl" alt="Nguồn ảnh" class="cropper-image" />
    </div>
    
    <div class="cropper-controls">
      <a-space>
        <a-button type="dashed" @click="rotateLeft" title="Xoay trái">
          <template #icon><RotateLeftOutlined /></template>
        </a-button>
        <a-button type="dashed" @click="rotateRight" title="Xoay phải">
          <template #icon><RotateRightOutlined /></template>
        </a-button>
        <a-button type="dashed" @click="resetCropper" title="Đặt lại">
          <template #icon><UndoOutlined /></template>
        </a-button>
      </a-space>
    </div>
  </a-modal>
</template>

<script setup>
import { ref, watch, nextTick, onBeforeUnmount } from 'vue'
import Cropper from 'cropperjs'
import 'cropperjs/dist/cropper.css'
import { RotateLeftOutlined, RotateRightOutlined, UndoOutlined } from '@ant-design/icons-vue'

const props = defineProps({
  open: { type: Boolean, required: true },
  imageUrl: { type: String, required: true },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:open', 'crop'])

const imageRef = ref(null)
let cropper = null

const initCropper = () => {
  if (cropper) cropper.destroy()
  
  if (imageRef.value) {
    cropper = new Cropper(imageRef.value, {
      aspectRatio: 1, // Hình vuông cho ảnh đại diện
      viewMode: 1, // Không cho phép khung cắt lọt ra ngoài ảnh
      dragMode: 'move', // Chế độ di chuyển ảnh thay vì vẽ khung mới
      autoCropArea: 0.8,
      restore: false,
      guides: true,
      center: true,
      highlight: false,
      cropBoxMovable: true,
      cropBoxResizable: true,
      toggleDragModeOnDblclick: false
    })
  }
}

watch(() => props.open, (isOpen) => {
  if (isOpen) {
    nextTick(() => {
      // Đợi ảnh render xong thì mới khởi tạo cropper
      setTimeout(initCropper, 100)
    })
  } else {
    if (cropper) {
      cropper.destroy()
      cropper = null
    }
  }
})

const rotateLeft = () => cropper?.rotate(-90)
const rotateRight = () => cropper?.rotate(90)
const resetCropper = () => cropper?.reset()

const handleOk = () => {
  if (!cropper) return
  
  // Lấy dữ liệu dạng canvas và chuyển sang Blob
  const canvas = cropper.getCroppedCanvas({
    width: 512,
    height: 512,
    fillColor: '#fff',
    imageSmoothingEnabled: true,
    imageSmoothingQuality: 'high'
  })
  
  canvas.toBlob((blob) => {
    if (blob) {
      emit('crop', blob)
    }
  }, 'image/jpeg', 0.9)
}

const handleCancel = () => {
  emit('update:open', false)
}

onBeforeUnmount(() => {
  if (cropper) cropper.destroy()
})
</script>

<style scoped>
.cropper-container {
  width: 100%;
  height: 400px;
  background-color: #f0f0f0;
  display: flex;
  justify-content: center;
  align-items: center;
  overflow: hidden;
  border-radius: 8px;
}

.cropper-image {
  display: block;
  max-width: 100%; /* Cần thiết cho cropperjs */
}

.cropper-controls {
  display: flex;
  justify-content: center;
  margin-top: 16px;
}
</style>
