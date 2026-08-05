/*
 * Vendor rating summary (was Views/Catalog/Partials/VendorReviewOverview.cshtml).
 *
 * `rating` was kept in sync by a watcher on Model; a computed says the same
 * thing without the extra state, and guards the divide-by-zero that bound NaN
 * into the stars when a vendor had no reviews yet.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'

registerView('vendorReviewOverview', ({ model }) => {
    window.vendorreviewsoverview = registerViewModel('vendorreviewsoverview', createViewModel({
        data: () => ({ Model: model }),
        computed: {
            rating() {
                return this.Model && this.Model.TotalReviews
                    ? this.Model.RatingSum / this.Model.TotalReviews
                    : 0
            }
        }
    }))
})
