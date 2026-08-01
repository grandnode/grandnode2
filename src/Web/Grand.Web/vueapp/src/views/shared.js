/*
 * Helpers the extracted view-models share.
 *
 * Each of these existed as copy-pasted lines in the inline view scripts - the
 * antiforgery lookup alone appeared in more than twenty .cshtml files, and the
 * error path was usually a bare alert().
 */
import axios from 'axios'
import { $bvToast } from '../compat/bv-services'

export function antiForgeryToken() {
    return document.querySelector('input[name=__RequestVerificationToken]')?.value ?? ''
}

/** Builds a FormData with the antiforgery token already appended. */
export function formData(fields = {}) {
    const data = new FormData()
    Object.entries(fields).forEach(([key, value]) => data.append(key, value))
    data.append('__RequestVerificationToken', antiForgeryToken())
    return data
}

export function notify(message, title, variant = 'danger') {
    if (!message) return
    $bvToast.toast(message, { title, variant, autoHideDelay: 5000, appendToast: true })
}

/**
 * Turns an axios rejection into something worth showing. Replaces `alert(error)`,
 * which showed the visitor a raw "Error: Request failed with status code 500".
 */
export function notifyRequestError(error, title, fallback) {
    const message = error?.response?.data?.message || fallback || error?.message || String(error)
    notify(message, title)
}

/**
 * Reports whether the microphone is usable, now and whenever that changes.
 * Shared by the search box and the voice navigator, which asked identically.
 */
export function watchMicrophonePermission(onChange) {
    if (!navigator.permissions) return
    navigator.permissions.query({ name: 'microphone' })
        .then(status => {
            onChange(status.state !== 'denied')
            status.onchange = () => onChange(status.state !== 'denied')
        })
        .catch(() => { /* browsers that reject the microphone query outright */ })
}

export { axios }
