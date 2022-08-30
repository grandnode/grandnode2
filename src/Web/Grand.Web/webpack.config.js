
const path = require("path");

const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const OptimizeCSSAssetsPlugin = require("css-minimizer-webpack-plugin");
const RemoveEmptyScriptsPlugin = require('webpack-remove-empty-scripts');

module.exports = {
    mode: 'production',
    entry: {
        "style.rtl": ["./wwwroot/theme/css/common/common.rtl.css",
            "./wwwroot/theme/css/header/header.rtl.css",
            "./wwwroot/theme/css/catalog/catalog.rtl.css",
            "./wwwroot/theme/css/product/product.rtl.css",
            "./wwwroot/theme/css/customer/customer.rtl.css",
            "./wwwroot/theme/css/cart/cart.rtl.css"
        ],
        "style": ["./wwwroot/theme/css/common/common.css",
            "./wwwroot/theme/css/header/header.css",
            "./wwwroot/theme/css/catalog/catalog.css",
            "./wwwroot/theme/css/product/product.css",
            "./wwwroot/theme/css/customer/customer.css",
            "./wwwroot/theme/css/cart/cart.css"
        ],
    },
    output: {
        path: path.resolve(__dirname, "wwwroot/bundles/"),
        filename: "[name].min.js",
    },
    optimization: {
        splitChunks: {
            chunks() {
                return false;
            },
        },
    },
    plugins: [
        new MiniCssExtractPlugin({ filename: "[name].min.css" }),
        new OptimizeCSSAssetsPlugin({}),
        new RemoveEmptyScriptsPlugin(),
    ],
    module: {
        rules: [
            {
                test: /\.css$/,
                use: [MiniCssExtractPlugin.loader, "css-loader"]
            }
        ]
    },

}