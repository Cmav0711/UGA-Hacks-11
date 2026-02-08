# Symbol Templates

This directory contains training images for symbol recognition organized in folders.

## Usage
Create a folder for each symbol you want to recognize. Inside each folder, place multiple training images showing variations of that symbol. The system will train on all images in each folder and identify the symbol based on the folder name.

### Structure
Each symbol should have its own folder containing multiple training images:
```
symbols/
  ├── circle/
  │   ├── circle_1.png
  │   ├── circle_2.png
  │   ├── circle_3.png
  │   └── circle_4.png
  ├── square/
  │   ├── square_1.png
  │   ├── square_2.png
  │   └── square_3.png
  ├── star/
  │   ├── star_1.png
  │   └── star_2.png
  └── README.md
```

## Training Images
Each training image should:
- Be a PNG, JPG, or JPEG file
- Contain a clear representation of the symbol
- Show variations (different sizes, positions, slight distortions)
- Have high contrast (white symbol on black background works best)

## How It Works
When you capture an image, the system:
1. Compares it against all training images in each symbol folder
2. Calculates an average confidence score for each symbol
3. Returns the folder name (symbol name) with the highest average confidence

## Benefits of Multiple Training Images
- Better recognition of messy or imperfect drawings
- More robust to variations in size, position, and orientation
- Higher accuracy through ensemble matching

## Supported Formats
- PNG (recommended)
- JPG/JPEG
