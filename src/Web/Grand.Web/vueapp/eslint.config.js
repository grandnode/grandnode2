import js from '@eslint/js'
import vue from 'eslint-plugin-vue'
import globals from 'globals'

export default [
    js.configs.recommended,
    ...vue.configs['flat/essential'],
    {
        files: ['src/**/*.js'],
        languageOptions: {
            ecmaVersion: 'latest',
            sourceType: 'module',
            globals: {
                ...globals.browser,
                //set by the Razor views and theme scripts, not by this bundle
                AxiosCart: 'readonly',
                grandRes: 'readonly',
                grandRoutes: 'readonly'
            }
        },
        rules: {
            //<countdown> is written literally in the Razor views; the multi-word
            //convention would mean renaming the tag in every template that uses it
            'vue/multi-word-component-names': 'off'
        }
    }
]
