# Nota sulla conversione delle icone

Le specifiche NuGet richiedono che le icone dei pacchetti siano in formato PNG, mentre abbiamo creato le nostre in formato SVG.

## Istruzioni per la conversione

Per utilizzare queste icone nei pacchetti NuGet, è necessario convertirle da SVG a PNG.

### Metodi di conversione:

1. **Utilizzo di un editor grafico**:
   - Apri il file SVG con un editor grafico come Adobe Illustrator, Inkscape o Figma
   - Esporta come PNG con una risoluzione di 128x128 o 256x256 pixel
   - Salva il file con nome `icon.png` nella cartella `assets`

2. **Utilizzo di strumenti da riga di comando**:
   - Con ImageMagick: `magick convert forma-icon.svg -resize 256x256 icon.png`
   - Con librsvg (Linux/macOS): `rsvg-convert -w 256 -h 256 forma-icon.svg > icon.png`

3. **Servizi web**:
   - Carica il file SVG su un convertitore online come [SVG2PNG](https://svgtopng.com/)
   - Scarica il PNG risultante

Dopo la conversione, aggiorna i riferimenti nei file .nuspec da:
```xml
<file src="..\..\assets\forma-icon.svg" target="images\icon.png" />
```

a:
```xml
<file src="..\..\assets\icon.png" target="images\icon.png" />
```
