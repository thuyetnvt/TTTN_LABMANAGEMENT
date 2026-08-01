export default {
  menu: {
    overview: 'Overview',
    devices: 'Devices & Consumables',
    borrowHistory: 'Borrow/Return History',
    teacherApproval: 'Teacher Approval',
    maintenanceHistory: 'Maintenance History',
    penalty: 'Penalty Management',
    userManagement: 'User Management',
    auditLogs: 'Activity Logs',
    borrowRequests: 'Borrow Requests',
    consumableRequests: 'Consumable Requests',
    studentBorrowHistory: 'Device Borrow History',
    studentConsumableHistory: 'Consumable Request History',
    logout: 'Logout'
  },
  header: {
    search: 'Search',
    workspace: 'Central Lab'
  },
  search_modal: {
    placeholder: 'Search devices by name or serial number',
    scope: 'Devices',
    title: 'Search devices',
    notFound: 'No matching devices found',
    hint: 'Enter a device name, serial number, or asset code to search inventory quickly.',
    example: 'Examples',
    move: 'Move',
    select: 'Select',
    close: 'Close'
  },
  common: {
    add: 'Add',
    edit: 'Edit',
    delete: 'Delete',
    save: 'Save',
    cancel: 'Cancel',
    detail: 'Detail',
    view: 'View',
    actions: 'Actions',
    confirm: 'Confirm',
    close: 'Close'
  },
  device: {
    name: 'Device Name',
    model: 'Model',
    serial: 'Serial Number',
    location: 'Location',
    status: 'Status',
    qrScan: 'Scan QR',
    exportExcel: 'Export to Excel',
    borrow: 'Borrow',
    addDevice: 'Add Device',
    editDevice: 'Edit Device',
    deviceList: 'Device List',
    qrcode: 'QR Code',
    scanToBorrow: 'Scan to Borrow',
    statusReady: 'Ready',
    statusBorrowing: 'Borrowing',
    statusMaintenance: 'Maintenance'
  },
  borrow: {
    requestBorrow: 'Request Borrow',
    expectedReturnDate: 'Expected Return Date',
    teacherGuarantee: 'Teacher Guarantee',
    optional: 'Optional',
    teacherGuaranteeHint: 'Hint: Enter teacher email if required',
    purpose: 'Purpose',
    purposePlaceholder: 'Enter purpose of borrowing',
    sendRequest: 'Send Request',
    selectPlaceholder: 'Select an option'
  },
  message: {
    fillAllInfo: 'Please fill in all required info',
    addSuccess: 'Added successfully',
    updateSuccess: 'Updated successfully',
    saveError: 'Failed to save',
    deleteSuccess: 'Deleted successfully',
    borrowSuccess: 'Borrowed successfully',
    loadTeacherError: 'Failed to load teachers',
    loadDeviceError: 'Failed to load devices',
    invalidQR: 'Invalid QR Code',
    deviceNotFound: 'Device not found'
  },
  consumable: {
    addConsumable: 'Add Consumable',
    consumableName: 'Consumable Name',
    unit: 'Unit',
    currentQuantity: 'Current Quantity',
    minQuantity: 'Min Quantity',
    status: 'Status',
    requestSupply: 'Request Supply',
    editQuantity: 'Edit Quantity',
    consumablePlaceholder: 'Enter consumable name',
    unitPlaceholder: 'Enter unit'
  }
}
