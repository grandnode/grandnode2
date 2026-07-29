module.exports = {
    publicPath: '/bundles/',
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
                'vue$': 'vue/dist/vue.esm-bundler.js'
            }
        },
        optimization: {
            splitChunks: {
                cacheGroups: {
                    styles: {
                        name: 'styles',
                        type: 'css/mini-extract',
                        chunks: 'all',
                        enforce: true
                    }
                }
            }
        },
    },
}