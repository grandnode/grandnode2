////import Vue from 'vue'
////import App from './App.vue'

////Vue.config.productionTip = false

////new Vue({
////  render: h => h(App),
////}).$mount('#app')

import Vue from 'vue'
Vue.config.productionTip = false
window.Vue = Vue;

import { CardPlugin } from 'bootstrap-vue'
Vue.use(CardPlugin)
import { LayoutPlugin } from 'bootstrap-vue'
Vue.use(LayoutPlugin)
import { ModalPlugin } from 'bootstrap-vue'
Vue.use(ModalPlugin)
import { SidebarPlugin } from 'bootstrap-vue'
Vue.use(SidebarPlugin)
import { CarouselPlugin } from 'bootstrap-vue'
Vue.use(CarouselPlugin)
import { DropdownPlugin } from 'bootstrap-vue'
Vue.use(DropdownPlugin)
import { FormCheckboxPlugin } from 'bootstrap-vue'
Vue.use(FormCheckboxPlugin)
import { FormRatingPlugin } from 'bootstrap-vue'
Vue.use(FormRatingPlugin)
import { ImagePlugin } from 'bootstrap-vue'
Vue.use(ImagePlugin)
import { NavbarPlugin } from 'bootstrap-vue'
Vue.use(NavbarPlugin)
import { ToastPlugin } from 'bootstrap-vue'
Vue.use(ToastPlugin)
import { TabsPlugin } from 'bootstrap-vue'
Vue.use(TabsPlugin)

import Axios from 'axios'
import Pikaday from 'pikaday'