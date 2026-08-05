/*
 * Product image gallery (was Views/Product/Partials/Pictures.cshtml and its
 * Theme.Modern override).
 *
 * The two `gallery-images` registrations differed only in the img classes, and
 * both built their template - and the image array - as a Razor-interpolated JS
 * string. The markup is here, the pictures come in on the payload.
 */
import { registerComponent } from '../runtime/islands'
import { VueGallerySlideshow } from '../compat/bv-components'
import { registerView } from '../views/index'
import { delegate } from './dom'

const TEMPLATES = {
    main: mainImageId => `<div>
        <img class="image main-image zoom" id="${mainImageId}" v-for="(image, i) in images" :src="image.url"
             :alt="image.alt" :title="image.title" :key="i" @click="index=i">
        <vue-gallery-slideshow :images="images" :index="index" @close="index=null"></vue-gallery-slideshow>
      </div>`,
    thumb: () => `<div>
        <img class="image thumb-image zoom" v-for="(image, i) in images" :src="image.url" :datasrc="image.fullimg"
             :alt="image.alt" :title="image.title" :key="i" @click="index=i">
        <div>
            <vue-gallery-slideshow :images="images" :index="index" @close="index=null"></vue-gallery-slideshow>
        </div>
      </div>`
}

registerView('productGallery', ({ componentName, variant, mainImageId, images }) => {
    registerComponent(componentName, {
        template: TEMPLATES[variant](mainImageId),
        data: () => ({ images, index: null }),
        components: { VueGallerySlideshow }
    })
})

/* Thumbnail strip under the main image: swaps the main image source. */
registerView('galleryThumbnails', ({ mainImageId }) => {
    delegate('click', '[data-gallery-thumb]', thumb => {
        const main = document.getElementById(mainImageId)
        if (main) main.setAttribute('src', thumb.getAttribute('data-src'))
    })
})

/* Theme.Modern's zoom button, which used onclick="zoomImg(this)". */
registerView('imageZoom', () => {
    delegate('click', '[data-zoom-image]', button => {
        const image = button.parentElement?.querySelector('img')
        if (image && window.mediumZoom) {
            window.mediumZoom(image, { background: '#232323', margin: 30 }).open()
        }
    })
})
