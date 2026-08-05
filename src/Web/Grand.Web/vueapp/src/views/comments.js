/*
 * Blog and news comment forms (were Views/Blog/BlogPost.cshtml and
 * Views/News/NewsItem.cshtml).
 *
 * The two views carried the same code twice, differing only in the global name,
 * the form id and whether there was a title field. One factory covers both.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios } from './shared'

registerView('comments', ({ name, formId, comments, fields }) => {
    const blank = {}
    fields.forEach(field => { blank[field] = '' })

    window[name] = registerViewModel(name, createViewModel({
        data: () => ({ Model: comments, ...blank }),
        methods: {
            submitComment() {
                const form = document.getElementById(formId)
                return axios.post(form.getAttribute('action'), new FormData(form), {
                    headers: { Accept: 'application/json' }
                }).then(response => {
                    const { success, message, model } = response.data
                    window.vm.displayBarNotification(message, '', success ? 'success' : 'error',
                        success ? 3000 : 3500)
                    if (!success) return
                    fields.forEach(field => { this[field] = '' })
                    if (model) this.addComment(model)
                }).catch(err => console.error('[grand] comment submit failed', err))
            },
            addComment(model) {
                this.Model.push(model)
            }
        }
    }))
})
