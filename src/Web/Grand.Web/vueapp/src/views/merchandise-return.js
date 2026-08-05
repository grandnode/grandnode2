/*
 * Merchandise return request form (was Views/MerchandiseReturn/MerchandiseReturn.cshtml).
 *
 * One `quantity_<itemId>` field per returnable line, which is why the state has
 * to be built from the payload rather than declared literally.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'

registerView('merchandiseReturn', ({ pickupDate, showPickupDate, itemIds }) => {
    const quantities = {}
    itemIds.forEach(id => { quantities['quantity_' + id] = '0' })

    window.merchandisereturns = registerViewModel('merchandisereturns', createViewModel({
        data: () => ({
            ...(showPickupDate ? { PickupDate: pickupDate ?? '' } : {}),
            newAddress: false,
            ...quantities,
            checkboxes: []
        }),
        created() {
            this.isNewAddress()
        },
        methods: {
            allowed(checkbox, element) {
                const target = window.vm.$refs[element]
                if (!target) return
                if (checkbox.checked) target.removeAttribute('disabled')
                else target.setAttribute('disabled', 'disabled')
            },
            isNewAddress() {
                const select = document.getElementById('pickup-address-select')
                this.newAddress = !select || select.value === ''
            }
        }
    }))
})
