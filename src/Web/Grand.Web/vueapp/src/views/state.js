/*
 * Generic state-only view-models.
 *
 * A lot of views declared a `new Vue({ data })` with no methods at all, purely
 * to give a form's v-model bindings somewhere to live. Those needed no code of
 * their own - only a global name and an initial state - so they all share this
 * factory:
 *
 *   <script type="application/json" data-grand-vm="state">
 *       { "name": "contactus", "data": { "Email": "..." } }
 *   </script>
 *
 * This also closes a real hole. The initial values used to be interpolated into
 * JS string literals as '@@Html.Raw(Model.Email)', so a value containing an
 * apostrophe broke the page and a value containing </script> was an injection
 * point. JSON is encoded properly by Json.Serialize.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'

registerView('state', ({ name, data }) => {
    // Registered under its name so an island can declare `vue-island="<name>"`
    // and get it as ordinary component data; still on window for the templates
    // that have not declared it yet and for callers outside the bundle.
    window[name] = registerViewModel(name, createViewModel({ data: () => ({ ...data }) }))
})
