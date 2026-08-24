import test from 'node:test'
import assert from 'node:assert/strict'
import { ROLE_LABELS, STATUS, normalizeStatus, roleLabel, statusLabel, statusMatches } from '../src/constants/business.js'

test('ánh xạ vai trò và trạng thái sang tiếng Việt', () => {
  assert.equal(roleLabel('Admin'), 'Quản trị viên')
  assert.equal(statusLabel(STATUS.BORROWED), 'Đang mượn')
  assert.equal(statusLabel('Hoàn tất'), 'Đã hoàn thành bảo trì')
  assert.equal(ROLE_LABELS.STUDENT, 'Sinh viên')
})

test('chuẩn hóa trạng thái cũ nhưng giữ mã ổn định', () => {
  assert.equal(normalizeStatus('Rảnh'), STATUS.AVAILABLE)
  assert.equal(normalizeStatus('Đang mượn'), STATUS.BORROWED)
  assert.equal(statusMatches('Đang mượn', STATUS.BORROWED), true)
  assert.equal(statusMatches(STATUS.BROKEN, STATUS.AVAILABLE), false)
})
