module.exports = {
    outputDir: '../wwwroot/bundles',
    lintOnSave: false,
    productionSourceMap: false,
    filenameHashing: false,
    css: {
        extract: {
            filename: 'libs.css'
        }
    },
    configureWebpack: {
        output: {
            filename: '[name].runtime.bundle.js',
        },
        resolve: {
            alias: {
                'vue$': 'vue/dist/vue.esm.js'
            }
        },
    },
}