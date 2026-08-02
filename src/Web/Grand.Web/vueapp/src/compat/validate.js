/*
 * vee-validate 3 compatible <validation-observer> / <validation-provider>
 * implemented natively for Vue 3. Supports the rule set actually used by the
 * storefront: required (with allowFalse), email, min, max, confirmed:@vid,
 * exact_length. Error messages come from data-val-* attributes on the input
 * (same source as the original configuration in main.js).
 */
import { h } from 'vue'

export function veeGetMessage(field, rule) {
    if (!field) return null
    const elements = document.getElementsByName(field)
    if (elements && elements[0]) {
        const text = elements[0].getAttribute('data-val-' + rule)
        if (text) return text
    }
    return null
}

/*
 * Where a message comes from, in order:
 *  1. the field's own data-val-* attribute, rendered by the view from Loc[...]
 *  2. the message carried by the rule list itself, as in
 *     "required|exact_length:1,Chck is required" - also from Loc[...]
 *  3. the store's language, published by Partials/JsResources.cshtml
 *  4. English, as a last resort if a store has not imported the resources yet
 *
 * Step 3 is why the defaults are not written out in this file: a shop running in
 * Polish would otherwise get English whenever a field carried no message.
 */
function localized(key, fallback, param) {
    const published = (window.grandValidationMessages || {})[key]
    //A store that has not imported these resources yet gets the key back from
    //the translation service ("validation.required"). Showing that to a visitor
    //would be worse than English.
    const text = published && published !== 'validation.' + key ? published : fallback
    return param === undefined ? text : String(text).replace('{0}', param)
}

const messages = {
    required: (f, p, msg) =>
        veeGetMessage(f, 'required') || msg || localized('required', 'This field is required.'),
    email: f =>
        veeGetMessage(f, 'email') || localized('email', 'This field must be a valid email.'),
    confirmed: f =>
        veeGetMessage(f, 'equalto') || localized('confirmed', 'The confirmation does not match.'),
    min: (f, p) =>
        veeGetMessage(f, 'min') || localized('minlength', 'This field should be at least {0} characters.', p),
    max: (f, p) =>
        veeGetMessage(f, 'max') || localized('maxlength', 'This field should be at most {0} characters.', p),
    exact_length: (f, p, msg) =>
        msg || localized('exactlength', 'Select at least {0} option.', p)
}

const isEmpty = v => v === '' || v === null || v === undefined

const validators = {
    required(value, params) {
        const allowFalse = params && typeof params === 'object' && 'allowFalse' in params
            ? params.allowFalse
            : undefined
        if (allowFalse === false || allowFalse === 'false') {
            return value === true
        }
        return !isEmpty(value)
    },
    email(value) {
        if (isEmpty(value)) return true
        return /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$/i.test(value)
    },
    min(value, param) {
        if (isEmpty(value)) return true
        const p = Array.isArray(param) ? param[0] : param
        return String(value).length >= parseInt(p)
    },
    max(value, param) {
        if (isEmpty(value)) return true
        const p = Array.isArray(param) ? param[0] : param
        return String(value).length <= parseInt(p)
    },
    confirmed(value, param, provider) {
        let target = Array.isArray(param) ? param[0] : param
        if (typeof target === 'string' && target.startsWith('@')) {
            target = target.substring(1)
        }
        const other = provider.observer ? provider.observer.findProvider(target) : null
        return other ? value === other.getValue() : true
    },
    exact_length(value) {
        return !(value && value.length < 1) && !isEmpty(value)
    }
}

function parseRules(rules) {
    if (!rules) return []
    if (typeof rules === 'string') {
        return rules.split('|').filter(Boolean).map(part => {
            const idx = part.indexOf(':')
            if (idx === -1) return { name: part, params: null }
            return { name: part.substring(0, idx), params: part.substring(idx + 1).split(',') }
        })
    }
    return Object.entries(rules)
        .filter(entry => entry[1] !== false)
        .map(([name, params]) => ({ name, params: params === true ? null : params }))
}

