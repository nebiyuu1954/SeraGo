export { ApiError, getApiErrorMessage, request } from './client.ts'
export type { RequestOptions } from './client.ts'
export { API_ENDPOINTS } from './endpoints.ts'
export type { ApiEndpoint } from './endpoints.ts'
export {
  fetchSavedJobs,
  saveJob,
  unsaveJob,
} from './savedJobs.ts'
export {
  applyToJob,
  fetchApplication,
  fetchMyApplications,
} from './applications.ts'
export { fetchProfile, parseResume, setPassword, updateProfile } from './profile.ts'
export {
  approveJob,
  createJob,
  deleteJob,
  fetchScraperWeekStats,
  fetchScraperWeekDetail,
  fetchJobViews,
  fetchJobViewsStats,
  fetchJob,
  fetchJobLocations,
  fetchJobs,
  rejectJob,
  restoreJob,
  setJobSector,
  runForYouMatching,
  submitJob,
  updateJob,
} from './jobs.ts'
export {
  addSectorAliases,
  createSector,
  deleteSector,
  fetchAdminSectors,
  fetchSectors,
  removeSectorAlias,
  syncScrapedJobs,
  updateSector,
} from './sectors.ts'
export { fetchTopStats } from './stats.ts'
export {
  fetchAdminUser,
  fetchAdminUsers,
  updateUserRole,
  updateUserStatus,
  sendAdminPasswordReset,
  anonymizeUser,
  fetchAdminStats,
  fetchAdminStatsOverview,
  fetchAiClassificationStats,
  fetchAiClassificationJobs,
} from './admin.ts'
export {
  AUTH_TOKENS_KEY,
  buildGoogleChallengeUrl,
  clearStoredAuthTokens,
  confirmEmail,
  fetchExternalProviders,
  fetchWhoAmI,
  forgotPassword,
  refreshAccessToken,
  resendConfirmationEmail,
  resetPassword,
  getStoredAuthTokens,
  signIn,
  signInExternal,
  signOut,
  signUp,
  signUpExternal,
  storeAuthTokens,
} from './auth.ts'
export type { StoredAuthTokens } from './auth.ts'
export {
  uploadFile,
  getPresignedDownloadUrl,
} from './fileUpload.ts'
export {
  fetchSettings,
  updateSettings,
  patchSettingsCategory,
} from './settings.ts'
