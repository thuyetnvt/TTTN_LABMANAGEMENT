<template>
  <a-avatar
    :size="size"
    :shape="shape"
    :src="resolvedSrc"
    :alt="alt || name"
    :class="['user-avatar', { 'user-avatar-loading': loading || avatarLoading }, $attrs.class]"
    @error="handleImageError"
  >
    <template v-if="loading || avatarLoading">
      <LoadingOutlined />
    </template>
    <template v-else>{{ initials }}</template>
  </a-avatar>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { LoadingOutlined } from '@ant-design/icons-vue'
import axiosClient, { apiBaseUrl } from '../api/axiosClient'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  name: { type: String, default: 'Tài khoản' },
  avatarUrl: { type: String, default: '' },
  avatarUpdatedAt: { type: [String, Date], default: '' },
  size: { type: [Number, String], default: 38 },
  shape: { type: String, default: 'circle' },
  alt: { type: String, default: '' },
  loading: { type: Boolean, default: false },
})

const imageFailed = ref(false)
const avatarBlobUrl = ref('')
const avatarLoading = ref(false)
let loadSequence = 0

const initials = computed(() => props.name.trim().charAt(0).toUpperCase() || 'T')
const resolvedSrc = computed(() => {
  if (!props.avatarUrl || imageFailed.value) return undefined
  // Local previews already belong to the profile view and must not be fetched
  // through Axios or revoked by this shared component.
  if (props.avatarUrl.startsWith('blob:')) return props.avatarUrl
  return avatarBlobUrl.value || undefined
})

const handleImageError = () => { imageFailed.value = true }

const revokeBlobUrl = () => {
  if (avatarBlobUrl.value) URL.revokeObjectURL(avatarBlobUrl.value)
  avatarBlobUrl.value = ''
}

const loadAvatar = async () => {
  const sequence = ++loadSequence
  imageFailed.value = false
  revokeBlobUrl()
  if (!props.avatarUrl || props.avatarUrl.startsWith('blob:')) return

  // axiosClient already prefixes relative requests with baseURL (/api). The
  // shared user URL also contains that prefix, so remove it before calling
  // Axios to avoid requesting /api/api/users/... .
  const apiRoot = String(apiBaseUrl || '').replace(/\/$/, '')
  const requestPath = apiRoot && props.avatarUrl.startsWith(`${apiRoot}/`)
    ? props.avatarUrl.slice(apiRoot.length)
    : props.avatarUrl
  const separator = requestPath.includes('?') ? '&' : '?'
  const version = props.avatarUpdatedAt ? encodeURIComponent(String(props.avatarUpdatedAt)) : ''
  const requestUrl = version ? `${requestPath}${separator}v=${version}` : requestPath
  avatarLoading.value = true
  try {
    const blob = await axiosClient.get(requestUrl, {
      responseType: 'blob',
      // A missing avatar is a valid fallback state. Keep authentication and
      // server errors visible, but do not log a noisy 404 for initials.
      validateStatus: status => status === 200 || status === 404
    })
    if (sequence !== loadSequence || !blob || blob.size === 0) {
      imageFailed.value = true
      return
    }
    avatarBlobUrl.value = URL.createObjectURL(blob)
  } catch {
    if (sequence === loadSequence) imageFailed.value = true
  } finally {
    if (sequence === loadSequence) avatarLoading.value = false
  }
}

watch(() => [props.avatarUrl, props.avatarUpdatedAt], loadAvatar, { immediate: true })
onBeforeUnmount(() => { loadSequence += 1; revokeBlobUrl() })
</script>

<style scoped>
.user-avatar { flex: 0 0 auto; background: var(--color-primary); color: var(--color-on-dark, #fff); }
.user-avatar-loading { opacity: .72; }
</style>
