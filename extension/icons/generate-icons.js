const fs = require('fs');

// Simple PNG file structure for a solid color square
function createPNG(size, r, g, b) {
    const { createCanvas } = require('canvas');
    const canvas = createCanvas(size, size);
    const ctx = canvas.getContext('2d');

    // Orange background (Kindle color)
    ctx.fillStyle = `rgb(${r}, ${g}, ${b})`;
    ctx.fillRect(0, 0, size, size);

    // White book icon
    ctx.strokeStyle = '#FFFFFF';
    ctx.fillStyle = '#FFFFFF';
    ctx.lineWidth = Math.max(2, size / 16);

    const padding = size * 0.2;
    const bookWidth = size - (padding * 2);
    const bookHeight = size - (padding * 2);

    // Book rectangle
    ctx.strokeRect(padding, padding, bookWidth, bookHeight);

    // Book spine
    ctx.beginPath();
    ctx.moveTo(padding + bookWidth * 0.25, padding);
    ctx.lineTo(padding + bookWidth * 0.25, padding + bookHeight);
    ctx.stroke();

    // "K" letter for Kindle
    if (size >= 48) {
        ctx.font = `bold ${size * 0.4}px Arial`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('K', size / 2 + size * 0.05, size / 2);
    }

    return canvas.toBuffer('image/png');
}

// Try to use canvas, fallback to simple solid color
try {
    const sizes = [16, 48, 128];
    sizes.forEach(size => {
        const buffer = createPNG(size, 255, 153, 0); // Kindle orange
        fs.writeFileSync(`icon${size}.png`, buffer);
        console.log(`Created icon${size}.png`);
    });
} catch (err) {
    console.error('Canvas module not found, creating simple placeholders...');

    // Create minimal valid PNG files (1x1 orange pixel, then scale)
    const createSimplePNG = (size) => {
        // This is a base64 encoded 16x16 orange PNG
        const base64 = 'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAHklEQVR42mP8z8DwHwyZGFABIwMVwKgBowaMGkBdAABeRQH9HBnI4QAAAABJRU5ErkJggg==';
        const buffer = Buffer.from(base64, 'base64');
        fs.writeFileSync(`icon${size}.png`, buffer);
    };

    [16, 48, 128].forEach(size => {
        createSimplePNG(size);
        console.log(`Created placeholder icon${size}.png`);
    });
}
