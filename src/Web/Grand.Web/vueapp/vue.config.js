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
    optimization: {
      splitChunks: false
    },
    output: {
      filename: 'libs.js'
    },
    resolve: {
      alias: {
        'vue$': 'vue/dist/vue.esm.js'
      }
    },
  },
}