import { useMemo } from 'react'
import { create } from 'zustand'
import { messages, type Lang, type MessageKey } from './messages'

export { LANGS } from './messages'
export type { Lang, MessageKey } from './messages'

const LANG_KEY = 'sib_lang'

type Params = Record<string, string | number>

/**
 * Picks the initial language. A previously chosen language (localStorage) wins;
 * otherwise we detect from the browser. Swedish is only used when the browser
 * actually prefers Swedish — everything else falls back to English.
 */
function detectLang(): Lang {
  const stored = localStorage.getItem(LANG_KEY)
  if (stored === 'sv' || stored === 'en') return stored
  const prefs = navigator.languages?.length ? navigator.languages : [navigator.language]
  return prefs.some((l) => l?.toLowerCase().startsWith('sv')) ? 'sv' : 'en'
}

function fill(template: string, params?: Params): string {
  if (!params) return template
  return template.replace(/\{(\w+)\}/g, (_, k) => {
    const v = params[k]
    return v === undefined ? `{${k}}` : String(v)
  })
}

/** Translate a key for a given language, falling back to English then the key. */
export function translate(lang: Lang, key: MessageKey, params?: Params): string {
  const template = messages[lang]?.[key] ?? messages.en[key] ?? key
  return fill(template, params)
}

interface I18nState {
  lang: Lang
  setLang: (lang: Lang) => void
}

export const useI18nStore = create<I18nState>((set) => {
  const initial = detectLang()
  document.documentElement.setAttribute('lang', initial)
  return {
    lang: initial,
    setLang: (lang) => {
      localStorage.setItem(LANG_KEY, lang)
      document.documentElement.setAttribute('lang', lang)
      set({ lang })
    },
  }
})

export type TFunction = (key: MessageKey, params?: Params) => string

/** Hook returning a `t()` bound to the current language; re-renders on change. */
export function useT(): TFunction {
  const lang = useI18nStore((s) => s.lang)
  return useMemo(() => (key: MessageKey, params?: Params) => translate(lang, key, params), [lang])
}
