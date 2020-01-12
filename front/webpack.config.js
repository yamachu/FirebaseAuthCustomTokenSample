const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

const output_path = 'dist';
const ASSET_PATH = process.env.ASSET_PATH || '/';

module.exports = function(_env, argv) {
    return [
        {
            entry: './src/index.tsx',
            output: {
                path: path.resolve(__dirname, output_path),
                publicPath: ASSET_PATH,
                filename: '[name].js',
            },
            resolve: {
                extensions: ['.ts', '.tsx', '.js'],
            },
            devtool: argv.mode === 'production' ? false : 'source-map',
            module: {
                rules: [
                    {
                        test: /\.tsx?$/,
                        exclude: /node_modules/,
                        use: ['ts-loader'],
                    },
                    { enforce: 'pre', test: /\.js$/, loader: 'source-map-loader' },
                ],
            },
            plugins: [
                new HtmlWebpackPlugin({
                    template: 'template/index.html',
                    filename: 'index.html',
                }),
            ],
            devServer: {
                disableHostCheck: true,
                contentBase: './dist',
                port: 8081,
                hot: true,
            },
        },
    ];
};
