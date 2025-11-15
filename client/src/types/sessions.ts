export interface Session {
  id: number
  patient: { firstName: string; lastName: string; email: string }
  startDateTime: string
  endDateTime: string
  statusId: number
  price: number
  googleMeetLink?: string
  payment?: {
    id: number
    statusId: number
    amount: number
    createdAt: string
    completedAt?: string | null
  } | null
  isPaid?: boolean
  canPay?: boolean
}

export interface PatientInformationTypeDto {
  id: number
  code: string
  name: string
  description?: string | null
  displayOrder: number
}

export interface PatientInformationEntryForm {
  id?: number
  patientInformationTypeId: number
  typeName: string
  content: string
  createdAt?: string | null
  updatedAt?: string | null
}

