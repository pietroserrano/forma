# Forma Logo Assets

This directory contains the logo assets for the Forma project.

## Files

- `forma-logo.svg` - Main logo with white background
- `forma-logo-transparent.svg` - Logo with transparent background
- `forma-icon.svg` - Simplified icon version for favicon and smaller contexts
- `icon.png` - PNG version of the icon for NuGet packages

## Logo Design Concept

The Forma logo represents the core architectural patterns that the library provides:

1. **Central Circle (Blue)**: Represents the Mediator pattern - the central hub that connects different components
2. **Purple Ring**: Represents the Decorator pattern - wrapping functionality around existing components
3. **Colored Nodes**: Represent the various modules of Forma:
   - **Red (Top)**: Chain of Responsibility pattern
   - **Green (Bottom)**: Pipeline pattern
   - **Yellow (Left)**: Publisher/Subscriber pattern
   - **Cyan (Right)**: Core module

The connected design visualizes how Forma enables building composable, decoupled, and maintainable application flows using clean architectural principles.

## Usage Guidelines

- Use the main logo (`forma-logo.svg`) for documentation, websites, and presentations
- Use the transparent logo (`forma-logo-transparent.svg`) when placing the logo on colored backgrounds
- Use the icon version (`forma-icon.svg`) for favicons, small UI elements, or when space is limited
- Use the PNG icon (`icon.png`) for NuGet package metadata

## Colors

The logo uses the following color palette:

- Primary Blue: `#5D87E8`
- Purple: `#7C4DFF`
- Red: `#FF5252`
- Green: `#43A047`
- Yellow: `#FFC107`
- Cyan: `#00BCD4`
- White: `#FFFFFF`
- Dark Gray (Text): `#333333`

## NuGet Package Icon Requirements

NuGet packages require icons to be in PNG format. The `icon.png` file is a converted version of `forma-icon.svg` at 256x256 pixels resolution, optimized for NuGet package display.

### Icon Conversion Notes

If you need to recreate or update the PNG icon from the SVG:

#### Using Graphic Editors:
- Open `forma-icon.svg` with Adobe Illustrator, Inkscape, or Figma
- Export as PNG with 256x256 pixel resolution
- Save as `icon.png`

#### Using Command Line Tools:
- **ImageMagick**: `magick convert forma-icon.svg -resize 256x256 icon.png`
- **librsvg (Linux/macOS)**: `rsvg-convert -w 256 -h 256 forma-icon.svg > icon.png`

#### Using Web Services:
- Upload the SVG file to an online converter like [SVG2PNG](https://svgtopng.com/)
- Download the PNG result at 256x256 resolution

After conversion, update any package references from:
```xml
<file src="..\..\assets\forma-icon.svg" target="images\icon.png" />
```

to:
```xml
<file src="..\..\assets\icon.png" target="images\icon.png" />
```
