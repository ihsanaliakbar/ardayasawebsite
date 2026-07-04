/**
 * Clinic contact facts used across the public site. Values sourced from the
 * clinic's Instagram/FAQ (2026-07). Presentation strings live in i18n files;
 * these are data, not copy.
 */
export const CLINIC = {
  whatsAppNumber: '+62 851-2130-5115',
  whatsAppUrl:
    'https://api.whatsapp.com/send/?phone=6285121305115&text&type=phone_number&app_absent=0',
  instagramUrl: 'https://www.instagram.com/ar.da.ya.sa',
  address: 'Bukit Cimanggu City Blok N3/4, Bogor, Jawa Barat',
  mapsUrl: 'https://maps.google.com/?q=Ardayasa+Wellbeing+and+Growth+Center',
  mapsEmbedUrl:
    'https://www.google.com/maps?q=Ardayasa+Wellbeing+and+Growth+Center,+Bukit+Cimanggu+City,+Bogor&output=embed',
} as const;