export const ValidationProvider = {
    name: 'ValidationProvider',
    inject: { observer: { from: 'veeObserver', default: null } },
    props: {
        tag: { type: String, default: 'div' },
        rules: { type: [String, Object], default: null },
        name: { type: String, default: null },
        vid: { type: String, default: null },
        mode: { type: String, default: null }
    },
    data() {
        return { errors: [], state: null }
    },
    computed: {
        classes() {
            if (this.state === null) return ''
            return this.state ? 'is-valid' : 'is-invalid'
        },
        fieldId() {
            return this.vid || this.name
        }
    },
    created() {
        if (this.observer) this.observer.register(this)
    },
    beforeUnmount() {
        if (this.observer) this.observer.unregister(this)
    },
    mounted() {
        this._onEvent = () => this.validate()

        /*
         * Controls that commit on `change` must not be validated on `input`.
         * A real click on a checkbox dispatches click, input and change as
         * separate tasks, so validating on `input` flushes a re-render in
         * between - and the re-render writes `checked` back from the model,
         * which v-model has not updated yet because that happens on `change`.
         * The change handler then sees an unchecked box, decides nothing
         * happened, and the tick is lost. (A scripted el.click() dispatches all
         * three in one stack, which is why it appeared to work.)
         *
         * <select> was already excluded for the same reason; checkbox and radio
         * belong with it.
         */
        const control = this.control()
        const commitsOnChange = control?.tagName === 'SELECT'
            || control?.type === 'checkbox'
            || control?.type === 'radio'

        if (!commitsOnChange) this.$el.addEventListener('input', this._onEvent)
        this.$el.addEventListener('change', this._onEvent)
        this.$el.addEventListener('focusout', this._onEvent)
    },
    unmounted() {
        if (this.$el && this._onEvent) {
            this.$el.removeEventListener('input', this._onEvent)
            this.$el.removeEventListener('change', this._onEvent)
            this.$el.removeEventListener('focusout', this._onEvent)
        }
    },
    methods: {
        control() {
            return this.$el ? this.$el.querySelector('input, select, textarea') : null
        },
        getValue() {
            const el = this.control()
            if (!el) return undefined
            if (el.type === 'checkbox') {
                /*
                 * A checkbox that carries a value belongs to a group standing for
                 * one answer - a custom address or customer attribute - and its
                 * value is the list of ticked options, the same shape v-model
                 * binds. Reporting el.checked of the first box instead made
                 * `required` see `false`, which is not empty, so a required
                 * attribute never reported anything and the form went to the
                 * server to be refused there.
                 *
                 * A lone checkbox with no value (accept terms, newsletter) keeps
                 * its boolean, which is what { required: { allowFalse: false } }
                 * expects.
                 */
                if (!el.hasAttribute('value')) return el.checked
                const group = this.$el.querySelectorAll('input[type=checkbox]')
                return Array.from(group).filter(box => box.checked).map(box => box.value)
            }
            if (el.type === 'radio') {
                const group = this.$el.querySelectorAll('input[type=radio]')
                for (const r of group) if (r.checked) return r.value
                return undefined
            }
            return el.value
        },
        fieldName() {
            const el = this.control()
            return this.name || (el ? el.getAttribute('name') : null) || this.vid
        },
        validate() {
            const value = this.getValue()
            const errors = []
            const rules = parseRules(this.rules)

            //A message attached to any rule ("exact_length:1,Chck is required")
            //is the field's message, whichever rule ends up failing.
            const declaredMessage = rules
                .map(rule => (Array.isArray(rule.params) ? rule.params[1] : null))
                .find(Boolean) || null

            for (const rule of rules) {
                const fn = validators[rule.name]
                if (!fn) continue
                const ok = fn(value, rule.params, this)
                if (!ok) {
                    const msgFn = messages[rule.name]
                    const p = Array.isArray(rule.params) ? rule.params[0] : rule.params
                    const msg = (rule.params && rule.params[1]) || declaredMessage
                    errors.push(msgFn ? msgFn(this.fieldName(), p, msg) : localized('invalid', 'Invalid value.'))
                }
            }
            this.errors = errors
            this.state = errors.length === 0
            const el = this.control()
            if (el) {
                el.classList.toggle('is-invalid', errors.length > 0)
                el.classList.toggle('is-valid', errors.length === 0 && this.state !== null)
            }
            return Promise.resolve(errors.length === 0)
        },
        reset() {
            this.errors = []
            this.state = null
            const el = this.control()
            if (el) el.classList.remove('is-invalid', 'is-valid')
        }
    },
    render() {
        const slot = this.$slots.default
            ? this.$slots.default({ errors: this.errors, classes: this.classes, valid: this.state })
            : []
        return h(this.tag, null, slot)
    }
}

export const ValidationObserver = {
    name: 'ValidationObserver',
    props: {
        tag: { type: String, default: 'div' }
    },
    provide() {
        return { veeObserver: this }
    },
    data() {
        return { providers: [] }
    },
    computed: {
        errors() {
            const all = {}
            this.providers.forEach(p => { all[p.fieldId || p.fieldName()] = p.errors })
            return all
        },
        invalid() {
            return this.providers.some(p => p.state === false)
        }
    },
    methods: {
        register(provider) {
            this.providers.push(provider)
        },
        unregister(provider) {
            const i = this.providers.indexOf(provider)
            if (i > -1) this.providers.splice(i, 1)
        },
        findProvider(vid) {
            return this.providers.find(p => p.vid === vid || p.name === vid) || null
        },
        validate() {
            return Promise.all(this.providers.map(p => p.validate()))
                .then(results => results.every(Boolean))
        },
        handleSubmit(cb) {
            return this.validate().then(ok => {
                if (ok && typeof cb === 'function') cb()
            })
        },
        reset() {
            this.providers.forEach(p => p.reset())
        }
    },
    render() {
        const slot = this.$slots.default
            ? this.$slots.default({
                handleSubmit: this.handleSubmit,
                validate: this.validate,
                reset: this.reset,
                errors: this.errors,
                invalid: this.invalid
            })
            : []
        return h(this.tag, null, slot)
    }
}

export function registerValidation(app) {
    app.component('validation-provider', ValidationProvider)
    app.component('validation-observer', ValidationObserver)
}
