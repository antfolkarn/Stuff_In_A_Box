import { Component, type ErrorInfo, type ReactNode } from 'react'

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
            border: '1px solid var(--border)',
            borderRadius: 16,
            padding: 32,
          }}
        >
          <div style={{ fontSize: 18, fontWeight: 600, marginBottom: 8 }}>Något gick fel</div>
          <div style={{ fontSize: 14, color: 'var(--text-2)', marginBottom: 20 }}>
            Ett oväntat fel inträffade. Försök ladda om sidan.
          </div>
          <button className="btn btn-accent" onClick={this.reset} style={{ margin: '0 auto' }}>
            Ladda om
          </button>
        </div>
      </div>
    )
  }
}
