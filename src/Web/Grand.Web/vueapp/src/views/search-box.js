/*
 * Search box: autocomplete plus voice search
 * (was Views/Shared/Components/SearchBox/Default.cshtml).
 *
 * The debounce here used to declare its timer handle *inside* the method, so
 * clearTimeout() always got undefined and every keystroke reached
 * /catalog/searchtermautocomplete 600ms later. The handle now lives with the
 * view-model, which is what the 600ms was for.
 */
import { createViewModel } from '../compat/view-model'
import { getRootVm, registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, watchMicrophonePermission } from './shared'

const AUTOCOMPLETE_DELAY = 600

function bySearchType(items, type) {
    return items.filter(item => item.SearchType === type)
}

registerView('searchBox', ({ lang, routes, res }) => {
    let delayTimer = null

    window.searchbox = registerViewModel('searchbox', createViewModel({
        data: () => ({
            recording: false,
            recognition: null,
            searchitems: null,
            focus: false,
            text: '',
            lang,
            placeholder: res.placeholder,
            micMessage: res.micHold,
            searchcategories: null,
            searchbrands: null,
            searchblog: null,
            searchproducts: null,
            allowed: true
        }),
        created() {
            this.allowCheck()
        },
        methods: {
            formSubmit() {
                getRootVm().$refs.searchForm.submit()
            },
            startRecording() {
                this.recognition.start()
            },
            stopRecording() {
                this.recognition.stop()
            },
            allowCheck() {
                watchMicrophonePermission(allowed => { this.allowed = allowed })
            },
            autocompleteVue() {
                const input = getRootVm().$refs.searchBoxInput
                const minLength = input.getAttribute('minlength')

                clearTimeout(delayTimer)
                if (input.value.length < minLength) return

                delayTimer = setTimeout(() => {
                    const categoryEl = document.getElementById('SearchCategoryId')
                    axios.get(routes.autocomplete, {
                        params: { term: this.text, categoryId: categoryEl ? categoryEl.value : '' }
                    }).then(response => {
                        const items = response.data
                        if (!items) return
                        this.searchitems = items
                        this.searchcategories = bySearchType(items, 'Category')
                        this.searchbrands = bySearchType(items, 'Brand')
                        this.searchblog = bySearchType(items, 'Blog')
                        this.searchproducts = bySearchType(items, 'Product')
                    }).catch(err => console.error('[grand] autocomplete failed', err))
                }, AUTOCOMPLETE_DELAY)
            },
            /** Applies one speech result, honouring the spoken control words. */
            handleSpeech(event, searchForm, input) {
                const transcript = event.results[event.resultIndex][0].transcript
                const spoken = transcript.toLowerCase().trim()

                if (spoken === res.voiceStop) {
                    this.stopRecording()
                } else if (!input.value && this.recording) {
                    this.text = transcript
                } else if (spoken === res.voiceGo) {
                    this.stopRecording()
                    searchForm.submit()
                } else if (spoken === res.voiceReset) {
                    this.text = ''
                } else if (this.recording) {
                    this.text = transcript
                }
            }
        },
        watch: {
            text() {
                this.autocompleteVue()
                if (this.text === '') this.searchitems = null
            },
            recording() {
                const Speech = window.SpeechRecognition || window.webkitSpeechRecognition
                if (!Speech) {
                    this.allowed = false
                    return
                }

                if (!this.recording) {
                    this.stopRecording()
                    this.placeholder = res.placeholder
                    this.micMessage = res.micHold
                    return
                }

                const refs = getRootVm().$refs
                this.recognition = new Speech()
                this.recognition.continuous = true
                this.recognition.lang = this.lang
                refs.searchBoxInput.focus()
                this.startRecording()

                this.placeholder = res.micPlaceholder
                this.micMessage = res.micPlaceholder
                this.recognition.addEventListener('result',
                    event => this.handleSpeech(event, refs.searchForm, refs.searchBoxInput))
            }
        }
    }))
})
