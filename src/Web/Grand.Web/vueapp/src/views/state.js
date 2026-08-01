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
import LegacyVue from '../compat/core'
import { registerView } from './index'

registerView('state', ({ name, data }) => {
    window[name] = new LegacyVue({ data: () => ({ ...data }) })
})
