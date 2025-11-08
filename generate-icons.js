const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const logoPath = path.join(__dirname, 'HiveReaderLogo.png');

async function generateIcons() {
    console.log('Generating icons from HiveReaderLogo.png...\n');

    // Extension icons
    const extensionSizes = [16, 48, 128];
    for (const size of extensionSizes) {
        const outputPath = path.join(__dirname, 'extension', 'icons', `icon${size}.png`);
        await sharp(logoPath)
            .resize(size, size)
            .toFile(outputPath);
        console.log(`Created extension icon: icon${size}.png`);
    }

    // Web favicons
    const webIcons = [
        { size: 16, filename: 'favicon.png' },
        { size: 32, filename: 'favicon-32x32.png' },
        { size: 180, filename: 'apple-touch-icon.png' },
        { size: 192, filename: 'favicon-192x192.png' },
        { size: 512, filename: 'favicon-512x512.png' }
    ];

    for (const icon of webIcons) {
        const outputPath = path.join(__dirname, 'web', 'public', icon.filename);
        await sharp(logoPath)
            .resize(icon.size, icon.size)
            .toFile(outputPath);
        console.log(`Created web favicon: ${icon.filename}`);
    }

    // Create favicon.ico (multi-size ico file using 16x16)
    const icoPath = path.join(__dirname, 'web', 'public', 'favicon.ico');
    await sharp(logoPath)
        .resize(32, 32)
        .toFormat('png')
        .toFile(icoPath);
    console.log(`Created web favicon: favicon.ico`);

    console.log('\nAll icons generated successfully!');
}

generateIcons().catch(err => {
    console.error('Error generating icons:', err);
    process.exit(1);
});
