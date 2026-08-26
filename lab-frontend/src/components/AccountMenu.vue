<template>
  <a-dropdown :placement="placement" trigger="click">
    <a-tooltip title="Mở menu tài khoản">
      <div :data-testid="testId" class="account-menu-trigger" aria-label="Mở menu tài khoản">
        <slot name="trigger">
          <UserAvatar
            :name="displayName"
            :avatar-url="avatarUrl"
            :avatar-updated-at="avatarUpdatedAt"
            :size="38"
          />
        </slot>
      </div>
    </a-tooltip>
    <template #overlay>
      <a-menu class="account-menu" @click="handleMenuClick">
        <div class="account-menu-heading">
          <strong>{{ displayName }}</strong>
          <span>{{ roleText }}</span>
        </div>
        <a-menu-divider />
        <a-menu-item key="profile" :data-testid="`${testId}-profile`">
          <user-outlined />
          Hồ sơ cá nhân
        </a-menu-item>
        <a-menu-item key="password" :data-testid="`${testId}-password`">
          <lock-outlined />
          Đổi mật khẩu
        </a-menu-item>
        <a-menu-divider />
        <a-menu-item key="logout" :data-testid="`${testId}-logout`" class="account-menu-logout">
          <logout-outlined />
          Đăng xuất
        </a-menu-item>
      </a-menu>
    </template>
  </a-dropdown>
</template>

<script setup>
import { computed } from 'vue'
import { LockOutlined, LogoutOutlined, UserOutlined } from '@ant-design/icons-vue'
import { roleLabel } from '../constants/business'
import UserAvatar from './UserAvatar.vue'

const props = defineProps({
  displayName: { type: String, default: 'Tài khoản' },
  role: { type: String, default: 'Guest' },
  avatarUrl: { type: String, default: '' },
  avatarUpdatedAt: { type: [String, Date], default: '' },
  placement: { type: String, default: 'bottomRight' },
  testId: { type: String, default: 'account-menu-trigger' }
})

const emit = defineEmits(['profile', 'password', 'logout'])
const roleText = computed(() => roleLabel(props.role))

const handleMenuClick = ({ key }) => emit(key)
</script>

<style scoped>
.account-menu-trigger { cursor: pointer; display: inline-flex; align-items: center; }
.account-menu { min-width: 220px; }
.account-menu-heading { display: flex; flex-direction: column; gap: 3px; padding: 10px 12px 8px; }
.account-menu-heading strong { color: #111827; }
.account-menu-heading span { color: #6b7280; font-size: 12px; }
.account-menu-logout { color: #dc2626; }
</style>
