<template>
  <a-avatar
    :size="size"
    :shape="shape"
    :src="resolvedSrc"
    :alt="alt || name"
    :class="['user-avatar', { 'user-avatar-loading': loading }, $attrs.class]"
    @error="handleImageError"
  >
    <template v-if="loading">
      <LoadingOutlined />
    </template>
    <template v-else>{{ initials }}</template>
  </a-avatar>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { LoadingOutlined } from '@ant-design/icons-vue'

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
const initials = computed(() => props.name.trim().charAt(0).toUpperCase() || 'T')
const resolvedSrc = computed(() => {
  if (!props.avatarUrl || imageFailed.value) return undefined
  const separator = props.avatarUrl.includes('?') ? '&' : '?'
  const version = props.avatarUpdatedAt ? encodeURIComponent(String(props.avatarUpdatedAt)) : ''
  return version ? `${props.avatarUrl}${separator}v=${version}` : props.avatarUrl
})

const handleImageError = () => { imageFailed.value = true }

watch(() => [props.avatarUrl, props.avatarUpdatedAt], () => { imageFailed.value = false })
</script>

<style scoped>
.user-avatar { flex: 0 0 auto; background: var(--color-primary); color: var(--color-on-dark, #fff); }
.user-avatar-loading { opacity: .72; }
</style>
