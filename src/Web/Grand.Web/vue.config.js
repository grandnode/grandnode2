module.exports = {
  outputDir: './wwwroot/dist',
  lintOnSave: false,
  productionSourceMap: false,
  filenameHashing: false,
  css: {
    extract: {
      filename: '[name].css'
    }
  },
  configureWebpack: {
    optimization: {
      splitChunks: false
    },
    output: {
      filename: '[name].js'
    },
    resolve: {
      alias: {
        'vue$': 'vue/dist/vue.esm.js'
      }
    }
  },
}