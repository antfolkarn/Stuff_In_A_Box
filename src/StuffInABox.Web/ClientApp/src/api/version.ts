export interface VersionInfo {
  version: string
  commit: string
  buildTimeUtc: string | null
}

// /version sits at the site root (not under /api/v1), and is anonymous — so a plain
// fetch is enough. Used to surface the running build in the UI footer.
export const getVersion = (): Promise<VersionInfo> =>
  fetch('/version').then((r) => r.json())
