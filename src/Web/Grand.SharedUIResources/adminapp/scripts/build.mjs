//npm run build: one Vite build per bundle entry (IIFE output cannot be code-split).
import { build } from 'vite'
import { fileURLToPath } from 'node:url'
import { entries } from '../vite.config.js'

const configFile = fileURLToPath(new URL('../vite.config.js', import.meta.url))
const only = process.argv.slice(2)

for (const entry of Object.keys(entries)) {
    if (only.length > 0 && !only.includes(entry)) continue
    process.env.ADMIN_ENTRY = entry
    await build({ configFile, logLevel: 'warn' })
    console.log(`built ${entry}`)
}
