/** Enum values mirror the API's enum names; labels live in id.json under enums.*. */
export type Gender = 'Male' | 'Female';
export type MaritalStatus = 'Single' | 'Married' | 'Divorced' | 'Widowed';
export type EducationLevel =
  | 'ElementarySchool'
  | 'JuniorHighSchool'
  | 'SeniorHighSchool'
  | 'Diploma'
  | 'Bachelor'
  | 'Master'
  | 'Doctorate';

export const GENDERS: Gender[] = ['Male', 'Female'];
export const MARITAL_STATUSES: MaritalStatus[] = ['Single', 'Married', 'Divorced', 'Widowed'];
export const EDUCATION_LEVELS: EducationLevel[] = [
  'ElementarySchool', 'JuniorHighSchool', 'SeniorHighSchool',
  'Diploma', 'Bachelor', 'Master', 'Doctorate',
];

export interface PatientProfile {
  fullName: string;
  birthPlace: string | null;
  birthDate: string | null; // 'YYYY-MM-DD'
  gender: Gender | null;
  domicileAddress: string | null;
  maritalStatus: MaritalStatus | null;
  lastEducation: EducationLevel | null;
  occupation: string | null;
  hasAccessedPsychologyServices: boolean | null;
  hasPriorDiagnosis: boolean | null;
  priorDiagnosis: string | null;
  consultationConcerns: string | null;
  counselingExpectations: string | null;
  isComplete: boolean;
  updatedAtUtc: string;
}

export interface AssignedPsychologist {
  psychologistId: string;
  displayName: string;
  title: string | null;
  specialization: string | null;
  slug: string | null;
  photoUrl: string | null;
  assignedAtUtc: string;
}

export interface PsychologistPatientListItem {
  patientUserId: string;
  fullName: string;
  whatsAppNumber: string | null;
  assignedAtUtc: string;
  profileCompleted: boolean;
}

export interface PsychologistPatientDetail {
  patientUserId: string;
  accountName: string;
  email: string;
  whatsAppNumber: string | null;
  assignedAtUtc: string;
  profile: PatientProfile | null;
}

export interface AdminPatientListItem {
  userId: string;
  fullName: string;
  email: string;
  whatsAppNumber: string | null;
  registeredAtUtc: string;
  profileCompleted: boolean;
  assignments: { psychologistId: string; displayName: string }[];
}

export interface PagedPatients {
  items: AdminPatientListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
