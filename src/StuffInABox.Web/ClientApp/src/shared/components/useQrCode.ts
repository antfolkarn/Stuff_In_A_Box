import { useRef } from 'react'
import qrcode from 'qrcode-generator'

/**
 * Generates QR code data URLs lazily and caches them in a ref-backed Map,
 * mirroring the prototype's `qr: Map<boxNumber, dataUrl>` behaviour.
 */
export function useQrCache() {
  const cache = useRef<Map<number, string>>(new Map())

  function getQr(boxNumber: number): string {
    const existing = cache.current.get(boxNumber)
    if (existing) return existing

    const deepLink = `${window.location.origin}/#box=${boxNumber}`
    const qr = qrcode(0, 'M')
    qr.addData(deepLink)
    qr.make()
    const dataUrl = qr.createDataURL(4, 0)
    cache.current.set(boxNumber, dataUrl)
    return dataUrl
  }

  return getQr
}
