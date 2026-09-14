//npm run build: one Vite build per shipped bundle entry (IIFE output cannot be code-split).
//npm run build -- admin.legacy builds a named entry instead.
import { build } from 'vite'
import { fileURLToPath } from 'node:url'
import { entries, shippedEntries } from '../vite.config.js'

const configFile = fileURLToPath(new URL('../vite.config.js', import.meta.url))
const only = process.argv.slice(2)

const selected = only.length > 0 ? only : shippedEntries
for (const entry of selected) {
    if (!entries[entry]) throw new Error(`unknown entry ${entry}`)
    process.env.ADMIN_ENTRY = entry
    await build({ configFile, logLevel: 'warn' })
    console.log(`built ${entry}`)
}
