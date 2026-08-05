/*
 * Voice navigation - say "cart", "blog", … and go there
 * (was Views/Shared/Components/VoiceNavigation/Default.cshtml).
 *
 * The command list and its localized trigger words come from the payload; the
 * view used to build seven near-identical JSON literals by hand.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { watchMicrophonePermission } from './shared'

const PUNCTUATION = /[`~!#$%^&*()_|+\-=?;:'",.<>{}[\]\\/]/gi

function speak(text) {
    const message = new SpeechSynthesisUtterance()
    message.text = text
    window.speechSynthesis.speak(message)
}

registerView('voiceNavigation', ({ lang, commands, res }) => {
    window.voicenavigator = registerViewModel('voicenavigator', createViewModel({
        data: () => ({
            recording: false,
            recognition: null,
            lang,
            commands,
            transcript: '',
            variant: 'info',
            micMessage: res.micHold,
            voiceMessage: res.tryAgain,
            allowed: true
        }),
        created() {
            this.allowCheck()
        },
        methods: {
            startRecording() {
                this.recognition.start()
            },
            stopRecording() {
                this.recognition.stop()
            },
            allowCheck() {
                watchMicrophonePermission(allowed => { this.allowed = allowed })
            },
            /** Matches one final speech result against the command triggers. */
            handleSpeech(event) {
                if (!this.recording || !event.results[0].isFinal) return

                const transcript = event.results[event.resultIndex][0].transcript
                this.transcript = transcript

                const spoken = transcript.replace(PUNCTUATION, '').toLowerCase()
                const match = this.commands.find(command =>
                    command.triggers.some(trigger => spoken.includes(trigger)))

                if (match) {
                    this.stopRecording()
                    this.variant = 'success'
                    setTimeout(() => { window.location.href = match.url }, 300)
                } else {
                    setTimeout(() => speak(this.voiceMessage), 300)
                }
            }
        },
        watch: {
            recording() {
                const Speech = window.SpeechRecognition || window.webkitSpeechRecognition
                if (!Speech) {
                    this.allowed = false
                    return
                }

                if (!this.recording) {
                    this.stopRecording()
                    this.transcript = ''
                    this.variant = 'info'
                    this.micMessage = res.micHold
                    return
                }

                this.recognition = new Speech()
                this.recognition.continuous = true
                this.recognition.lang = this.lang
                this.recognition.interimResults = false
                this.recognition.maxAlternatives = 1
                this.startRecording()

                this.micMessage = res.micPlaceholder
                this.recognition.addEventListener('result', event => this.handleSpeech(event))
            }
        }
    }))
})
