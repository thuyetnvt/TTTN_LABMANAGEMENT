<template>
  <div class="qr-scanner-wrapper">
    <div v-if="loadingCameras" class="loading-state">
      <a-spin tip="Đang tìm kiếm máy ảnh..." />
    </div>

    <div v-else-if="cameraError" class="error-state">
      <a-alert type="error" show-icon>
        <template #message>Không thể truy cập máy ảnh</template>
        <template #description>{{ cameraError }}</template>
      </a-alert>
      <a-button class="retry-btn" type="primary" @click="initCameras">
        Thử lại
      </a-button>
    </div>

    <div v-else class="scanner-container">
      <div class="controls-row">
        <a-select
          v-model:value="selectedCameraId"
          :options="cameraOptions"
          class="camera-select"
          placeholder="Chọn máy ảnh"
          :disabled="isScanning"
        />
        <a-button
          :type="isScanning ? 'default' : 'primary'"
          :danger="isScanning"
          @click="toggleScanning"
          class="scan-btn"
        >
          <template #icon>
            <component :is="isScanning ? 'StopOutlined' : 'ScanOutlined'" />
          </template>
          {{ isScanning ? 'Dừng quét' : 'Bắt đầu quét' }}
        </a-button>
      </div>

      <div class="reader-container" :class="{ 'is-scanning': isScanning }">
        <div id="html5-qrcode-reader" class="qr-reader-div"></div>
        <div v-if="!isScanning" class="reader-overlay">
          <ScanOutlined class="overlay-icon" />
          <p>Nhấn "Bắt đầu quét" để bật máy ảnh</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { Html5Qrcode } from 'html5-qrcode'
import { StopOutlined, ScanOutlined } from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'

const props = defineProps({
  fps: {
    type: Number,
    default: 10
  },
  qrbox: {
    type: Number,
    default: 250
  }
})

const emit = defineEmits(['scan-success', 'scan-error'])

const loadingCameras = ref(true)
const cameraError = ref('')
const cameras = ref([])
const selectedCameraId = ref(null)
const isScanning = ref(false)
let html5QrCode = null

const cameraOptions = computed(() => {
  return cameras.value.map(cam => ({
    label: cam.label || `Máy ảnh ${cam.id.substring(0, 5)}...`,
    value: cam.id
  }))
})

const initCameras = async () => {
  loadingCameras.value = true
  cameraError.value = ''
  try {
    const devices = await Html5Qrcode.getCameras()
    if (devices && devices.length > 0) {
      cameras.value = devices
      // Prefer back camera if available, otherwise first camera
      const backCamera = devices.find(d => d.label.toLowerCase().includes('back') || d.label.toLowerCase().includes('sau'))
      selectedCameraId.value = backCamera ? backCamera.id : devices[0].id
    } else {
      cameraError.value = 'Không tìm thấy máy ảnh nào trên thiết bị của bạn.'
    }
  } catch (err) {
    cameraError.value = 'Vui lòng cấp quyền truy cập máy ảnh cho trình duyệt để sử dụng tính năng này.'
    console.error('Error getting cameras', err)
  } finally {
    loadingCameras.value = false
  }
}

const startScanning = async () => {
  if (!selectedCameraId.value) {
    message.warning('Vui lòng chọn một máy ảnh trước khi quét.')
    return
  }
  
  if (!html5QrCode) {
    html5QrCode = new Html5Qrcode('html5-qrcode-reader')
  }

  try {
    await html5QrCode.start(
      selectedCameraId.value,
      {
        fps: props.fps,
        qrbox: { width: props.qrbox, height: props.qrbox },
        aspectRatio: 1.0
      },
      (decodedText) => {
        // Stop scanning after successful scan to prevent multiple fires
        stopScanning()
        emit('scan-success', decodedText)
      },
      (errorMessage) => {
        // Only emit scan-error if needed, it triggers constantly on empty frames
        // emit('scan-error', errorMessage)
      }
    )
    isScanning.value = true
  } catch (err) {
    message.error('Không thể bật máy ảnh: ' + (err?.message || err))
    isScanning.value = false
  }
}

const stopScanning = async () => {
  if (html5QrCode && isScanning.value) {
    try {
      await html5QrCode.stop()
      isScanning.value = false
    } catch (err) {
      console.error('Failed to stop scanning', err)
    }
  }
}

const toggleScanning = () => {
  if (isScanning.value) {
    stopScanning()
  } else {
    startScanning()
  }
}

// Expose a method to stop scanner from parent if modal closes
defineExpose({
  stopScanning
})

onMounted(() => {
  initCameras()
})

onUnmounted(() => {
  if (isScanning.value) {
    stopScanning().then(() => {
      html5QrCode?.clear()
    })
  } else {
    html5QrCode?.clear()
  }
})
</script>

<style scoped>
.qr-scanner-wrapper {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.loading-state, .error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  background: #f5f5f5;
  border-radius: 8px;
  gap: 16px;
}

.controls-row {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}

.camera-select {
  flex: 1;
}

.scan-btn {
  min-width: 140px;
}

.reader-container {
  position: relative;
  width: 100%;
  background: #000;
  border-radius: 8px;
  overflow: hidden;
  min-height: 250px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.reader-container.is-scanning {
  background: transparent;
}

.qr-reader-div {
  width: 100%;
}

.reader-overlay {
  position: absolute;
  top: 0; left: 0; right: 0; bottom: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: rgba(255, 255, 255, 0.7);
  background: rgba(0, 0, 0, 0.8);
  z-index: 10;
  gap: 12px;
}

.overlay-icon {
  font-size: 48px;
  opacity: 0.5;
}

.reader-overlay p {
  margin: 0;
  font-size: 14px;
}

/* Override html5-qrcode styles */
:deep(#html5-qrcode-reader) {
  border: none !important;
}
:deep(#html5-qrcode-reader video) {
  border-radius: 8px;
  object-fit: cover;
}
</style>
