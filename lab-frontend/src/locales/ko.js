export default {
  menu: {
    overview: '개요',
    devices: '장비 및 자재',
    borrowHistory: '대여/반납 내역',
    teacherApproval: '교수 승인',
    maintenanceHistory: '유지보수 내역',
    penalty: '배상 관리',
    userManagement: '사용자 관리',
    borrowRequests: '대여 승인',
    consumableRequests: '소모품 승인',
    studentBorrowHistory: '장비 대여 내역',
    studentConsumableHistory: '소모품 내역',
    logout: '로그아웃'
  },
  header: {
    search: '검색',
    workspace: '중앙 실험실'
  },
  common: {
    add: '추가',
    edit: '편집',
    delete: '삭제',
    save: '저장',
    cancel: '취소',
    detail: '상세',
    view: '보기',
    actions: '작업',
    confirm: '확인',
    close: '닫기'
  },
  device: {
    name: '장비 이름',
    model: '모델',
    serial: '일련번호',
    location: '위치',
    status: '상태',
    qrScan: 'QR 스캔',
    exportExcel: 'Excel 내보내기',
    borrow: '대여',
    addDevice: '장비 추가',
    editDevice: '장비 편집',
    deviceList: '장비 목록',
    qrcode: 'QR 코드',
    scanToBorrow: '스캔하여 대여',
    statusReady: '대기 중',
    statusBorrowing: '대여 중',
    statusMaintenance: '유지보수 중'
  },
  borrow: {
    requestBorrow: '대여 요청',
    expectedReturnDate: '반납 예정일',
    teacherGuarantee: '담당 교수',
    optional: '선택',
    teacherGuaranteeHint: '힌트: 필요한 경우 교수 이메일을 입력하세요',
    purpose: '목적',
    purposePlaceholder: '대여 목적을 입력하세요',
    sendRequest: '요청 보내기',
    selectPlaceholder: '선택하세요'
  },
  message: {
    fillAllInfo: '모든 정보를 입력하세요',
    addSuccess: '성공적으로 추가되었습니다',
    updateSuccess: '성공적으로 업데이트되었습니다',
    saveError: '저장 실패',
    deleteSuccess: '성공적으로 삭제되었습니다',
    borrowSuccess: '성공적으로 대여되었습니다',
    loadTeacherError: '교수 목록 불러오기 실패',
    loadDeviceError: '장비 목록 불러오기 실패',
    invalidQR: '잘못된 QR 코드',
    deviceNotFound: '장비를 찾을 수 없습니다'
  },
  consumable: {
    addConsumable: '소모품 추가',
    consumableName: '소모품 이름',
    unit: '단위',
    currentQuantity: '현재 수량',
    minQuantity: '최소 수량',
    status: '상태',
    requestSupply: '지급 요청',
    editQuantity: '수량 편집',
    consumablePlaceholder: '소모품 이름을 입력하세요',
    unitPlaceholder: '단위를 입력하세요'
  }
}
