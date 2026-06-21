import { Component, type ErrorInfo, type ReactNode } from 'react'
import { translate, useI18nStore } from '../../i18n'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

/**
 * Catches render-time errors anywhere in the tree and shows a friendly fallback
 * instead of a blank white screen.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Ohanterat fel i gränssnittet:', error, info)
  }

  private reset = () => {
    this.setState({ hasError: false })
    window.location.reload()
  }

  render() {
    if (!this.state.hasError) return this.props.children

    // Non-reactive read is fine here: the boundary reloads the page on reset.
    const lang = useI18nStore.getState().lang
    const t = (key: Parameters<typeof translate>[1]) => translate(lang, key)

    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 24,
          background: 'var(--bg)',
          color: 'var(--text)',
        }}
      >
        <div
          style={{
            maxWidth: 420,
            textAlign: 'center',
            background: 'var(--surface)',
            border: 'var(--bw) solid var(--border)',
            borderRadius: 'var(--r-lg)',
            padding: 32,
          }}
        >
          <div style={{ fontSize: 18, fontWeight: 600, marginBottom: 8 }}>{t('error.title')}</div>
          <div style={{ fontSize: 14, color: 'var(--text-2)', marginBottom: 20 }}>
            {t('error.body')}
          </div>
          <button className="btn btn-accent" onClick={this.reset} style={{ margin: '0 auto' }}>
            {t('error.reload')}
          </button>
        </div>
      </div>
    )
  }
}
