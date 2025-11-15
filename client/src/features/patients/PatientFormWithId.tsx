import { useParams } from 'react-router-dom'
import PatientForm from './PatientForm'

export default function PatientFormWithId() {
  const { id } = useParams<{ id: string }>()
  return <PatientForm id={id ? Number(id) : undefined} />
}

