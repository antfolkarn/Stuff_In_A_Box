import { useEffect } from 'react'
import { useAuthStore } from './store/authStore'
import { useUiStore } from './store/uiStore'
import { useSettingsStore } from './store/settingsStore'
import { useT } from './i18n'
import LoginView from './features/auth/LoginView'
import AppHeader from './shared/components/AppHeader'
import HomeView from './features/home/HomeView'
import SpaceView from './features/space/SpaceView'
import BoxView from './features/box/BoxView'
import SearchView from './features/search/SearchView'
import LabelsView from './features/labels/LabelsView'
import SettingsView from './features/settings/SettingsView'
import AddItemSheet from './features/addItem/AddItemSheet'
import ImageLightbox from './shared/components/ImageLightbox'

export default function App() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const ready = useAuthStore((s) => s.ready)
  const bootstrap = useAuthStore((s) => s.bootstrap)
  const { view, query, addOpen } = useUiStore()
  // Subscribing here ensures the settings store initializes (and applies the saved
  // theme + design) before the first paint, including on the login/loading screens.
  useSettingsStore((s) => s.theme)
  const loadSettings = useSettingsStore((s) => s.loadFromServer)
  const t = useT()

  useEffect(() => {
    bootstrap()
  }, [bootstrap])

  // Once signed in, pull the user's settings from the DB so theme/design follow
  // them across devices.
  useEffect(() => {
    if (isAuthenticated) loadSettings()
  }, [isAuthenticated, loadSettings])

  if (!ready) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-3)' }}>
        {t('common.loading')}
      </div>
    )
  }

  if (!isAuthenticated) return <LoginView />

  const showSearch = query.trim().length > 0

  return (
    <div className="app-layout">
      <AppHeader />
      <main className="main-content">
        {showSearch ? (
          <SearchView />
        ) : view === 'settings' ? (
          <SettingsView />
        ) : view === 'labels' ? (
          <LabelsView />
        ) : view === 'box' ? (
          <BoxView />
        ) : view === 'space' ? (
          <SpaceView />
        ) : (
          <HomeView />
        )}
      </main>
      {addOpen && <AddItemSheet />}
      <ImageLightbox />
    </div>
  )
}
