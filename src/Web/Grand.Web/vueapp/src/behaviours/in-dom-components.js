/*
 * Global Vue components whose templates are `<template id="...">` elements in
 * the page (were one inline Vue.component call per view: the quick-view
 * attribute row, and Theme.Modern's product box and product list box).
 *
 * The template stays in the markup - it is Razor-rendered and theme-specific -
 * and only the registration moves here.
 */
import { registerComponent } from '../runtime/islands'
import { registerView } from '../views/index'

registerView('inDomComponents', ({ components }) => {
    components.forEach(({ name, template, props }) => {
        registerComponent(name, {
            template,
            props: (props || []).reduce((definition, prop) => {
                definition[prop] = null
                return definition
            }, {})
        })
    })
})
